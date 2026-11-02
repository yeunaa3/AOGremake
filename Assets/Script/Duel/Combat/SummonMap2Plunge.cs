using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Prefab triệu hồi cung Map 2:
    /// Chọn cố định 1 điểm rơi trên không (không bám theo kẻ địch nếu kẻ địch di chuyển),
    /// rung rung rồi phi đâm một phát cắm xuống đất, tạm dừng rồi từ từ rút lên như ban đầu.
    /// Nếu kẻ địch chạm vào nó trong lúc phi xuống, cắm đất hoặc rút lên thì ăn sát thương.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SummonMap2Plunge : MonoBehaviour
    {
        [Header("Sát Thương & Tấn Công")]
        [Tooltip("Lượng sát thương gây ra khi kẻ địch chạm vào")]
        [SerializeField, Min(1)] private int damage = 50;

        [Header("Thời Gian & Vận Tốc")]
        [Tooltip("Độ cao lơ lửng ban đầu trên không (mét)")]
        [SerializeField] private float hoverHeight = 3.6f;
        [Tooltip("Thời gian rung rung trên không trước khi đâm xuống (giây)")]
        [SerializeField, Min(0.1f)] private float hoverDuration = 0.5f;
        [Tooltip("Cường độ rung rung trên không")]
        [SerializeField] private float shakeIntensity = 0.08f;
        [Tooltip("Tốc độ đâm phi lao xuống đất")]
        [SerializeField, Min(1f)] private float plungeSpeed = 28f;
        [Tooltip("Độ cắm sâu của mũi vũ khí vào lòng đất (mét) - chỉ cắm mũi nhọn, không chìm nửa thân")]
        [SerializeField, Min(0.05f)] private float embedDepth = 0.35f;
        [Tooltip("Thời gian cắm trên mặt đất trước khi rút lên (giây)")]
        [SerializeField, Min(0f)] private float groundPauseDuration = 0.25f;
        [Tooltip("Tốc độ từ từ rút lên")]
        [SerializeField, Min(0.5f)] private float retractSpeed = 2.8f;
        [Tooltip("Thời gian rút lên trước khi biến mất")]
        [SerializeField, Min(0.2f)] private float retractDuration = 1.0f;

        [Header("Hiệu Ứng & Vật Lý")]
        [Tooltip("Layer mặt đất")]
        [SerializeField] private LayerMask groundLayers = 1 << 8;
        [Tooltip("Hiệu ứng bụi/chấn động khi cắm xuống đất")]
        [SerializeField] private ParticleSystem impactEffectPrefab;

        private PlayerController owner;
        private Collider2D hitCollider;
        private SpriteRenderer spriteRenderer;
        private bool hasDamagedEnemy;
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

            hitCollider = GetComponent<Collider2D>();
            if (hitCollider != null) hitCollider.isTrigger = true;

            spriteRenderer = GetComponent<SpriteRenderer>();

            StopAllCoroutines();
            StartCoroutine(PlungeSequence());
        }

        private IEnumerator PlungeSequence()
        {
            // --- GIAI ĐOẠN 1: Rung rung tại đúng điểm rơi đã chọn trên không (KHÔNG BÁM THEO) ---
            float hoverElapsed = 0f;
            while (hoverElapsed < hoverDuration)
            {
                hoverElapsed += Time.deltaTime;
                float ox = Random.Range(-shakeIntensity, shakeIntensity);
                float oy = Random.Range(-shakeIntensity, shakeIntensity);
                transform.position = fixedSpawnPos + new Vector3(ox, oy, 0f);
                yield return null;
            }

            transform.position = fixedSpawnPos;

            // --- GIAI ĐOẠN 2: Đâm 1 phát phi cắm xuống đất tại đúng tọa độ X cố định này ---
            float fallbackSurfaceY = (owner != null && owner.Enemy != null) 
                ? owner.Enemy.transform.position.y 
                : (owner != null ? owner.transform.position.y : 0f);
            float targetGroundY = CalculateGroundY(fixedSpawnPos.x, fallbackSurfaceY);
            Vector3 impactPos = new Vector3(fixedSpawnPos.x, targetGroundY, 0f);

            while (Vector3.Distance(transform.position, impactPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, impactPos, plungeSpeed * Time.deltaTime);
                CheckHitEnemy();
                yield return null;
            }

            transform.position = impactPos;

            // Chạm đất: âm thanh chấn động mặt đất + hiệu ứng bụi khói
            SkillAudioManager.PlayEarthquake();
            if (impactEffectPrefab != null)
            {
                ParticleSystem ps = Instantiate(impactEffectPrefab, impactPos, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 2f);
            }

            CheckHitEnemy();

            // --- GIAI ĐOẠN 3: Tạm dừng một chút trên mặt đất ---
            yield return new WaitForSeconds(groundPauseDuration);

            // --- GIAI ĐOẠN 4: Rút từ từ lên như ban đầu và mờ dần ---
            float retractElapsed = 0f;
            Color initColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (retractElapsed < retractDuration)
            {
                retractElapsed += Time.deltaTime;
                transform.position += Vector3.up * (retractSpeed * Time.deltaTime);

                if (spriteRenderer != null)
                {
                    float alpha = Mathf.Clamp01(1f - (retractElapsed / retractDuration));
                    spriteRenderer.color = new Color(initColor.r, initColor.g, initColor.b, alpha);
                }

                // Nếu kẻ địch chạm vào lúc rút lên vẫn dính sát thương
                CheckHitEnemy();

                yield return null;
            }

            Despawn();
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void CheckHitEnemy()
        {
            if (hasDamagedEnemy) return;

            Vector2 boxCenter = transform.position;
            Vector2 boxSize = new Vector2(1.2f, 3.0f);

            if (hitCollider != null)
            {
                boxCenter = hitCollider.bounds.center;
                boxSize = hitCollider.bounds.size;
            }
            else if (spriteRenderer != null)
            {
                boxCenter = spriteRenderer.bounds.center;
                boxSize = spriteRenderer.bounds.size;
            }

            Collider2D[] cols = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
            for (int i = 0; i < cols.Length; i++)
            {
                if (TryDamage(cols[i])) break;
            }
        }

        private bool TryDamage(Collider2D col)
        {
            if (hasDamagedEnemy || col == null) return false;

            PlayerController p = col.GetComponentInParent<PlayerController>();
            if (p != null && p.Alive && (owner == null || p.Team != owner.Team))
            {
                hasDamagedEnemy = true;
                p.Stats.TakeDamage(new DamageInfo(owner, damage, false, transform.position));
                SkillAudioManager.PlayPlayerHit();
                return true;
            }

            return false;
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
            hasDamagedEnemy = false;
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }
        }
    }
}
