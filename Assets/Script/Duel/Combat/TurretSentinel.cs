using UnityEngine;

namespace AOG.Duel
{
    public class TurretSentinel : SkillObject
    {
        [Header("Turret Shooting")]
        [Tooltip("Thời gian giữa các lần bắn tên (giây)")]
        [SerializeField, Min(0.1f)] private float shootInterval = 1.2f;
        [Tooltip("Sát thương cố định của mũi tên")]
        [SerializeField, Min(1)] private int damage = 20;
        [Tooltip("Nếu bật, sát thương sẽ tính theo damage của chủ nhân")]
        [SerializeField] private bool useOwnerDamage = false;
        [SerializeField, Min(0f)] private float ownerDamageMultiplier = 1f;
        [Tooltip("Tốc độ bay của mũi tên")]
        [SerializeField, Min(0.1f)] private float arrowSpeedMultiplier = 1f;
        [Tooltip("Quỹ đạo bay của mũi tên")]
        [SerializeField] private ProjectilePath projectilePath = ProjectilePath.NormalArc;
        [SerializeField] private float arc = 0f;
        [Tooltip("Điểm bắn tên ra (nếu không gán sẽ lấy tâm vật thể)")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Mũi tên riêng của trụ (nếu để trống sẽ lấy mũi tên mặc định của chủ nhân)")]
        [SerializeField] private Arrow customArrowPrefab;
        [Tooltip("Hình dáng mũi tên riêng (nếu để trống sẽ lấy từ cung của chủ nhân)")]
        [SerializeField] private Sprite customArrowSprite;
        [Tooltip("Hiệu ứng khói/lóe sáng khi bắn")]
        [SerializeField] private ParticleSystem shootMuzzleEffect;

        private float shootTimer;

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            // Nếu đang bị đóng băng (Freeze), tạm dừng tích lũy và bắn tên
            if (IsFrozen) return;

            shootTimer += Time.deltaTime;
            if (shootTimer >= shootInterval)
            {
                shootTimer = 0f;
                ShootArrow();
            }
        }

        private void ShootArrow()
        {
            if (owner == null || owner.Enemy == null || !owner.Enemy.Alive) return;

            Vector2 start = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            Vector2 target = owner.Enemy.Aim;

            int finalDamage = (useOwnerDamage && owner.Stats != null)
                ? Mathf.RoundToInt(owner.Stats.Damage * ownerDamageMultiplier)
                : damage;

            Sprite sprite = customArrowSprite != null ? customArrowSprite
                : (owner.Stats != null ? owner.Stats.ArrowSprite : null);

            Arrow projectile = null;
            if (owner.Pool != null)
            {
                projectile = owner.Pool.Spawn(customArrowPrefab);
            }
            else if (customArrowPrefab != null)
            {
                projectile = Instantiate(customArrowPrefab);
            }

            if (projectile != null && sprite != null)
            {
                projectile.Launch(owner, start, target, projectilePath, arc, finalDamage, sprite,
                    arrowSpeedMultiplier, ArrowEffect.None, 0, 0f, 0f, null, -1);

                if (shootMuzzleEffect != null)
                {
                    shootMuzzleEffect.Play(true);
                }
            }
        }
    }
}
