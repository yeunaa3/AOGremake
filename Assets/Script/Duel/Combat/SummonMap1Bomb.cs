using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Prefab triệu hồi cung Map 1:
    /// Chọn cố định 1 điểm rơi trên không (không bám theo kẻ địch nếu kẻ địch di chuyển),
    /// rung rung rồi phi rơi cắm xuống đất, nằm im đếm ngược sau 4s thì phát nổ gây sát thương diện rộng.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SummonMap1Bomb : MonoBehaviour
    {
        [Header("Sát Thương & Vụ Nổ")]
        [Tooltip("Lượng sát thương nổ gây ra")]
        [SerializeField, Min(1)] private int explosionDamage = 65;
        [Tooltip("Bán kính vụ nổ (mét)")]
        [SerializeField, Min(0.5f)] private float explosionRadius = 2.4f;

        [Header("Thời Gian & Vận Tốc")]
        [Tooltip("Độ cao lơ lửng ban đầu trước khi rơi xuống (mét)")]
        [SerializeField] private float hoverHeight = 3.6f;
        [Tooltip("Thời gian rung rung báo hiệu trên không trước khi rơi xuống (giây)")]
        [SerializeField, Min(0.1f)] private float hoverDuration = 0.5f;
        [Tooltip("Cường độ rung rung trên không")]
        [SerializeField] private float hoverShakeIntensity = 0.08f;
        [Tooltip("Tốc độ phi cắm xuống đất")]
        [SerializeField, Min(1f)] private float plungeSpeed = 28f;
        [Tooltip("Độ cắm sâu của mũi vũ khí vào lòng đất (mét) - chỉ cắm mũi nhọn, không chìm nửa thân")]
        [SerializeField, Min(0.05f)] private float embedDepth = 0.3f;
        [Tooltip("Thời gian nằm im cắm dưới đất trước khi phát nổ (mặc định 4s theo yêu cầu)")]
        [SerializeField, Min(0.5f)] private float fuseDuration = 4.0f;
        [Tooltip("Thời gian rung lắc cảnh báo trước khi nổ (giây cuối)")]
        [SerializeField, Min(0.2f)] private float warningShakeDuration = 1.0f;
        [Tooltip("Cường độ rung lắc khi sắp nổ")]
        [SerializeField] private float groundShakeIntensity = 0.12f;

        [Header("Hiệu Ứng & Vật Lý")]
        [Tooltip("Layer mặt đất")]
        [SerializeField] private LayerMask groundLayers = 1 << 8;
        [Tooltip("Hiệu ứng hạt vụ nổ")]
        [SerializeField] private ParticleSystem explosionParticlePrefab;
        [Tooltip("Màu chớp cảnh báo khi chuẩn bị nổ")]
        [SerializeField] private Color warningFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

        // Quản lý sát thương dồn từ nhiều quả bom trong cùng 1 đợt nổ
        private static readonly Dictionary<PlayerController, float> recentHitTimes = new Dictionary<PlayerController, float>();
        private static readonly Dictionary<PlayerController, int> recentHitCounts = new Dictionary<PlayerController, int>();

        private PlayerController owner;
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private Vector3 fixedSpawnPos;

        public void Setup(PlayerController source)
        {
            owner = source;
            PlayerController target = (source != null && source.Enemy != null) ? source.Enemy : null;

            // Điểm rơi cố định đã được chọn từ trước trên trục X - KHÔNG bám theo kẻ địch
            float fixedX = transform.position.x;
            float baseY = (target != null) ? target.transform.position.y : transform.position.y;
            fixedSpawnPos = new Vector3(fixedX, baseY + hoverHeight, 0f);
            transform.position = fixedSpawnPos;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) originalColor = spriteRenderer.color;

            StopAllCoroutines();
            StartCoroutine(BombSequence());
        }

        private IEnumerator BombSequence()
        {
            // --- GIAI ĐOẠN 1: Rung rung tại đúng điểm rơi đã chọn trên không (KHÔNG BÁM THEO) ---
            float hoverElapsed = 0f;
            while (hoverElapsed < hoverDuration)
            {
                hoverElapsed += Time.deltaTime;
                float ox = Random.Range(-hoverShakeIntensity, hoverShakeIntensity);
                float oy = Random.Range(-hoverShakeIntensity, hoverShakeIntensity);
                transform.position = fixedSpawnPos + new Vector3(ox, oy, 0f);
                yield return null;
            }

            transform.position = fixedSpawnPos;

            // --- GIAI ĐOẠN 2: Tính toán mặt đất chuẩn xác, chỉ cắm sâu phần mũi nhọn (embedDepth) ---
            float fallbackSurfaceY = (owner != null && owner.Enemy != null) 
                ? owner.Enemy.transform.position.y 
                : (owner != null ? owner.transform.position.y : 0f);
            float targetGroundY = CalculateGroundY(fixedSpawnPos.x, fallbackSurfaceY);
            Vector3 groundPos = new Vector3(fixedSpawnPos.x, targetGroundY, 0f);

            while (Vector3.Distance(transform.position, groundPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, groundPos, plungeSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = groundPos;
            SkillAudioManager.PlayEarthquake(); // Âm thanh cắm xuống đất

            // --- GIAI ĐOẠN 3: Nằm im trên đất và đếm ngược sau 4s thì nổ ---
            float groundElapsed = 0f;
            Vector3 stickPos = groundPos;

            while (groundElapsed < fuseDuration)
            {
                groundElapsed += Time.deltaTime;
                float remainingTime = fuseDuration - groundElapsed;

                // Nếu còn dưới 1 giây cuối -> Rung lắc và nháy đỏ dồn dập cảnh báo nổ
                if (remainingTime <= warningShakeDuration)
                {
                    float warningProgress = 1f - (remainingTime / warningShakeDuration);
                    float curIntensity = Mathf.Lerp(0f, groundShakeIntensity, warningProgress);
                    float rx = Random.Range(-curIntensity, curIntensity);
                    float ry = Random.Range(-curIntensity, curIntensity);
                    transform.position = stickPos + new Vector3(rx, ry, 0f);

                    if (spriteRenderer != null)
                    {
                        float flashFreq = Mathf.Lerp(10f, 30f, warningProgress);
                        float sin = (Mathf.Sin(groundElapsed * flashFreq) + 1f) * 0.5f;
                        spriteRenderer.color = Color.Lerp(originalColor, warningFlashColor, sin);
                    }
                }
                else
                {
                    // Nằm im hoàn toàn
                    transform.position = stickPos;
                    if (spriteRenderer != null) spriteRenderer.color = originalColor;
                }

                yield return null;
            }

            // --- GIAI ĐOẠN 4: Phát nổ! ---
            Explode();
        }

        private float CalculateGroundY(float x, float fallbackY)
        {
            // 1. Tìm vị trí bề mặt đất dưới chân vũ khí
            float surfaceY = fallbackY;
            int mask = groundLayers.value;
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) mask |= (1 << groundLayer);

            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, fixedSpawnPos.y), Vector2.down, 50f, mask);
            float highestGroundY = float.MinValue;
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.isTrigger) continue;

                if (hit.point.y > highestGroundY)
                {
                    highestGroundY = hit.point.y;
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                surfaceY = highestGroundY;
            }

            // 2. Tính khoảng cách từ Pivot (transform.position.y) xuống điểm thấp nhất của vũ khí (mũi kiếm)
            float pivotToBottom = 0.5f;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                // spriteRenderer.bounds.min.y luôn là tọa độ Y thấp nhất thực tế của sprite trong world space
                pivotToBottom = transform.position.y - spriteRenderer.bounds.min.y;
            }
            else
            {
                Collider2D col = GetComponent<Collider2D>();
                if (col != null)
                {
                    pivotToBottom = transform.position.y - col.bounds.min.y;
                }
            }

            // 3. Đảm bảo mũi kiếm cắm sâu vào đất bằng embedDepth:
            //    Mũi kiếm ở: surfaceY - embedDepth
            //    => Pivot cần đặt ở: (surfaceY - embedDepth) + pivotToBottom
            float safeEmbed = Mathf.Clamp(embedDepth, 0.05f, Mathf.Max(0.1f, pivotToBottom * 0.8f));
            return (surfaceY - safeEmbed) + pivotToBottom;
        }

        private void Explode()
        {
            SkillAudioManager.PlayBoom();

            // Hiệu ứng nổ lửa
            if (explosionParticlePrefab != null)
            {
                ParticleSystem ps = Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 2f);
            }

            // SỬA LỖI DAME 1000: Dùng HashSet để mỗi nhân vật chỉ nhận sát thương đúng 1 lần
            // (Character có tới 11 collider con, không dùng HashSet sẽ kích hoạt 11 lần sát thương dồn thành 1000 dame!)
            HashSet<PlayerController> hitPlayers = new HashSet<PlayerController>();
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null) continue;
                PlayerController p = cols[i].GetComponentInParent<PlayerController>();
                if (p != null && p.Alive && (owner == null || p.Team != owner.Team))
                {
                    if (hitPlayers.Add(p))
                    {
                        int finalDmg = CalculateVolleyDamage(p);
                        p.Stats.TakeDamage(new DamageInfo(owner, finalDmg, false, transform.position));
                    }
                }
            }

            Despawn();
        }

        private int CalculateVolleyDamage(PlayerController p)
        {
            float now = Time.time;
            if (recentHitTimes.TryGetValue(p, out float lastTime) && (now - lastTime < 0.4f))
            {
                recentHitCounts.TryGetValue(p, out int hitCount);
                hitCount++;
                recentHitCounts[p] = hitCount;
                recentHitTimes[p] = now;

                // Các quả bom tiếp theo trong cùng 1 chùm nổ (0.4s) giảm bớt để tránh dồn sát thương 1-shot chết tươi
                float mult = hitCount == 2 ? 0.5f : 0.25f;
                return Mathf.Max(1, Mathf.RoundToInt(explosionDamage * mult));
            }
            else
            {
                recentHitTimes[p] = now;
                recentHitCounts[p] = 1;
                return explosionDamage;
            }
        }

        public void Despawn()
        {
            StopAllCoroutines();
            if (SummonPool.Instance != null)
            {
                SummonPool.Instance.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
