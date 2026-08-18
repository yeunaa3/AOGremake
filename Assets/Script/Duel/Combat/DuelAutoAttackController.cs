using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelAutoAttackController : MonoBehaviour
    {
        [SerializeField] private DuelCharacter owner;
        [SerializeField] private DuelCharacterMotor motor;
        [SerializeField] private DuelCharacterActionController actionController;
        [SerializeField, Min(0f)] private float idleDelay = 0.2f;
        [SerializeField, Min(0.01f)] private float attackInterval = 1f;

        private float idleTimer;
        private float attackCooldownRemaining;

        private void Awake()
        {
            if (owner == null) owner = GetComponent<DuelCharacter>();
            if (motor == null) motor = GetComponent<DuelCharacterMotor>();
            if (actionController == null) actionController = GetComponent<DuelCharacterActionController>();
        }

        private void Update()
        {
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - Time.deltaTime);

            if (owner == null || !owner.CanReceiveInput || motor.IsMoving || !actionController.IsReady)
            {
                idleTimer = 0f;
                return;
            }

            idleTimer += Time.deltaTime;
            if (idleTimer < idleDelay || attackCooldownRemaining > 0f)
            {
                return;
            }

            if (actionController.TryAutoAttack())
            {
                attackCooldownRemaining = attackInterval;
                idleTimer = 0f;
            }
        }

        public void ResetAttack()
        {
            idleTimer = 0f;
            attackCooldownRemaining = 0f;
        }
    }
}
