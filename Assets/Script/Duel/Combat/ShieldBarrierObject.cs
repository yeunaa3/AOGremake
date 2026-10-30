using UnityEngine;

namespace AOG.Duel
{
    public class ShieldBarrierObject : SkillObject
    {
        [Header("Shield Immortal Config")]
        [Tooltip("Thời gian tồn tại của khiên kể từ lần đầu nhận dame (giây, mặc định 5s).")]
        [SerializeField, Min(1f)] private float immortalDuration = 5f;
        [Tooltip("Bám theo người chơi ở phía trước (true) hay đứng yên tại chỗ khi triệu hồi (false).")]
        [SerializeField] private bool followOwner = true;
        [Tooltip("Khoảng cách phía trước mặt người chơi (theo hướng Face).")]
        [SerializeField] private float frontOffset = 0.85f;
        [Tooltip("Độ cao lệch so với gốc người chơi.")]
        [SerializeField] private float heightOffset = 0.35f;

        private bool hasReceivedFirstHit;
        private float remainingImmortalTime;

        public bool HasReceivedFirstHit => hasReceivedFirstHit;
        public float RemainingImmortalTime => remainingImmortalTime;
        public float ImmortalDuration => immortalDuration;

        protected override void Awake()
        {
            base.Awake();
            decayPercentPerSecond = 0f;
            maxLifetime = 0f;
            enableFallPhysics = false;
        }

        public override void Setup(PlayerController creator)
        {
            base.Setup(creator);
            hasReceivedFirstHit = false;
            remainingImmortalTime = immortalDuration;
            currentHp = maxHp;

            if (followOwner && owner != null)
            {
                UpdatePositionAndFacing(true);
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            if (followOwner && owner != null && owner.Alive)
            {
                UpdatePositionAndFacing(false);
            }

            if (hasReceivedFirstHit)
            {
                remainingImmortalTime -= Time.deltaTime;
                if (miniHealthBar != null)
                {
                    miniHealthBar.SetHealth(remainingImmortalTime, immortalDuration);
                }

                if (remainingImmortalTime <= 0f)
                {
                    Die();
                    return;
                }
            }
        }

        private void UpdatePositionAndFacing(bool immediate)
        {
            if (owner == null) return;

            Vector3 targetPos = owner.transform.position + new Vector3(owner.Face * frontOffset, heightOffset, 0f);
            if (immediate)
            {
                transform.position = targetPos;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 25f);
            }

            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (owner.Face >= 0f ? 1f : -1f);
            transform.localScale = s;
        }

        public override void TakeDamage(int amount, PlayerController attacker = null, Vector2 hitDirection = default,
            ArrowEffect effect = ArrowEffect.None, int effectPower = 0, float effectDuration = 0f)
        {
            if (isDead) return;

            // Lần đầu nhận dame: bắt đầu tính giờ 5s bất tử của khiên
            if (!hasReceivedFirstHit)
            {
                hasReceivedFirstHit = true;
                remainingImmortalTime = immortalDuration;
                SkillAudioManager.PlayPlayerHit();
            }

            // Khiên là bất tử: KHÔNG bị trừ HP, không chết từ sát thương!
            currentHp = maxHp;

            if (hitDirection == default && attacker != null)
            {
                hitDirection = (transform.position - attacker.transform.position).normalized;
            }
            PlayHitReaction(hitDirection);
        }
    }
}
