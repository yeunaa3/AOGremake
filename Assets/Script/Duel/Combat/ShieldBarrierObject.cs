using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Khiên chắn bảo vệ trước mặt nhân vật, tự động bám theo và thu hồi về pool khi hết thời gian bất tử.
    /// </summary>
    public class ShieldBarrierObject : SkillObject
    {
        [Header("Shield Immortal Config")]
        [Tooltip("Thời gian tồn tại của khiên kể từ lần đầu nhận sát thương (giây).")]
        [SerializeField, Min(1f)] private float immortalDuration = 5f;

        [Header("Vị trí & Góc quay khiên")]
        [Tooltip("Khoảng cách phía trước mặt nhân vật. Tự động lật đối xứng khi đổi hướng nhìn.")]
        [SerializeField] private float frontOffset = 0.85f;
        [Tooltip("Độ cao so với gốc nhân vật.")]
        [SerializeField] private float heightOffset = 0.35f;
        [Tooltip("Góc nghiêng của khiên (độ). Tự động lật đối xứng khi đổi hướng nhìn.")]
        [SerializeField] private float rotationAngle = 0f;

        private const string CloseTriggerName = "Close";
        private const float DefaultCloseDuration = 0.5f;
        private const float FollowSpeed = 25f;

        private Animator animator;
        private Collider2D[] barrierColliders;
        private bool hasReceivedFirstHit;
        private float remainingImmortalTime;
        private bool isClosing;
        private Coroutine closeRoutine;

        public bool HasReceivedFirstHit => hasReceivedFirstHit;
        public float RemainingImmortalTime => remainingImmortalTime;
        public float ImmortalDuration => immortalDuration;
        public bool IsClosing => isClosing;

        public float FrontOffset => frontOffset;
        public float HeightOffset => heightOffset;
        public float RotationAngle => rotationAngle;

        public void SetTransformConfig(float newFrontOffset, float newHeightOffset, float newRotationAngle)
        {
            frontOffset = newFrontOffset;
            heightOffset = newHeightOffset;
            rotationAngle = newRotationAngle;
            if (owner != null)
            {
                UpdatePositionAndFacing(true);
            }
        }

        public void SetOffset(float newFrontOffset, float newHeightOffset)
        {
            SetTransformConfig(newFrontOffset, newHeightOffset, rotationAngle);
        }

        protected override void Awake()
        {
            base.Awake();
            decayPercentPerSecond = 0f;
            maxLifetime = 0f;
            enableFallPhysics = false;
            animator = GetComponent<Animator>();
            barrierColliders = GetComponentsInChildren<Collider2D>(true);
        }

        public override void Setup(PlayerController creator)
        {
            base.Setup(creator);
            hasReceivedFirstHit = false;
            remainingImmortalTime = immortalDuration;
            currentHp = maxHp;
            isClosing = false;
            isDead = false;

            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            if (barrierColliders == null || barrierColliders.Length == 0)
            {
                barrierColliders = GetComponentsInChildren<Collider2D>(true);
            }
            if (barrierColliders != null)
            {
                for (int i = 0; i < barrierColliders.Length; i++)
                {
                    if (barrierColliders[i] != null) barrierColliders[i].enabled = true;
                }
            }

            // Bắt đầu hiển thị: chạy tự nhiên ShieldOpen -> ShieldHold
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger(CloseTriggerName);
                animator.Play("ShieldOpen", 0, 0f);
                animator.Update(0f);
            }

            if (owner != null)
            {
                UpdatePositionAndFacing(true);
            }
        }

        protected override void Update()
        {
            if (isDead) return;

            if (owner != null && owner.Alive)
            {
                UpdatePositionAndFacing(false);
            }

            if (isClosing) return;

            if (hasReceivedFirstHit)
            {
                remainingImmortalTime -= Time.deltaTime;
                if (miniHealthBar != null)
                {
                    miniHealthBar.SetHealth(remainingImmortalTime, immortalDuration);
                }

                if (remainingImmortalTime <= 0f)
                {
                    StartClose();
                }
            }
        }

        public void StartClose()
        {
            if (isClosing || isDead) return;
            isClosing = true;

            // Tắt collider để khi bắt đầu close không còn chắn đạn/nhận đòn
            if (barrierColliders != null)
            {
                for (int i = 0; i < barrierColliders.Length; i++)
                {
                    if (barrierColliders[i] != null) barrierColliders[i].enabled = false;
                }
            }

            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(CloseTriggerName);
            }

            float waitDuration = DefaultCloseDuration;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip != null && clip.name == "ShieldClose")
                    {
                        waitDuration = clip.length;
                        break;
                    }
                }
            }

            if (gameObject.activeInHierarchy)
            {
                closeRoutine = StartCoroutine(CloseSequence(waitDuration));
            }
            else
            {
                Die();
            }
        }

        private IEnumerator CloseSequence(float waitDuration)
        {
            yield return new WaitForSeconds(waitDuration);
            closeRoutine = null;
            Die();
        }

        private void UpdatePositionAndFacing(bool immediate)
        {
            if (owner == null) return;

            float faceSign = owner.Face >= 0f ? 1f : -1f;
            Vector3 targetPosition = owner.transform.position + new Vector3(faceSign * frontOffset, heightOffset, 0f);

            if (immediate)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * FollowSpeed);
            }

            Vector3 currentScale = transform.localScale;
            currentScale.x = Mathf.Abs(currentScale.x) * faceSign;
            transform.localScale = currentScale;

            transform.localRotation = Quaternion.Euler(0f, 0f, faceSign * rotationAngle);
        }

        public override void TakeDamage(int amount, PlayerController attacker = null, Vector2 hitDirection = default,
            ArrowEffect effect = ArrowEffect.None, int effectPower = 0, float effectDuration = 0f)
        {
            if (isDead || isClosing) return;

            if (!hasReceivedFirstHit)
            {
                hasReceivedFirstHit = true;
                remainingImmortalTime = immortalDuration;
                SkillAudioManager.PlayPlayerHit();
            }

            currentHp = maxHp;

            if (hitDirection == default && attacker != null)
            {
                hitDirection = (transform.position - attacker.transform.position).normalized;
            }
            PlayHitReaction(hitDirection);
        }

        public override void Die()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }
            base.Die();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
            }
        }

        private void OnDrawGizmosSelected()
        {
            float faceSign = owner != null ? (owner.Face >= 0f ? 1f : -1f) : 1f;
            Vector3 centerPosition = owner != null 
                ? owner.transform.position + new Vector3(faceSign * frontOffset, heightOffset, 0f)
                : transform.position;

            Gizmos.color = Color.cyan;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(centerPosition, Quaternion.Euler(0f, 0f, faceSign * rotationAngle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.5f, 1.2f, 0.1f));
            Gizmos.matrix = previousMatrix;

            if (owner != null)
            {
                Gizmos.DrawLine(owner.transform.position, centerPosition);
            }
        }
#endif
    }
}
