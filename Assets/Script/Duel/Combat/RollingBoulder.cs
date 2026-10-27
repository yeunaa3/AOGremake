using UnityEngine;

namespace AOG.Duel
{
    public class RollingBoulder : SkillObject
    {
        [Header("Rolling Movement")]
        [Tooltip("Tốc độ lăn ngang trên bản đồ")]
        [SerializeField] private float rollSpeed = 4f;
        [Tooltip("Tốc độ xoay tròn của hình ảnh khi lăn (độ/giây)")]
        [SerializeField] private float rollRotationSpeed = 360f;
        [Tooltip("Phần hình ảnh sẽ xoay tròn")]
        [SerializeField] private Transform visualTransform;

        [Header("Boundary Bouncing")]
        [Tooltip("Bật giới hạn tọa độ để tự động quay đầu khi chạm biên bản đồ")]
        [SerializeField] private bool useCoordinateBoundary = true;
        [SerializeField] private float leftBoundary = -7.5f;
        [SerializeField] private float rightBoundary = 7.5f;
        [SerializeField, Min(0.05f)] private float bounceCooldown = 0.2f;

        [Header("Damage Reflection")]
        [Tooltip("Phần trăm sát thương phản lại cho kẻ tấn công (ví dụ 50 = phản 50% damage đối thủ gây ra)")]
        [SerializeField, Range(0f, 200f)] private float reflectDamagePercent = 50f;
        [Tooltip("Hiệu ứng khi phản sát thương")]
        [SerializeField] private ParticleSystem reflectEffectPrefab;

        [Header("Contact Damage")]
        [Tooltip("Sát thương gây ra khi lăn trúng đối thủ")]
        [SerializeField, Min(0)] private int contactDamage = 15;
        [Tooltip("Khoảng cách thời gian giữa 2 lần gây sát thương va chạm (giây)")]
        [SerializeField, Min(0.1f)] private float contactInterval = 0.5f;

        private float direction = 1f;
        private float lastBounceTime;
        private float nextContactTime;

        public override void Setup(PlayerController source)
        {
            base.Setup(source);
            if (owner != null)
            {
                direction = owner.Face;
            }
            if (visualTransform == null)
            {
                visualTransform = transform.Find("Visual");
                if (visualTransform == null && mainVisual != null)
                    visualTransform = mainVisual.transform;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            // Di chuyển lăn ngang
            transform.position += Vector3.right * (direction * rollSpeed * Time.deltaTime);

            // Xoay tròn hình ảnh
            if (visualTransform != null)
            {
                visualTransform.Rotate(0f, 0f, -direction * rollRotationSpeed * Time.deltaTime);
            }

            // Kiểm tra biên tọa độ
            if (useCoordinateBoundary)
            {
                if (transform.position.x <= leftBoundary && direction < 0f && Time.time >= lastBounceTime + bounceCooldown)
                {
                    ReverseDirection();
                }
                else if (transform.position.x >= rightBoundary && direction > 0f && Time.time >= lastBounceTime + bounceCooldown)
                {
                    ReverseDirection();
                }
            }
        }

        public override void TakeDamage(int amount, PlayerController attacker = null)
        {
            base.TakeDamage(amount, attacker);

            // Phản lại % theo sát thương đối thủ gây ra
            if (attacker != null && attacker.Team != Team && reflectDamagePercent > 0f && amount > 0)
            {
                int reflected = Mathf.RoundToInt(amount * (reflectDamagePercent / 100f));
                if (reflected > 0 && attacker.Stats != null && attacker.Stats.Alive)
                {
                    attacker.Stats.TakeDamage(new DamageInfo(owner, reflected, false, attacker.Aim));
                    if (reflectEffectPrefab != null)
                    {
                        ParticleSystem ps = Instantiate(reflectEffectPrefab, transform.position, Quaternion.identity);
                        ps.Play(true);
                        Destroy(ps.gameObject, 1.5f);
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckBoundaryCollision(other);
            TryDealContactDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealContactDamage(other);
        }

        private void CheckBoundaryCollision(Collider2D other)
        {
            if (Time.time < lastBounceTime + bounceCooldown) return;

            // Kiểm tra va chạm với các vật cản biên
            string colName = other.gameObject.name.ToLower();
            string tag = other.gameObject.tag.ToLower();
            if (colName.Contains("bound") || colName.Contains("wall") || colName.Contains("fence") || tag == "bound")
            {
                ReverseDirection();
            }
        }

        private void ReverseDirection()
        {
            direction = -direction;
            lastBounceTime = Time.time;
        }

        private void TryDealContactDamage(Collider2D other)
        {
            if (contactDamage <= 0 || isDead || owner == null) return;
            if (Time.time < nextContactTime) return;

            PlayerController target = other.GetComponentInParent<PlayerController>();
            if (target != null && target.Team != Team && target.Alive)
            {
                nextContactTime = Time.time + contactInterval;
                target.Stats.TakeDamage(new DamageInfo(owner, contactDamage, false, transform.position));
            }
        }
    }
}
