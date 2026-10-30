using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    public class SkillObject : MonoBehaviour
    {
        [Header("Health & Decay")]
        [SerializeField, Min(1)] protected int maxHp = 100;
        [Tooltip("Phần trăm máu tối đa mất đi mỗi giây (ví dụ: 10 nghĩa là mất 10% max HP/giây -> tự mất sau 10s nếu không nhận thêm sát thương)")]
        [SerializeField, Range(0f, 100f)] protected float decayPercentPerSecond = 10f;
        [Tooltip("Giới hạn thời gian sống tối đa (giây). 0 = chỉ phụ thuộc vào decay máu.")]
        [SerializeField, Min(0f)] protected float maxLifetime = 0f;

        [Header("Thanh Máu Mini (World-Space Mini Health Bar)")]
        [Tooltip("Thanh máu mini gắn trực tiếp trong Prefab của vật thể để người dùng tự do căn chỉnh")]
        [SerializeField] protected MiniHealthBar miniHealthBar;

        [Header("Hiệu Ứng Trạng Thái (Status Effects Visuals & Particles)")]
        [Tooltip("Prefab hiệu ứng lửa khi bị thiêu đốt (nếu để trống sẽ tự lấy từ Resources hoặc Prefab)")]
        [SerializeField] protected ParticleSystem burnParticlesPrefab;
        [Tooltip("Prefab hiệu ứng độc khi bị trúng tên độc")]
        [SerializeField] protected ParticleSystem poisonParticlesPrefab;
        [Tooltip("Prefab hiệu ứng tuyết/băng khi bị trúng tên đóng băng")]
        [SerializeField] protected ParticleSystem freezeParticlesPrefab;

        [Header("Effects & Feedback")]
        [SerializeField] protected ParticleSystem deathEffectPrefab;
        [SerializeField] protected SpriteRenderer mainVisual;
        [Tooltip("Màu chớp đỏ khi bị bắn trúng (giữ đỏ đỏ)")]
        [SerializeField] protected Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
        [Tooltip("Độ mạnh giật lùi (jolt) khi bị trúng tên")]
        [SerializeField, Min(0f)] protected float hitJoltDistance = 0.12f;
        [Tooltip("Thời gian hiệu ứng giật và chớp đỏ (giây)")]
        [SerializeField, Min(0.02f)] protected float hitDuration = 0.1f;

        [Header("Vật Lý & Rơi Từ Trên Trời")]
        [Tooltip("Bật để vật thể rơi từ trên trời xuống đất khi xuất hiện")]
        [SerializeField] protected bool enableFallPhysics = true;
        [Tooltip("Độ cao xuất hiện phía trên điểm triệu hồi để rơi xuống đất")]
        [SerializeField] protected float dropHeight = 5f;
        [Tooltip("Độ mạnh trọng lực khi rơi (Gravity Scale)")]
        [SerializeField] protected float fallGravity = 3f;
        [Tooltip("Vận tốc rơi ban đầu")]
        [SerializeField] protected float initialFallSpeed = 2f;
        [Tooltip("Layer mặt đất để xác định tiếp đất")]
        [SerializeField] protected LayerMask groundLayers = 1 << 8;
        [Tooltip("Hiệu ứng bụi/chấn động khi tiếp đất")]
        [SerializeField] protected ParticleSystem landEffectPrefab;

        protected PlayerController owner;
        protected float currentHp;
        protected float lifetimeTimer;
        protected bool isDead;
        protected Rigidbody2D rb;
        protected Collider2D col;
        protected bool hasLanded;

        // Trạng thái hiệu ứng mũi tên (Burn, Poison, Freeze, Slow)
        protected float freezeTime;
        protected float burnTime;
        protected float burnTick;
        protected int burnDamage;
        protected PlayerController burnAttacker;
        protected float poisonTime;
        protected float poisonTick;
        protected int poisonDamage;
        protected PlayerController poisonAttacker;
        protected float slowTime;
        protected float slowMultiplier = 1f;

        protected ParticleSystem activeBurnParticles;
        protected ParticleSystem activePoisonParticles;
        protected ParticleSystem activeFreezeParticles;

        private SpriteRenderer[] allRenderers;
        private Color[] originalColors;
        private Coroutine hitReactionRoutine;
        protected Vector3 initialBaseScale;

        public PlayerController Owner => owner;
        public int Team => owner == null ? -1 : owner.Team;
        public int MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public bool IsDead => isDead;
        public bool HasLanded => hasLanded;
        public bool IsFrozen => freezeTime > 0f;
        public bool IsBurning => burnTime > 0f;
        public bool IsPoisoned => poisonTime > 0f;
        public bool IsSlowed => slowTime > 0f;
        public float SlowMultiplier => IsSlowed ? slowMultiplier : 1f;
        public MiniHealthBar MiniHealthBar => miniHealthBar;

        protected virtual void Awake()
        {
            if (mainVisual == null)
                mainVisual = GetComponentInChildren<SpriteRenderer>();

            if (miniHealthBar == null)
                miniHealthBar = GetComponentInChildren<MiniHealthBar>(true);

            CacheVisualRenderers();

            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            initialBaseScale = transform.localScale;
        }

        private void CacheVisualRenderers()
        {
            SpriteRenderer[] found = GetComponentsInChildren<SpriteRenderer>(true);
            if (found != null && found.Length > 0)
            {
                var list = new List<SpriteRenderer>();
                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] == null) continue;
                    // Bỏ qua renderer thuộc thanh máu mini để thanh máu không bị chớp đỏ hoặc đổi màu hiệu ứng theo thân
                    if (miniHealthBar != null && found[i].transform.IsChildOf(miniHealthBar.transform)) continue;
                    list.Add(found[i]);
                }

                allRenderers = list.ToArray();
                originalColors = new Color[allRenderers.Length];
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    originalColors[i] = allRenderers[i] != null ? allRenderers[i].color : Color.white;
                }
            }
        }

        protected void UpdateHealthBar()
        {
            if (miniHealthBar != null)
            {
                miniHealthBar.SetHealth(currentHp, maxHp);
            }
        }

        public virtual void Setup(PlayerController source)
        {
            owner = source;
            currentHp = maxHp;
            lifetimeTimer = 0f;
            isDead = false;
            hasLanded = false;

            // Mặc định prefab khi triệu hồi phải quay về phía địch dựa vào Aim Target của địch
            OrientTowardsEnemy();

            freezeTime = 0f;
            burnTime = 0f;
            poisonTime = 0f;
            slowTime = 0f;
            slowMultiplier = 1f;

            if (miniHealthBar != null)
            {
                miniHealthBar.SetInstantHealth(currentHp, maxHp);
            }

            if (enableFallPhysics)
            {
                // Nâng lên vị trí trên cao để rơi xuống
                transform.position += Vector3.up * dropHeight;

                if (rb == null) rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = fallGravity;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    rb.linearVelocity = Vector2.down * initialFallSpeed;
                }

                if (col == null) col = GetComponent<Collider2D>();
                if (col != null && !(this is RollingBoulder))
                {
                    col.isTrigger = false; // Tạm thời để va chạm vật lý với mặt đất
                }
            }
            else
            {
                hasLanded = true;
            }
        }

        /// <summary>
        /// Mặc định quay mặt về phía địch dựa vào Aim Target của địch
        /// </summary>
        public void OrientTowardsEnemy()
        {
            if (this is RollingBoulder) return;
            if (owner == null) return;

            float enemyDirection = owner.Face;
            if (owner.Enemy != null)
            {
                Vector3 enemyTarget = owner.Enemy.Aim;
                float diffX = enemyTarget.x - transform.position.x;
                if (Mathf.Abs(diffX) > 0.05f)
                {
                    enemyDirection = Mathf.Sign(diffX);
                }
            }

            Vector3 s = transform.localScale;
            float absX = Mathf.Abs(s.x);
            s.x = (enemyDirection >= 0f) ? absX : -absX;
            transform.localScale = s;
        }

        protected virtual void Update()
        {
            if (isDead) return;

            // 1. Tự giảm % máu tối đa theo thời gian (Decay)
            if (decayPercentPerSecond > 0f)
            {
                float loss = (maxHp * (decayPercentPerSecond / 100f)) * Time.deltaTime;
                currentHp -= loss;
                UpdateHealthBar();
                if (currentHp <= 0f)
                {
                    Die();
                    return;
                }
            }

            // 2. Giới hạn thời gian sống dự phòng
            if (maxLifetime > 0f)
            {
                lifetimeTimer += Time.deltaTime;
                if (lifetimeTimer >= maxLifetime)
                {
                    Die();
                    return;
                }
            }

            // 3. Xử lý hiệu ứng Đốt Cháy (Burn)
            if (burnTime > 0f)
            {
                burnTime -= Time.deltaTime;
                burnTick -= Time.deltaTime;
                if (burnTick <= 0f)
                {
                    burnTick += 1f;
                    int dmg = burnDamage > 0 ? burnDamage : 5;
                    ApplyDotDamage(dmg, burnAttacker);
                    if (isDead) return;
                }
            }

            // 4. Xử lý hiệu ứng Nhiễm Độc (Poison)
            if (poisonTime > 0f)
            {
                poisonTime -= Time.deltaTime;
                poisonTick -= Time.deltaTime;
                if (poisonTick <= 0f)
                {
                    poisonTick += 1f;
                    int dmg = poisonDamage > 0 ? poisonDamage : 4;
                    ApplyDotDamage(dmg, poisonAttacker);
                    if (isDead) return;
                }
            }

            // 5. Giảm thời gian Đóng Băng (Freeze) và Làm Chậm (Slow)
            if (freezeTime > 0f)
            {
                freezeTime = Mathf.Max(0f, freezeTime - Time.deltaTime);
            }
            if (slowTime > 0f)
            {
                slowTime = Mathf.Max(0f, slowTime - Time.deltaTime);
            }

            // 6. Cập nhật hiệu ứng hạt và màu sắc trạng thái
            UpdateStatusParticles();
            UpdateVisualTint();
        }

        protected void ApplyDotDamage(int amount, PlayerController attacker)
        {
            if (isDead || amount <= 0) return;
            currentHp -= amount;
            UpdateHealthBar();
            if (currentHp <= 0f)
            {
                Die();
            }
        }

        public virtual void TakeDamage(int amount, PlayerController attacker = null, Vector2 hitDirection = default,
            ArrowEffect effect = ArrowEffect.None, int effectPower = 0, float effectDuration = 0f)
        {
            if (isDead || amount <= 0) return;
            currentHp -= amount;
            UpdateHealthBar();

            if (hitDirection == default && attacker != null)
            {
                hitDirection = (transform.position - attacker.transform.position).normalized;
            }
            if (hitDirection == default)
            {
                hitDirection = Vector2.right * (attacker != null ? attacker.Face : 1f);
            }

            PlayHitReaction(hitDirection);

            if (effect != ArrowEffect.None)
            {
                ApplyStatus(attacker, effect, effectPower, effectDuration);
            }

            if (currentHp <= 0f)
            {
                Die();
            }
        }

        public void TakeDamage(int amount) => TakeDamage(amount, null, default, ArrowEffect.None, 0, 0f);

        public virtual void ApplyStatus(PlayerController attacker, ArrowEffect effect, int power, float duration)
        {
            if (isDead || effect == ArrowEffect.None) return;

            switch (effect)
            {
                case ArrowEffect.Burn:
                    burnDamage = Mathf.Max(burnDamage, power > 0 ? power : 5);
                    burnTime = Mathf.Max(burnTime, duration > 0f ? duration : 3f);
                    burnTick = 1f;
                    burnAttacker = attacker;
                    break;

                case ArrowEffect.Poison:
                    poisonDamage += (power > 0 ? power : 3);
                    poisonTime = Mathf.Max(poisonTime, duration > 0f ? duration : 4f);
                    poisonTick = 1f;
                    poisonAttacker = attacker;
                    break;

                case ArrowEffect.Freeze:
                    freezeTime = Mathf.Max(freezeTime, duration > 0f ? duration : 2f);
                    break;

                case ArrowEffect.Slow:
                    slowMultiplier = Mathf.Clamp(power > 0 ? power / 100f : 0.5f, 0.05f, 1f);
                    slowTime = Mathf.Max(slowTime, duration > 0f ? duration : 2.5f);
                    break;

                case ArrowEffect.Stun:
                case ArrowEffect.KnockUp:
                    freezeTime = Mathf.Max(freezeTime, duration > 0f ? duration : 1.5f);
                    break;

                case ArrowEffect.Boom:
                    if (power > 0)
                    {
                        TakeDamage(power, attacker, Vector2.zero, ArrowEffect.None);
                    }
                    break;
            }
        }

        public virtual void Heal(int amount)
        {
            if (isDead || amount <= 0) return;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            UpdateHealthBar();
        }

        public virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            CleanupStatusParticles();

            if (deathEffectPrefab != null)
            {
                ParticleSystem ps = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 1.5f);
            }

            if (SummonPool.Instance != null)
            {
                SummonPool.Instance.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected void PlayHitReaction(Vector2 hitDirection)
        {
            if (!gameObject.activeInHierarchy) return;
            if (allRenderers == null || allRenderers.Length == 0) CacheVisualRenderers();

            if (hitReactionRoutine != null)
            {
                StopCoroutine(hitReactionRoutine);
                UpdateVisualTint();
            }
            hitReactionRoutine = StartCoroutine(HitReactionRoutine(hitDirection));
        }

        private IEnumerator HitReactionRoutine(Vector2 hitDirection)
        {
            // 1. Chớp đỏ toàn bộ hình ảnh của vật thể triệu hồi
            if (allRenderers != null)
            {
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    if (allRenderers[i] != null)
                    {
                        allRenderers[i].color = hitFlashColor;
                    }
                }
            }

            // 2. Hiệu ứng giật 1 cái (Jolt & Squash/Stretch)
            bool isBoulder = this is RollingBoulder;
            Vector3 joltVec = (Vector3)(hitDirection.normalized) * hitJoltDistance;
            joltVec.y += hitJoltDistance * 0.2f;

            transform.position += joltVec;

            if (!isBoulder)
            {
                float signX = Mathf.Sign(transform.localScale.x);
                transform.localScale = new Vector3(signX * Mathf.Abs(initialBaseScale.x) * 1.12f, initialBaseScale.y * 0.88f, initialBaseScale.z);
            }

            float halfTime = hitDuration * 0.5f;
            yield return new WaitForSeconds(halfTime);

            // Thu hồi vị trí giật
            transform.position -= joltVec;

            // Khôi phục scale chuẩn
            if (!isBoulder)
            {
                float currentSignX = Mathf.Sign(transform.localScale.x);
                transform.localScale = new Vector3(currentSignX * Mathf.Abs(initialBaseScale.x), initialBaseScale.y, initialBaseScale.z);
            }

            hitReactionRoutine = null;

            // Phục hồi lại màu sắc trạng thái hoặc ban đầu
            UpdateVisualTint();
        }

        protected virtual void UpdateVisualTint()
        {
            if (hitReactionRoutine != null) return; // Đang chớp đỏ khi trúng tên, ưu tiên hiệu ứng giật đỏ
            if (allRenderers == null || originalColors == null) return;

            Color tint = Color.white;
            bool applyTint = false;

            if (IsFrozen)
            {
                tint = new Color(0.45f, 0.8f, 1f, 1f); // Xanh băng tuyết
                applyTint = true;
            }
            else if (IsBurning)
            {
                tint = new Color(1f, 0.5f, 0.15f, 1f); // Cam lửa cháy
                applyTint = true;
            }
            else if (IsPoisoned)
            {
                tint = new Color(0.35f, 0.95f, 0.35f, 1f); // Xanh độc tố
                applyTint = true;
            }

            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == null || i >= originalColors.Length) continue;

                if (applyTint)
                {
                    allRenderers[i].color = Color.Lerp(originalColors[i], tint, 0.65f);
                }
                else
                {
                    allRenderers[i].color = originalColors[i];
                }
            }
        }

        private void ResetColors()
        {
            if (allRenderers != null && originalColors != null)
            {
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    if (allRenderers[i] != null && i < originalColors.Length)
                    {
                        allRenderers[i].color = originalColors[i];
                    }
                }
            }
        }

        protected void UpdateStatusParticles()
        {
            // 1. Burn particles
            UpdateParticleInstance(ref activeBurnParticles, burnParticlesPrefab, IsBurning, new Vector3(0f, 0.2f, 0f));

            // 2. Poison particles
            UpdateParticleInstance(ref activePoisonParticles, poisonParticlesPrefab, IsPoisoned, new Vector3(0f, 0.3f, 0f));

            // 3. Freeze particles
            UpdateParticleInstance(ref activeFreezeParticles, freezeParticlesPrefab, IsFrozen, new Vector3(0f, 0.2f, 0f));
        }

        private void UpdateParticleInstance(ref ParticleSystem activeInstance, ParticleSystem prefab, bool shouldPlay, Vector3 offset)
        {
            if (shouldPlay)
            {
                if (activeInstance == null && prefab != null)
                {
                    activeInstance = Instantiate(prefab, transform.position + offset, Quaternion.identity);
                    foreach (ParticleSystem ps in activeInstance.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = ps.main;
                        main.playOnAwake = false;
                        main.stopAction = ParticleSystemStopAction.None;
                    }
                }

                if (activeInstance != null)
                {
                    activeInstance.transform.position = transform.position + offset;
                    activeInstance.transform.rotation = Quaternion.identity;
                    activeInstance.transform.localScale = Vector3.one;

                    if (!activeInstance.isPlaying)
                    {
                        activeInstance.Play(true);
                    }
                }
            }
            else
            {
                if (activeInstance != null && activeInstance.isPlaying)
                {
                    activeInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void CleanupStatusParticles()
        {
            if (activeBurnParticles != null)
            {
                Destroy(activeBurnParticles.gameObject);
                activeBurnParticles = null;
            }
            if (activePoisonParticles != null)
            {
                Destroy(activePoisonParticles.gameObject);
                activePoisonParticles = null;
            }
            if (activeFreezeParticles != null)
            {
                Destroy(activeFreezeParticles.gameObject);
                activeFreezeParticles = null;
            }
        }

        protected virtual void OnDisable()
        {
            if (hitReactionRoutine != null)
            {
                StopCoroutine(hitReactionRoutine);
                hitReactionRoutine = null;
                ResetColors();
            }
            if (initialBaseScale != Vector3.zero)
            {
                float currentSignX = Mathf.Sign(transform.localScale.x);
                transform.localScale = new Vector3(currentSignX * Mathf.Abs(initialBaseScale.x), initialBaseScale.y, initialBaseScale.z);
            }
            CleanupStatusParticles();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }


        }

        protected virtual void OnDestroy()
        {
            CleanupStatusParticles();
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (!hasLanded && IsGround(collision.collider))
            {
                Land();
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (!hasLanded && IsGround(other))
            {
                Land();
            }
        }

        protected virtual void Land()
        {
            hasLanded = true;
            if (rb != null)
            {
                if (!(this is RollingBoulder))
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                }
            }

            if (col == null) col = GetComponent<Collider2D>();
            if (col != null && !(this is RollingBoulder))
            {
                col.isTrigger = true; // Chuyển thành trigger sau khi đã tiếp đất
            }

            if (landEffectPrefab != null)
            {
                ParticleSystem ps = Instantiate(landEffectPrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 1.5f);
            }
        }

        protected bool IsGround(Collider2D other)
        {
            if (other == null) return false;
            int layerBit = 1 << other.gameObject.layer;
            if ((groundLayers.value & layerBit) != 0) return true;
            if (other.CompareTag("Ground")) return true;
            return string.Equals(LayerMask.LayerToName(other.gameObject.layer), "Ground",
                System.StringComparison.OrdinalIgnoreCase);
        }
    }

}
