using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    public enum TrapDropTarget
    {
        Enemy = 0,          // Thả trên đầu đối thủ
        Player = 1,         // Thả trên đầu người chơi (bản thân)
        CurrentPosition = 2 // Thả tại vị trí spawn hiện tại
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class TrapObject : MonoBehaviour
    {
        [Header("Cài Đặt Thả Bẫy (Số Lượng & Vị Trí Rơi)")]
        [Tooltip("Số lượng bẫy được thả mỗi lần")]
        [SerializeField, Min(1)] private int trapCount = 1;
        [Tooltip("Vị trí mục tiêu thả bẫy: Enemy (Đối thủ), Player (Bản thân), CurrentPosition (Vị trí hiện tại)")]
        [SerializeField] private TrapDropTarget dropTarget = TrapDropTarget.Enemy;
        [Tooltip("Độ lệch vị trí rơi (X, Y) so với mục tiêu")]
        [SerializeField] private Vector2 dropOffset = Vector2.zero;
        [Tooltip("Khoảng cách dàn hàng ngang giữa các bẫy nếu số lượng > 1")]
        [SerializeField, Min(0f)] private float spreadDistance = 0.8f;
        [Tooltip("Độ ngẫu nhiên vị trí theo phương ngang")]
        [SerializeField, Min(0f)] private float horizontalRandomness = 0f;

        [Header("Cài Đặt Rơi & Vật Lý (Fall Physics)")]
        [Tooltip("Độ cao xuất hiện từ trên trời khi được thả xuống")]
        [SerializeField] private float dropHeight = 5f;
        [Tooltip("Độ mạnh trọng lực khi rơi (Gravity Scale)")]
        [SerializeField] private float fallGravity = 3f;
        [Tooltip("Vận tốc rơi ban đầu")]
        [SerializeField] private float initialFallSpeed = 2f;
        [Tooltip("Layer mặt đất để bẫy tiếp đất")]
        [SerializeField] private LayerMask groundLayers = 1 << 8;
        [Tooltip("Hiệu ứng bụi/chấn động khi bẫy tiếp đất")]
        [SerializeField] private ParticleSystem landEffectPrefab;

        [Header("Cơ Chế Bẫy & Kích Hoạt (Trap Trigger)")]
        [Tooltip("Thời gian chờ kích hoạt sau khi giẫm phải (giây) - Mặc định 1s theo yêu cầu")]
        [SerializeField, Min(0f)] private float triggerDelay = 1f;
        [Tooltip("Sát thương gây ra cho nạn nhân khi bẫy sập")]
        [SerializeField, Min(0)] private int damage = 25;
        [Tooltip("Thời gian khóa (bất động, không di chuyển/bắn/skill) nạn nhân (giây)")]
        [SerializeField, Min(0f)] private float lockDuration = 2f;
        [Tooltip("Thời gian tồn tại tối đa của bẫy nếu không ai giẫm (giây)")]
        [SerializeField, Min(1f)] private float lifetime = 15f;
        [Tooltip("Bán kính vùng nổ ảnh hưởng khi bẫy kích hoạt")]
        [SerializeField, Min(0.1f)] private float triggerRadius = 1.2f;

        [Header("Hiệu Ứng Bổ Trợ")]
        [Tooltip("Hiệu ứng phụ kèm theo (ví dụ Làm chậm/Slow)")]
        [SerializeField] private ArrowEffect effect = ArrowEffect.Slow;
        [SerializeField, Min(0)] private int effectPower = 30;
        [SerializeField, Min(0f)] private float effectDuration = 2f;
        [Tooltip("Hiệu ứng cảnh báo khi vừa giẫm phải trong 1s đếm ngược")]
        [SerializeField] private ParticleSystem warningEffectPrefab;
        [Tooltip("Hiệu ứng nổ / kẹp khi bẫy kích hoạt")]
        [SerializeField] private ParticleSystem triggerParticlePrefab;

        [Header("Hiệu Ứng Trói Bằng Xích (Chain Binding)")]
        [Tooltip("GameObject chứa hiệu ứng xích trói quản lý bởi Animator")]
        [SerializeField] private GameObject chainEffect;
        [Tooltip("Animator điều khiển bẫy và xích")]
        [SerializeField] private Animator animator;

        private PlayerController owner;
        private Collider2D trapCollider;
        private Rigidbody2D body;
        private bool landed;
        private bool isArming;
        private bool triggered;
        private float lifeTimer;
        [System.NonSerialized] private bool isClusterChild;

        public int TrapCount { get => trapCount; set => trapCount = Mathf.Max(1, value); }
        public TrapDropTarget DropTarget { get => dropTarget; set => dropTarget = value; }
        public Vector2 DropOffset { get => dropOffset; set => dropOffset = value; }
        public float SpreadDistance { get => spreadDistance; set => spreadDistance = Mathf.Max(0f, value); }
        public float HorizontalRandomness { get => horizontalRandomness; set => horizontalRandomness = Mathf.Max(0f, value); }
        public float DropHeight { get => dropHeight; set => dropHeight = value; }
        public bool IsClusterChild { get => isClusterChild; set => isClusterChild = value; }

        public float TriggerDelay { get => triggerDelay; set => triggerDelay = value; }
        public int Damage { get => damage; set => damage = value; }
        public float LockDuration { get => lockDuration; set => lockDuration = value; }
        public float Lifetime { get => lifetime; set => lifetime = value; }
        public float TriggerRadius { get => triggerRadius; set => triggerRadius = value; }

        private void Awake()
        {
            trapCollider = GetComponent<Collider2D>();
            body = GetComponent<Rigidbody2D>();
            lifeTimer = lifetime;
            EnsureTriggerColliders();

            if (animator == null) animator = GetComponent<Animator>();
            if (chainEffect == null)
            {
                Transform t = transform.Find("Chains");
                if (t != null) chainEffect = t.gameObject;
            }
            if (chainEffect != null)
            {
                chainEffect.SetActive(false);
            }
        }

        public void Setup(PlayerController source)
        {
            Setup(source, dropHeight);
        }

        public void Setup(PlayerController source, float customDropHeight)
        {
            owner = source;
            if (source != null && source.Gear != null && source.Gear.BowState != null)
            {
                source.Gear.BowState.RegisterSummon(gameObject);
            }
            lifeTimer = lifetime;
            triggered = false;
            isArming = false;
            landed = false;

            if (chainEffect != null)
            {
                chainEffect.SetActive(false);
            }
            if (animator != null)
            {
                animator.Rebind();
                animator.Play("TrapTrigger", 0, 0f);
            }

            float actualDrop = customDropHeight > 0f ? customDropHeight : dropHeight;
            if (actualDrop <= 0f) actualDrop = 4.5f;

            if (isClusterChild)
            {
                InitFallPhysics();
                return;
            }

            // Xác định tọa độ gốc rơi theo dropTarget (mặc định rơi trên đầu đối thủ)
            Vector3 basePos = transform.position;
            if (dropTarget == TrapDropTarget.Enemy && source != null && source.Enemy != null)
            {
                basePos = source.Enemy.transform.position;
            }
            else if (dropTarget == TrapDropTarget.Player && source != null)
            {
                basePos = source.transform.position;
            }
            else if (source != null && source.Enemy != null)
            {
                basePos = source.Enemy.transform.position;
            }
            // Thêm độ lệch dropOffset
            basePos.x += dropOffset.x;
            basePos.y += dropOffset.y;

            int count = Mathf.Max(1, trapCount);
            if (count <= 1)
            {
                float randX = horizontalRandomness > 0f ? Random.Range(-horizontalRandomness, horizontalRandomness) : 0f;
                transform.position = new Vector3(basePos.x + randX, basePos.y + actualDrop, basePos.z);
                InitFallPhysics();
            }
            else
            {
                float first = -(count - 1) * spreadDistance * 0.5f;
                // Vị trí bẫy thứ nhất (chính đối tượng này)
                float randX0 = horizontalRandomness > 0f ? Random.Range(-horizontalRandomness, horizontalRandomness) : 0f;
                transform.position = new Vector3(basePos.x + first + randX0, basePos.y + actualDrop, basePos.z);
                isClusterChild = true;
                InitFallPhysics();

                // Tạo các bẫy còn lại trong cụm
                for (int i = 1; i < count; i++)
                {
                    float randXi = horizontalRandomness > 0f ? Random.Range(-horizontalRandomness, horizontalRandomness) : 0f;
                    Vector3 childPos = new Vector3(basePos.x + first + i * spreadDistance + randXi, basePos.y + actualDrop, basePos.z);
                    GameObject siblingObj = SummonPool.Instance != null
                        ? SummonPool.Instance.Spawn(gameObject, childPos, Quaternion.identity)
                        : Instantiate(gameObject, childPos, Quaternion.identity);
                    TrapObject sibling = siblingObj.GetComponent<TrapObject>();
                    if (sibling != null)
                    {
                        sibling.isClusterChild = true;
                        sibling.owner = source;
                        sibling.lifeTimer = lifetime;
                        sibling.InitFallPhysics();
                    }
                    if (source != null && source.Gear != null && source.Gear.BowState != null)
                    {
                        source.Gear.BowState.RegisterSummon(siblingObj);
                    }
                }
            }
        }

        private void InitFallPhysics()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = fallGravity;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.linearVelocity = Vector2.down * initialFallSpeed;
            }

            if (trapCollider == null) trapCollider = GetComponent<Collider2D>();
            if (trapCollider != null)
            {
                trapCollider.isTrigger = true; // Xuyên qua người chơi dạng trigger, không đè lên đầu địch
            }
        }

        private void Update()
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                Despawn();
                return;
            }

            if (!landed)
            {
                // Fallback rơi tự do nếu không có Rigidbody2D
                if (body == null)
                {
                    transform.position += Vector3.down * (initialFallSpeed * Time.deltaTime);
                }

                // Raycast kiểm tra mặt đất phía dưới để tiếp đất chính xác
                float checkDist = Mathf.Max(0.35f, (body != null ? Mathf.Abs(body.linearVelocity.y) : initialFallSpeed) * Time.deltaTime + 0.15f);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, checkDist, groundLayers);
                if (hit.collider != null && hit.collider.gameObject != gameObject && IsGround(hit.collider))
                {
                    Land(hit.collider);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleTrigger(other);
        }

        private void HandleTrigger(Collider2D other)
        {
            if (triggered || isArming || other == null) return;

            // 1. Khi đang rơi: Chỉ nhận mặt đất để tiếp đất, rơi xuyên qua người chơi
            if (!landed)
            {
                if (IsGround(other))
                {
                    Land(other);
                }
                return;
            }

            // 2. Sau khi đã tiếp đất: Người chơi đối thủ giẫm phải
            PlayerController target = other.GetComponentInParent<PlayerController>();
            if (target != null && owner != null && target.Team != owner.Team && target.Alive)
            {
                StartCoroutine(TriggerTrapRoutine(target));
            }
        }

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

        private void Land(Collider2D groundCol = null)
        {
            if (landed) return;
            landed = true;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.bodyType = RigidbodyType2D.Kinematic;
            }

            if (trapCollider != null)
            {
                trapCollider.isTrigger = true;
            }
            EnsureTriggerColliders();

            // Căn chỉnh nằm sát mặt đất
            RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.5f, Vector2.down, 2.5f, groundLayers);
            if (hit.collider != null && hit.collider.gameObject != gameObject && IsGround(hit.collider))
            {
                float halfH = trapCollider != null ? trapCollider.bounds.extents.y : 0.2f;
                transform.position = new Vector3(transform.position.x, hit.point.y + halfH, transform.position.z);
            }
            else if (groundCol != null)
            {
                float halfH = trapCollider != null ? trapCollider.bounds.extents.y : 0.2f;
                transform.position = new Vector3(transform.position.x, groundCol.bounds.max.y + halfH, transform.position.z);
            }

            if (landEffectPrefab != null)
            {
                ParticleSystem ps = Instantiate(landEffectPrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 1.5f);
            }
        }

        private IEnumerator TriggerTrapRoutine(PlayerController target)
        {
            isArming = true;

            // Bật hiệu ứng cảnh báo nếu có
            if (warningEffectPrefab != null)
            {
                ParticleSystem ps = Instantiate(warningEffectPrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, triggerDelay + 0.5f);
            }

            // Chờ thời gian trễ (mặc định 1s theo yêu cầu)
            float elapsed = 0f;
            Vector3 originalPos = transform.position;
            while (elapsed < triggerDelay)
            {
                elapsed += Time.deltaTime;
                // Hiệu ứng rung lắc nhẹ cảnh báo bẫy sắp nổ
                float shake = Mathf.Sin(elapsed * 40f) * 0.04f;
                transform.position = originalPos + new Vector3(shake, 0f, 0f);
                yield return null;
            }
            transform.position = originalPos;

            triggered = true;

            // LÚC CẮN TRÚNG:
            // 1. Chỉnh position x của trap = position x của aim target địch theo yêu cầu
            if (target != null)
            {
                Vector3 trapPos = transform.position;
                trapPos.x = target.Aim.x;
                transform.position = trapPos;
            }

            // 2. Hiển thị hiệu ứng xích trói khớp với aim target của địch
            if (chainEffect != null)
            {
                if (target != null)
                {
                    chainEffect.transform.position = new Vector3(target.Aim.x, target.Aim.y, 0f);
                }
                chainEffect.SetActive(true);
            }

            // 3. Quản lý bởi Animator: Chuyển sang TrapCap (cắn bẫy & kích hoạt hiệu ứng xích)
            if (animator != null)
            {
                animator.SetTrigger("Cap");
                animator.Play("TrapCap", 0, 0f);
            }

            // 4. Gây sát thương và khóa nạn nhân (Stun)
            ApplyTrapEffect(target);

            // Hiệu ứng bẫy nổ/sập
            SkillAudioManager.PlayBoom();
            if (triggerParticlePrefab != null)
            {
                ParticleSystem ps = Instantiate(triggerParticlePrefab, transform.position, Quaternion.identity);
                ps.Play(true);
                Destroy(ps.gameObject, 1.5f);
            }

            // 5. Duy trì hiệu ứng trói và khóa nạn nhân trong suốt thời gian lockDuration
            float elapsedLock = 0f;
            while (elapsedLock < lockDuration)
            {
                elapsedLock += Time.deltaTime;
                if (chainEffect != null && target != null)
                {
                    chainEffect.transform.position = new Vector3(target.Aim.x, target.Aim.y, 0f);
                }
                yield return null;
            }

            if (chainEffect != null)
            {
                chainEffect.SetActive(false);
            }

            Despawn();
        }

        private void ApplyTrapEffect(PlayerController primaryTarget)
        {
            if (primaryTarget != null && primaryTarget.Alive && primaryTarget.Stats != null)
            {
                DamageAndLockTarget(primaryTarget);
            }

            // Đồng thời tác động lên bất kỳ đối thủ nào đứng trong phạm vi bẫy khi nổ
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, triggerRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                PlayerController p = colliders[i].GetComponentInParent<PlayerController>();
                if (p != null && p != primaryTarget && owner != null && p.Team != owner.Team && p.Alive)
                {
                    DamageAndLockTarget(p);
                }
            }
        }

        private void DamageAndLockTarget(PlayerController p)
        {
            if (p == null || p.Stats == null || !p.Alive) return;

            // 1. Gây sát thương
            p.Stats.TakeDamage(new DamageInfo(owner, damage, false, transform.position,
                effect, effectPower, effectDuration));

            // 2. Khóa người chơi (Stun khóa di chuyển, đánh và dùng chiêu)
            if (lockDuration > 0f)
            {
                p.Stats.Stun(lockDuration);
                if (p.Move != null)
                {
                    p.Move.Lock(true);
                    p.StartCoroutine(UnlockPlayerMoveAfter(p.Move, lockDuration));
                }
            }
        }

        private static IEnumerator UnlockPlayerMoveAfter(PlayerMove move, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (move != null) move.Lock(false);
        }

        private bool IsGround(Collider2D other)
        {
            if (other == null || other.gameObject == gameObject) return false;
            // Không bao giờ nhận diện Player là Ground
            if (other.GetComponentInParent<PlayerController>() != null) return false;

            int layerBit = 1 << other.gameObject.layer;
            if ((groundLayers.value & layerBit) != 0) return true;
            return other.CompareTag("Ground") ||
                   string.Equals(LayerMask.LayerToName(other.gameObject.layer), "Ground", System.StringComparison.OrdinalIgnoreCase) ||
                   other.gameObject.name.ToLower().Contains("ground");
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
            isClusterChild = false;
            if (chainEffect != null)
            {
                chainEffect.SetActive(false);
            }
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }
    }
}
