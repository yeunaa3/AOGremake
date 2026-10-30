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
        private float initialLeftBoundary;
        private float initialRightBoundary;
        private bool boundaryInitialized;

        protected override void Awake()
        {
            base.Awake();
            if (!boundaryInitialized)
            {
                initialLeftBoundary = leftBoundary;
                initialRightBoundary = rightBoundary;
                boundaryInitialized = true;
            }
            // Đảm bảo kích thước chuẩn 1.8 đồng nhất
            transform.localScale = new Vector3(1.8f, 1.8f, 1f);
        }

        public override void Setup(PlayerController source)
        {
            base.Setup(source);
            if (boundaryInitialized)
            {
                leftBoundary = initialLeftBoundary;
                rightBoundary = initialRightBoundary;
            }
            lastBounceTime = 0f;
            nextContactTime = 0f;

            // Bỏ qua va chạm vật lý giữa con lăn và người triệu hồi (không bị cấn đầu/chân người triệu hồi)
            if (owner != null && col != null)
            {
                foreach (Collider2D ownerCol in owner.GetComponentsInChildren<Collider2D>(true))
                {
                    if (ownerCol != null)
                    {
                        Physics2D.IgnoreCollision(col, ownerCol, true);
                    }
                }
            }

            if (owner != null)
            {
                // Mặc định hướng lăn về phía đối thủ
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

                // Biên nảy tọa độ theo từng hòn đảo để con lăn không bao giờ lăn xuống vực thẳm ở giữa map (x: -1.35 đến +1.35)
                // Đảo bên trái (Player): [-6.8, -1.8]
                // Đảo bên phải (Enemy): [1.8, 6.8]
                bool isRightSide = (owner.Enemy != null && transform.position.x > owner.Enemy.transform.position.x) || transform.position.x > 0f;
                if (isRightSide)
                {
                    leftBoundary = 1.8f;
                    rightBoundary = 6.8f;
                }
                else
                {
                    leftBoundary = -6.8f;
                    rightBoundary = -1.8f;
                }
            }

            // Ép scale luôn là 1.8 đồng nhất, tuyệt đối không bị dẹt méo
            transform.localScale = new Vector3(1.8f, 1.8f, 1f);

            if (visualTransform == null)
            {
                visualTransform = transform.Find("Visual");
                if (visualTransform == null && mainVisual != null)
                    visualTransform = mainVisual.transform;
            }

            if (visualTransform != null)
            {
                visualTransform.localScale = Vector3.one;
                visualTransform.localRotation = Quaternion.identity;
            }

            // Tự động tìm mặt đất ngay bên dưới vị trí triệu hồi để đặt con lăn xuống đất chuẩn xác lập tức,
            // tránh trường hợp rơi từ trên trời xuống gây hiện tượng lăn lơ lửng trên không
            float colRadius = 0.702f;
            CircleCollider2D circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                colRadius = circle.radius * Mathf.Abs(transform.lossyScale.x);
            }

            int mask = groundLayers.value != 0 ? groundLayers.value : (1 << 8);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) mask |= (1 << groundLayer);

            // Bắn raycast từ trên cao xuống tìm mặt đất thực tế bên dưới, bỏ qua caster và trigger
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(transform.position.x, 5f), Vector2.down, 40f, mask);
            bool foundGround = false;
            foreach (RaycastHit2D h in hits)
            {
                if (h.collider != null && !h.collider.isTrigger && IsGround(h.collider))
                {
                    if (owner != null && (h.collider.transform == owner.transform || h.collider.transform.IsChildOf(owner.transform)))
                        continue;

                    transform.position = new Vector3(transform.position.x, h.point.y + colRadius, transform.position.z);
                    hasLanded = true;
                    foundGround = true;
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                    break;
                }
            }

            // Nếu không tìm thấy mặt đất qua raycast (do map setup), đặt về độ cao mặt đất chuẩn của game (-2.35f)
            if (!foundGround)
            {
                transform.position = new Vector3(transform.position.x, -2.35f + colRadius, transform.position.z);
                hasLanded = true;
                if (rb != null) rb.linearVelocity = Vector2.zero;
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

            // Nếu đang rơi chưa chạm đất, chỉ rơi tự do theo trọng lực Y, KHÔNG lăn ngang trên trời
            if (!hasLanded)
            {
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                return;
            }

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
                    // Lăn sang phải (direction = 1) -> xoay theo chiều kim đồng hồ (-Z)
                    // Lăn sang trái (direction = -1) -> xoay ngược chiều kim đồng hồ (+Z)
                    visualTransform.Rotate(Vector3.forward, -direction * rotSpeed * Time.deltaTime, Space.Self);
                }
            }

            // Kiểm tra biên tọa độ và ngăn chặn hoàn toàn việc rớt xuống vực
            if (useCoordinateBoundary)
            {
                if (transform.position.x <= leftBoundary)
                {
                    if (direction < 0f && Time.time >= lastBounceTime + bounceCooldown)
                    {
                        ReverseDirection();
                    }
                    transform.position = new Vector3(Mathf.Max(leftBoundary, transform.position.x), transform.position.y, transform.position.z);
                }
                else if (transform.position.x >= rightBoundary)
                {
                    if (direction > 0f && Time.time >= lastBounceTime + bounceCooldown)
                    {
                        ReverseDirection();
                    }
                    transform.position = new Vector3(Mathf.Min(rightBoundary, transform.position.x), transform.position.y, transform.position.z);
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
            // GIỮ NGUYÊN localScale DƯƠNG! Không đổi dấu transform.localScale.x
            // vì hình tròn xoay Space.World trong parent có scale.x âm sẽ bị biến dạng elip (dẹt)
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
