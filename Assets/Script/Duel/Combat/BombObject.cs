using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class BombObject : MonoBehaviour
    {
        [Header("Cài đặt Bom")]
        [Tooltip("Thời gian đếm ngược trước khi phát nổ (giây, mặc định 1s theo yêu cầu).")]
        [SerializeField, Min(0.1f)] private float delay = 1f;
        [Tooltip("Sát thương vụ nổ gây ra.")]
        [SerializeField, Min(1)] private int damage = 40;
        [Tooltip("Hiệu ứng kèm theo khi nổ (None, Burn, Stun, Boom...).")]
        [SerializeField] private ArrowEffect effect = ArrowEffect.None;
        [Tooltip("Chỉ số hiệu ứng (sát thương đốt / thời gian stun...).")]
        [SerializeField, Min(0)] private int effectPower = 0;
        [Tooltip("Thời gian hiệu lực của hiệu ứng.")]
        [SerializeField, Min(0f)] private float effectDuration = 0f;
        [Tooltip("Thời gian tồn tại tối đa trên mặt đất nếu không ai giẫm phải (giây).")]
        [SerializeField, Min(1f)] private float groundLifetime = 20f;

        [Header("Collider Vùng Nổ & Trigger")]
        [Tooltip("Collider trên Prefab để xác định phạm vi nổ. Tự động lấy Collider2D trên đối tượng nếu không kéo vào.")]
        [SerializeField] private Collider2D damageCollider;

        [Header("Bộ Lọc Layer Kích Hoạt")]
        [Tooltip("Chỉ kích hoạt bom khi chạm trúng Layer Người (Character) hoặc Totem.")]
        [SerializeField] private bool triggerOnlyOnCharacterOrTotem = true;
        [Tooltip("Các layer có thể kích hoạt bom (mặc định Character và Totem).")]
        [SerializeField] private LayerMask targetLayers;

        [Header("Hiệu ứng hình ảnh & âm thanh")]
        [Tooltip("Prefab hiệu ứng vụ nổ (particle khói/lửa).")]
        [SerializeField] private GameObject explosionParticlePrefab;
        [Tooltip("Prefab hiệu ứng khói mặt đất bổ sung.")]
        [SerializeField] private GameObject groundSmokePrefab;
        [Tooltip("Âm thanh vụ nổ tùy chọn.")]
        [SerializeField] private AudioClip explosionSound;

        private PlayerController owner;
        private Transform attachedTarget;
        private Vector3 attachOffset;
        private bool isCountingDown;
        private bool hasExploded;
        private float lifeTimer;
        private Animator animator;

        public float Delay { get => delay; set => delay = Mathf.Max(0.05f, value); }
        public int Damage { get => damage; set => damage = Mathf.Max(1, value); }
        public Collider2D DamageCollider => damageCollider;
        public bool IsCountingDown => isCountingDown;
        public bool HasExploded => hasExploded;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (damageCollider == null)
            {
                damageCollider = GetComponent<Collider2D>();
            }
            EnsureTriggerColliders();
            InitTargetLayers();
            lifeTimer = groundLifetime;
        }

        private void OnValidate()
        {
            EnsureTriggerColliders();
        }

        private void InitTargetLayers()
        {
            if (targetLayers.value == 0)
            {
                int charLayer = LayerMask.NameToLayer("Character");
                int totemLayer = LayerMask.NameToLayer("Totem");
                if (charLayer >= 0) targetLayers |= (1 << charLayer);
                if (totemLayer >= 0) targetLayers |= (1 << totemLayer);
            }
        }

        /// <summary>
        /// Đảm bảo tất cả Collider2D đều là Trigger: người chơi chạm vào kích hoạt trigger chứ không đi lên/đứng lên nó.
        /// </summary>
        public void EnsureTriggerColliders()
        {
            Collider2D[] allCols = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < allCols.Length; i++)
            {
                if (allCols[i] != null && !allCols[i].isTrigger)
                {
                    allCols[i].isTrigger = true;
                }
            }
        }

        public void Setup(PlayerController source, Transform hitTarget = null)
        {
            owner = source;
            hasExploded = false;
            isCountingDown = false;
            lifeTimer = groundLifetime;

            EnsureTriggerColliders();
            InitTargetLayers();

            if (hitTarget != null)
            {
                // Kiểm tra mục tiêu có thuộc layer Người hoặc Totem hay không
                if (!triggerOnlyOnCharacterOrTotem || IsTargetTransform(hitTarget))
                {
                    Activate(hitTarget);
                }
                else
                {
                    attachedTarget = null;
                }
            }
            else
            {
                // Bắn trúng đất hoặc spawn tự do: Bom nằm chờ ở dạng Trigger, KHÔNG tự nổ cho đến khi chạm Người hoặc Totem!
                attachedTarget = null;
            }
        }

        private void Update()
        {
            if (attachedTarget != null)
            {
                transform.position = attachedTarget.position + attachOffset;
            }

            if (!isCountingDown && !hasExploded)
            {
                lifeTimer -= Time.deltaTime;
                if (lifeTimer <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void HandleContact(Collider2D other)
        {
            if (isCountingDown || hasExploded || other == null) return;

            if (IsTargetCollider(other, out Transform targetRoot))
            {
                Activate(targetRoot);
            }
        }

        /// <summary>
        /// Kiểm tra collider chạm vào có phải là Layer Người (Character) hoặc Totem hợp lệ (khác team).
        /// </summary>
        private bool IsTargetCollider(Collider2D other, out Transform targetRoot)
        {
            targetRoot = null;
            if (other == null) return false;

            // 1. Kiểm tra Người (PlayerController / Character)
            PlayerController targetPlayer = other.GetComponentInParent<PlayerController>();
            if (targetPlayer != null && targetPlayer.Alive)
            {
                if (owner == null || targetPlayer.Team != owner.Team)
                {
                    targetRoot = targetPlayer.transform;
                    return true;
                }
                return false; // Bỏ qua đồng minh
            }

            // 2. Kiểm tra Totem / Vật cản kỹ năng (SkillObject)
            SkillObject obstacle = other.GetComponentInParent<SkillObject>();
            if (obstacle != null && !obstacle.IsDead)
            {
                if (owner == null || obstacle.Team != owner.Team)
                {
                    targetRoot = obstacle.transform;
                    return true;
                }
                return false; // Bỏ qua vật cản đồng minh
            }

            // 3. Kiểm tra theo Layer Name hoặc LayerMask
            int layerBit = 1 << other.gameObject.layer;
            string layerName = LayerMask.LayerToName(other.gameObject.layer);
            bool isCharLayer = string.Equals(layerName, "Character", System.StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(layerName, "Player", System.StringComparison.OrdinalIgnoreCase);
            bool isTotemLayer = string.Equals(layerName, "Totem", System.StringComparison.OrdinalIgnoreCase);

            if (isCharLayer || isTotemLayer || ((targetLayers.value & layerBit) != 0))
            {
                targetRoot = other.transform;
                return true;
            }

            return false;
        }

        private bool IsTargetTransform(Transform target)
        {
            if (target == null) return false;
            PlayerController player = target.GetComponentInParent<PlayerController>();
            if (player != null) return owner == null || player.Team != owner.Team;

            SkillObject obstacle = target.GetComponentInParent<SkillObject>();
            if (obstacle != null) return owner == null || obstacle.Team != owner.Team;

            string layerName = LayerMask.LayerToName(target.gameObject.layer);
            return string.Equals(layerName, "Character", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(layerName, "Totem", System.StringComparison.OrdinalIgnoreCase);
        }

        public void Activate(Transform target = null)
        {
            if (isCountingDown || hasExploded) return;
            isCountingDown = true;

            if (target != null)
            {
                attachedTarget = target;
                attachOffset = transform.position - target.position;
            }

            // Âm thanh khi bom kích hoạt hẹn giờ
            SkillAudioManager.PlayMine();

            StartCoroutine(ExplodeCountdownRoutine());
        }

        private IEnumerator ExplodeCountdownRoutine()
        {
            // Chạy animation nhấp nháy đèn báo hiệu
            if (animator != null)
            {
                animator.Play("MineOnArrow", 0, 0f);
            }

            // Đếm ngược delay (mặc định 1 giây)
            float elapsed = 0f;
            Vector3 baseOffset = attachOffset;
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                // Rung nhẹ ở 0.3s cuối báo hiệu sắp phát nổ
                if (delay - elapsed <= 0.3f)
                {
                    float shake = Mathf.Sin(elapsed * 60f) * 0.04f;
                    if (attachedTarget != null)
                    {
                        attachOffset = baseOffset + new Vector3(shake, 0f, 0f);
                    }
                    else
                    {
                        transform.position += new Vector3(shake * Time.deltaTime * 6f, 0f, 0f);
                    }
                }
                yield return null;
            }

            Explode();
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            Vector3 explodeCenter = transform.position;

            // 1. Phát âm thanh nổ
            SkillAudioManager.PlayBoom();

            // 2. Sinh hiệu ứng khói/lửa nổ
            if (explosionParticlePrefab != null)
            {
                GameObject vfx = Instantiate(explosionParticlePrefab, explodeCenter, Quaternion.identity);
                Destroy(vfx, 2f);
            }
            if (groundSmokePrefab != null)
            {
                GameObject smoke = Instantiate(groundSmokePrefab, explodeCenter, Quaternion.identity);
                Destroy(smoke, 2f);
            }

            // 3. Gây sát thương dựa trên Collider2D của Prefab (không quét bán kính bằng code cứng)
            if (damageCollider == null)
            {
                damageCollider = GetComponent<Collider2D>();
            }

            HashSet<Collider2D> targetColliders = new HashSet<Collider2D>();

            if (damageCollider != null)
            {
                bool prevEnabled = damageCollider.enabled;
                damageCollider.enabled = true;

                ContactFilter2D filter = new ContactFilter2D();
                filter.useTriggers = true;
                filter.SetLayerMask(Physics2D.AllLayers);

                List<Collider2D> overlapResults = new List<Collider2D>();
                Physics2D.OverlapCollider(damageCollider, filter, overlapResults);
                for (int i = 0; i < overlapResults.Count; i++)
                {
                    if (overlapResults[i] != null && overlapResults[i] != damageCollider)
                    {
                        targetColliders.Add(overlapResults[i]);
                    }
                }

                // Hỗ trợ bổ sung hình học collider phòng khi vật lý chưa sync frame
                if (damageCollider is CircleCollider2D circle)
                {
                    Vector2 center = (Vector2)damageCollider.transform.TransformPoint(circle.offset);
                    float radius = circle.radius * Mathf.Max(Mathf.Abs(damageCollider.transform.lossyScale.x), Mathf.Abs(damageCollider.transform.lossyScale.y));
                    Collider2D[] circleHits = Physics2D.OverlapCircleAll(center, radius);
                    for (int i = 0; i < circleHits.Length; i++)
                    {
                        if (circleHits[i] != null && circleHits[i] != damageCollider)
                        {
                            targetColliders.Add(circleHits[i]);
                        }
                    }
                }
                else if (damageCollider is BoxCollider2D box)
                {
                    Vector2 center = (Vector2)damageCollider.transform.TransformPoint(box.offset);
                    Vector2 size = Vector2.Scale(box.size, damageCollider.transform.lossyScale);
                    Collider2D[] boxHits = Physics2D.OverlapBoxAll(center, size, damageCollider.transform.eulerAngles.z);
                    for (int i = 0; i < boxHits.Length; i++)
                    {
                        if (boxHits[i] != null && boxHits[i] != damageCollider)
                        {
                            targetColliders.Add(boxHits[i]);
                        }
                    }
                }

                damageCollider.enabled = prevEnabled;
            }
            else
            {
                Collider2D[] fallbackHits = Physics2D.OverlapCircleAll(explodeCenter, 2.5f);
                for (int i = 0; i < fallbackHits.Length; i++)
                {
                    if (fallbackHits[i] != null) targetColliders.Add(fallbackHits[i]);
                }
            }

            HashSet<PlayerController> hitPlayers = new HashSet<PlayerController>();
            HashSet<SkillObject> hitObstacles = new HashSet<SkillObject>();

            foreach (Collider2D col in targetColliders)
            {
                if (col == null) continue;

                PlayerController target = col.GetComponentInParent<PlayerController>();
                if (target != null && target.Alive && !hitPlayers.Contains(target))
                {
                    hitPlayers.Add(target);
                    if (owner == null || target.Team != owner.Team)
                    {
                        target.Stats.TakeDamage(new DamageInfo(owner, damage, false, explodeCenter,
                            effect, effectPower, effectDuration));
                        SkillAudioManager.PlayPlayerHit();
                    }
                    continue;
                }

                SkillObject obstacle = col.GetComponentInParent<SkillObject>();
                if (obstacle != null && !obstacle.IsDead && !hitObstacles.Contains(obstacle))
                {
                    hitObstacles.Add(obstacle);
                    if (owner == null || obstacle.Team != owner.Team)
                    {
                        Vector2 hitDir = ((Vector2)obstacle.transform.position - (Vector2)explodeCenter).normalized;
                        obstacle.TakeDamage(damage, owner, hitDir, effect, effectPower, effectDuration);
                    }
                }
            }

            // 4. Hủy vật thể bom
            Destroy(gameObject);
        }
    }
}
