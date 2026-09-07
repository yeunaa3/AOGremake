using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DuelCharacterMotor : MonoBehaviour
    {
        [Tooltip("Chỉ dùng nếu CharacterStats chưa được gắn.")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private float minimumX = -8f;
        [SerializeField] private float maximumX = 8f;
        [SerializeField, Min(0f)] private float movingThreshold = 0.05f;

        private Rigidbody2D body;
        private float movementInput;
        private bool gameplayMovementAllowed;
        private bool actionMovementAllowed = true;
        private bool isDashing;
        private float dashDirection;
        private float dashSpeed;

        public float MovementInput => movementInput;
        public bool IsDashing => isDashing;
        public bool IsMoving => isDashing || Mathf.Abs(movementInput) > movingThreshold;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (stats == null) stats = GetComponent<CharacterStats>();
        }

        private void FixedUpdate()
        {
            float velocityX = 0f;
            if (gameplayMovementAllowed)
            {
                if (isDashing)
                {
                    velocityX = dashDirection * dashSpeed;
                }
                else if (actionMovementAllowed)
                {
                    float effectiveMoveSpeed = stats != null ? stats.MoveSpeed : moveSpeed;
                    velocityX = movementInput * effectiveMoveSpeed;
                }
            }

            Vector2 velocity = GetVelocity();
            SetVelocity(new Vector2(velocityX, velocity.y));

            Vector2 position = body.position;
            position.x = Mathf.Clamp(position.x, minimumX, maximumX);
            if (!Mathf.Approximately(position.x, body.position.x))
            {
                body.position = position;
                velocity = GetVelocity();
                SetVelocity(new Vector2(0f, velocity.y));
            }
        }

        public void SetMovementInput(float value)
        {
            movementInput = Mathf.Clamp(value, -1f, 1f);
        }

        public void SetGameplayMovementAllowed(bool allowed)
        {
            gameplayMovementAllowed = allowed;
            if (!allowed)
            {
                movementInput = 0f;
                StopImmediately();
            }
        }

        public void SetActionMovementAllowed(bool allowed)
        {
            actionMovementAllowed = allowed;
        }

        public void BeginDash(float direction, float speed)
        {
            dashDirection = Mathf.Approximately(direction, 0f) ? 1f : Mathf.Sign(direction);
            dashSpeed = Mathf.Max(0f, speed);
            isDashing = true;
        }

        public void EndDash()
        {
            isDashing = false;
        }

        public void StopImmediately()
        {
            isDashing = false;
            if (body != null)
            {
                Vector2 velocity = GetVelocity();
                SetVelocity(new Vector2(0f, velocity.y));
            }
        }

        private Vector2 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        private void SetVelocity(Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = velocity;
#else
            body.velocity = velocity;
#endif
        }

        public void SetHorizontalBounds(float min, float max)
        {
            minimumX = Mathf.Min(min, max);
            maximumX = Mathf.Max(min, max);
        }

        private void OnValidate()
        {
            if (minimumX > maximumX)
            {
                float oldMinimum = minimumX;
                minimumX = maximumX;
                maximumX = oldMinimum;
            }
        }
    }
}
