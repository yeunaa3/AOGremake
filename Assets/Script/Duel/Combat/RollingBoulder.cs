using UnityEngine;

namespace AOG.Duel
{
    public class RollingBoulder : SkillObject
    {
        [Header("Rolling Movement")]
        [Tooltip("Tốc độ lăn ngang trên bản đồ")]
        [SerializeField] private float rollSpeed = 4f;
        [Tooltip("Tự động tính góc quay theo tốc độ lăn và bán kính để vừa lăn vừa xoay tròn tự nhiên, không cần làm animation")]
        [SerializeField] private bool autoRotateWithSpeed = true;
        [Tooltip("Tốc độ xoay tròn thủ công (độ/giây) nếu tắt autoRotateWithSpeed")]
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
                // Mặc định quay mặt và lăn về phía địch dựa vào Aim Target của địch
                float enemyDirection = owner.Face;
                if (owner.Enemy != null)
                {
                    Vector3 enemyAim = owner.Enemy.Aim;
                    float diffX = enemyAim.x - transform.position.x;
                    if (Mathf.Abs(diffX) > 0.05f)
                    {
                        enemyDirection = Mathf.Sign(diffX);
                    }
                }
                direction = enemyDirection;

                Vector3 s = transform.localScale;
                float absX = Mathf.Abs(s.x);
                s.x = (direction >= 0f) ? absX : -absX;
                transform.localScale = s;

                // Căn chỉnh biên nảy tọa độ theo vị trí của người triệu hồi so với đối thủ
                // (Không phụ thuộc vào teamId; nếu xuất hiện bên phải đối thủ thì dùng nửa sân bên phải)
                bool isSpawnedOnRightSide = (owner.Enemy != null && transform.position.x > owner.Enemy.transform.position.x) || transform.position.x > 0f;
                if (isSpawnedOnRightSide)
                {
                    float oldLeft = leftBoundary;
                    float oldRight = rightBoundary;
                    leftBoundary = -oldRight;
                    rightBoundary = -oldLeft;
                    if (leftBoundary > rightBoundary)
                    {
                        float tmp = leftBoundary;
                        leftBoundary = rightBoundary;
                        rightBoundary = tmp;
                    }
                }
            }
            if (visualTransform == null)
            {
                visualTransform = transform.Find("Visual");
                if (visualTransform == null && mainVisual != null)
                    visualTransform = mainVisual.transform;
            }

            // Tự động tắt Animator nếu có để code tự xoay tròn, người dùng không cần phải tạo animation
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = false;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (isDead) return;

            // Nếu đang bị đóng băng (Freeze), dừng toàn bộ lăn và xoay tròn
            if (IsFrozen)
            {
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                return;
            }

            float currentRollSpeed = rollSpeed * SlowMultiplier;

            // Di chuyển lăn ngang (kết hợp trọng lực rơi nếu có Rigidbody)
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(direction * currentRollSpeed, rb.linearVelocity.y);
            }
            else
            {
                transform.position += Vector3.right * (direction * currentRollSpeed * Time.deltaTime);
            }

            // Tự động xoay tròn hình ảnh theo tốc độ di chuyển thực tế (vừa lăn vừa quay tự nhiên)
            if (visualTransform != null)
            {
                float rotSpeed = rollRotationSpeed * SlowMultiplier;
                if (autoRotateWithSpeed)
                {
                    // Công thức vật lý lăn không trượt: omega (độ/giây) = (v / R) * (180 / PI)
                    float radius = 0.39f;
                    CircleCollider2D circle = GetComponent<CircleCollider2D>();
                    if (circle != null)
                    {
                        radius = Mathf.Max(0.1f, circle.radius * Mathf.Abs(transform.lossyScale.x));
                    }
                    rotSpeed = (currentRollSpeed / radius) * Mathf.Rad2Deg;
                }

                if (rotSpeed > 0f)
                {
                    // Xoay trong không gian thế giới (Space.World) quanh trục forward (Z)
                    // Lăn sang phải -> xoay theo chiều kim đồng hồ; Lăn sang trái -> xoay ngược chiều kim đồng hồ
                    visualTransform.Rotate(Vector3.forward, -direction * rotSpeed * Time.deltaTime, Space.World);
                }
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

        public override void TakeDamage(int amount, PlayerController attacker = null, Vector2 hitDirection = default,
            ArrowEffect effect = ArrowEffect.None, int effectPower = 0, float effectDuration = 0f)
        {
            base.TakeDamage(amount, attacker, hitDirection, effect, effectPower, effectDuration);

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

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            base.OnTriggerEnter2D(other);
            CheckBoundaryCollision(other);
            TryDealContactDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckBoundaryCollision(other);
            TryDealContactDamage(other);
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            base.OnCollisionEnter2D(collision);
            CheckBoundaryCollision(collision.collider);
            TryDealContactDamage(collision.collider);
        }

        protected void OnCollisionStay2D(Collision2D collision)
        {
            CheckBoundaryCollision(collision.collider);
            TryDealContactDamage(collision.collider);
        }

        private void CheckBoundaryCollision(Collider2D other)
        {
            if (Time.time < lastBounceTime + bounceCooldown) return;
            if (other == null) return;

            if (IsBoundary(other))
            {
                ReverseDirection();
            }
        }

        private bool IsBoundary(Collider2D other)
        {
            if (other == null) return false;
            string colName = other.gameObject.name.ToLower();
            string tag = other.gameObject.tag.ToLower();
            string layerName = LayerMask.LayerToName(other.gameObject.layer).ToLower();

            if (colName.Contains("bound") || colName.Contains("wall") || colName.Contains("fence")
                || tag == "bound" || layerName.Contains("fence") || layerName.Contains("bound"))
            {
                return true;
            }

            if (other.transform.parent != null)
            {
                string pName = other.transform.parent.name.ToLower();
                if (pName.Contains("bound") || pName.Contains("wall") || pName.Contains("fence"))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReverseDirection()
        {
            direction = -direction;
            lastBounceTime = Time.time;

            // Đổi hướng nhìn: tự scale x * với âm 1 theo yêu cầu người chơi
            Vector3 s = transform.localScale;
            s.x = -s.x;
            transform.localScale = s;
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
