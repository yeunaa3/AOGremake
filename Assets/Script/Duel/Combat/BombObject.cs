using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Bom trên mặt đất kích nổ khi kẻ địch hoặc totem giẫm phải, gây sát thương diện rộng sau thời gian đếm ngược.
    /// </summary>
    public sealed class BombObject : MonoBehaviour
    {
        [Header("Cài đặt Bom")]
        [Tooltip("Thời gian đếm ngược trước khi phát nổ (giây).")]
        [SerializeField, Min(0.1f)] private float delay = 1f;
        [Tooltip("Sát thương vụ nổ gây ra.")]
        [SerializeField, Min(1)] private int damage = 86;
        [Tooltip("Thời gian tồn tại tối đa trên sàn nếu không ai giẫm phải (giây).")]
        [SerializeField, Min(1f)] private float groundLifetime = 20f;

        [Header("Vùng Nổ")]
        [Tooltip("Bán kính nổ gây sát thương diện rộng.")]
        [SerializeField, Min(0.1f)] private float explosionRadius = 2.5f;

        [Header("Hiệu ứng & Âm thanh")]
        [SerializeField] private GameObject explosionParticlePrefab;
        [SerializeField] private GameObject groundSmokePrefab;
        [SerializeField] private AudioClip explosionSound;

        private PlayerController owner;
        private bool isCountingDown;
        private bool hasExploded;
        private float lifeTimer;
        private Animator animator;
        private Collider2D bombCollider;

        public float Delay { get => delay; set => delay = Mathf.Max(0.05f, value); }
        public int Damage { get => damage; set => damage = Mathf.Max(1, value); }
        public float ExplosionRadius { get => explosionRadius; set => explosionRadius = Mathf.Max(0.1f, value); }
        public bool IsCountingDown => isCountingDown;
        public bool HasExploded => hasExploded;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            bombCollider = GetComponent<Collider2D>();
            EnsureTriggerColliders();
            lifeTimer = groundLifetime;
        }

        private void OnValidate()
        {
            EnsureTriggerColliders();
        }

        public void EnsureTriggerColliders()
        {
            Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] != null && !allColliders[i].isTrigger)
                {
                    allColliders[i].isTrigger = true;
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

            if (hitTarget != null && IsTargetTransform(hitTarget))
            {
                Activate();
            }
        }

        private void Update()
        {
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

            if (IsTargetCollider(other))
            {
                Activate();
            }
        }

        private bool IsTargetCollider(Collider2D other)
        {
            if (other == null) return false;

            // Kiểm tra người chơi địch
            PlayerController targetPlayer = other.GetComponentInParent<PlayerController>();
            if (targetPlayer != null && targetPlayer.Alive)
            {
                return owner == null || targetPlayer.Team != owner.Team;
            }

            // Kiểm tra Totem / Vật cản địch
            SkillObject obstacle = other.GetComponentInParent<SkillObject>();
            if (obstacle != null && !obstacle.IsDead)
            {
                return owner == null || obstacle.Team != owner.Team;
            }

            // Kiểm tra Layer Character hoặc Totem
            string layerName = LayerMask.LayerToName(other.gameObject.layer);
            bool isCharacterLayer = string.Equals(layerName, "Character", System.StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(layerName, "Player", System.StringComparison.OrdinalIgnoreCase);
            bool isTotemLayer = string.Equals(layerName, "Totem", System.StringComparison.OrdinalIgnoreCase);

            if (isCharacterLayer || isTotemLayer)
            {
                if (owner != null && other.transform.IsChildOf(owner.transform))
                {
                    return false;
                }
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

        public void Activate()
        {
            if (isCountingDown || hasExploded) return;
            isCountingDown = true;

            SkillAudioManager.PlayMine();
            StartCoroutine(ExplodeCountdownRoutine());
        }

        private IEnumerator ExplodeCountdownRoutine()
        {
            if (animator != null)
            {
                animator.Play("MineOnArrow", 0, 0f);
            }

            float elapsedTime = 0f;
            Vector3 originPosition = transform.position;

            while (elapsedTime < delay)
            {
                elapsedTime += Time.deltaTime;
                // Rung nhẹ ở 0.3s cuối báo hiệu phát nổ
                if (delay - elapsedTime <= 0.3f)
                {
                    float shakeOffset = Mathf.Sin(elapsedTime * 60f) * 0.04f;
                    transform.position = originPosition + new Vector3(shakeOffset, 0f, 0f);
                }
                yield return null;
            }

            transform.position = originPosition;
            Explode();
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            Vector3 explodeCenter = transform.position;

            SkillAudioManager.PlayBoom();

            if (explosionParticlePrefab != null)
            {
                GameObject particleEffect = Instantiate(explosionParticlePrefab, explodeCenter, Quaternion.identity);
                Destroy(particleEffect, 2f);
            }
            if (groundSmokePrefab != null)
            {
                GameObject smokeEffect = Instantiate(groundSmokePrefab, explodeCenter, Quaternion.identity);
                Destroy(smokeEffect, 2f);
            }

            HashSet<Collider2D> targetColliders = new HashSet<Collider2D>();

            // Quét mục tiêu trong bán kính nổ
            float effectiveRadius = Mathf.Max(0.1f, explosionRadius);
            Collider2D[] circleHits = Physics2D.OverlapCircleAll(explodeCenter, effectiveRadius);
            for (int i = 0; i < circleHits.Length; i++)
            {
                if (circleHits[i] != null && circleHits[i].gameObject != gameObject)
                {
                    targetColliders.Add(circleHits[i]);
                }
            }

            if (bombCollider != null && bombCollider.gameObject.activeInHierarchy)
            {
                Collider2D[] contactHits = new Collider2D[16];
                ContactFilter2D contactFilter = new ContactFilter2D();
                contactFilter.useTriggers = true;
                int contactCount = bombCollider.Overlap(contactFilter, contactHits);
                for (int i = 0; i < contactCount; i++)
                {
                    if (contactHits[i] != null && contactHits[i].gameObject != gameObject)
                    {
                        targetColliders.Add(contactHits[i]);
                    }
                }
            }

            // Gây sát thương cho mục tiêu
            HashSet<PlayerController> hitPlayers = new HashSet<PlayerController>();
            HashSet<SkillObject> hitObstacles = new HashSet<SkillObject>();

            foreach (Collider2D hitCollider in targetColliders)
            {
                if (hitCollider == null) continue;

                PlayerController targetPlayer = hitCollider.GetComponentInParent<PlayerController>();
                if (targetPlayer != null && !hitPlayers.Contains(targetPlayer))
                {
                    if (owner != null && targetPlayer.Team == owner.Team) continue;

                    hitPlayers.Add(targetPlayer);
                    Vector2 hitPoint = hitCollider.ClosestPoint(explodeCenter);
                    DamageInfo damageInfo = new DamageInfo(owner, damage, false, hitPoint, ArrowEffect.Boom, 0, 0f);
                    targetPlayer.Stats.TakeDamage(damageInfo);
                    continue;
                }

                SkillObject obstacle = hitCollider.GetComponentInParent<SkillObject>();
                if (obstacle != null && !hitObstacles.Contains(obstacle))
                {
                    if (owner != null && obstacle.Team == owner.Team) continue;

                    hitObstacles.Add(obstacle);
                    Vector2 hitDirection = (obstacle.transform.position - explodeCenter).normalized;
                    obstacle.TakeDamage(damage, owner, hitDirection, ArrowEffect.Boom, 0, 0f);
                }
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, explosionRadius));
        }
    }
}
