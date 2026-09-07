using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelCharacterActionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DuelCharacter owner;
        [SerializeField] private DuelCharacterMotor motor;
        [SerializeField] private DuelCharacterAnimator animationController;
        [SerializeField] private CharacterStats stats;
        [SerializeField] private CharacterStatusController statusController;

        [Header("Basic attack")]
        [SerializeField, Min(0.01f)] private float autoAttackDuration = 0.55f;
        [SerializeField, Min(0f)] private float autoAttackReleaseTime = 0.32f;
        [Tooltip("Bật khi clip có Animation Event gọi ReleaseArrow.")]
        [SerializeField] private bool releaseArrowByAnimationEvent;

        [Header("Dash")]
        [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
        [SerializeField, Min(0f)] private float dashSpeed = 14f;
        [SerializeField, Min(0f)] private float dashCooldown = 2f;

        [Header("Shield")]
        [SerializeField, Min(0.01f)] private float shieldDuration = 0.7f;
        [SerializeField, Min(0f)] private float shieldCooldown = 4f;

        [Header("Loadout")]
        [SerializeField] private DuelSkillDefinition[] equippedSkills = new DuelSkillDefinition[4];

        private readonly float[] skillCooldownRemaining = new float[4];
        private Action activeCallback;
        private Action finishCallback;
        private float actionElapsed;
        private float actionDuration;
        private float activeTime;
        private bool activeTriggered;
        private int activeSkillSlot = -1;
        private float pendingCooldown;
        private bool autoAttackReleased;

        public DuelActionState CurrentState { get; private set; } = DuelActionState.Ready;
        public bool IsReady => CurrentState == DuelActionState.Ready;
        public bool IsShieldActive { get; private set; }
        public float DashCooldownRemaining { get; private set; }
        public float ShieldCooldownRemaining { get; private set; }
        public float DashCooldownDuration => dashCooldown;
        public float ShieldCooldownDuration => shieldCooldown;
        public int SkillCount => equippedSkills != null ? equippedSkills.Length : 0;

        public event Action<DuelActionState> StateChanged;

        private void Awake()
        {
            if (owner == null) owner = GetComponent<DuelCharacter>();
            if (motor == null) motor = GetComponent<DuelCharacterMotor>();
            if (animationController == null) animationController = GetComponent<DuelCharacterAnimator>();
            if (stats == null) stats = GetComponent<CharacterStats>();
            if (statusController == null) statusController = GetComponent<CharacterStatusController>();
            EnsureFourSkillSlots();
        }

        private void Update()
        {
            TickCooldowns(Time.deltaTime);
            SynchronizeStunState();

            if (CurrentState == DuelActionState.Ready
                || CurrentState == DuelActionState.Dead
                || CurrentState == DuelActionState.Stunned)
            {
                return;
            }

            actionElapsed += Time.deltaTime;
            if (!activeTriggered && actionElapsed >= activeTime)
            {
                TriggerActiveMoment();
            }

            if (actionElapsed >= actionDuration)
            {
                FinishCurrentAction();
            }
        }

        public bool TryAutoAttack()
        {
            if (!CanBeginBasicAttack()) return false;

            float attackAnimationSpeed = stats != null ? stats.AttackAnimationSpeed : 1f;
            float effectiveDuration = autoAttackDuration / attackAnimationSpeed;
            float effectiveReleaseTime = autoAttackReleaseTime / attackAnimationSpeed;
            autoAttackReleased = false;

            return BeginAction(
                DuelActionState.AutoAttacking,
                effectiveDuration,
                Mathf.Min(effectiveReleaseTime, effectiveDuration),
                0f,
                true,
                releaseArrowByAnimationEvent ? null : ReleaseAutoAttack,
                () => owner.SetLoadedArrowVisible(false),
                () =>
                {
                    owner.SetLoadedArrowVisible(true);
                    animationController.PlayAutoAttack();
                });
        }

        public bool TryDash(float requestedDirection, float facingDirection)
        {
            if (!CanBeginSpecialAction() || DashCooldownRemaining > 0f) return false;

            float direction = Mathf.Abs(requestedDirection) > 0.01f
                ? requestedDirection
                : facingDirection;

            return BeginAction(
                DuelActionState.Dashing,
                dashDuration,
                0f,
                dashCooldown,
                true,
                () => motor.BeginDash(direction, dashSpeed),
                () => motor.EndDash(),
                () => animationController.PlayDash());
        }

        public bool TryShield()
        {
            if (!CanBeginSpecialAction() || ShieldCooldownRemaining > 0f) return false;

            return BeginAction(
                DuelActionState.Shielding,
                shieldDuration,
                0f,
                shieldCooldown,
                true,
                () => IsShieldActive = true,
                () => IsShieldActive = false,
                () => animationController.PlayShield());
        }

        public bool TrySkill(int slot)
        {
            if (!CanBeginSpecialAction() || !IsValidSkillSlot(slot)) return false;
            if (skillCooldownRemaining[slot] > 0f) return false;

            DuelSkillDefinition skill = equippedSkills[slot];
            if (skill == null) return false;

            activeSkillSlot = slot;
            return BeginAction(
                DuelActionState.UsingSkill,
                skill.ActionDuration,
                skill.ActiveTime,
                skill.Cooldown,
                skill.AllowMovement,
                () => skill.Execute(new DuelSkillContext(owner, owner.Opponent, slot)),
                null,
                () => animationController.PlaySkill(skill.AnimationTrigger, slot));
        }

        public void ApplyStun(float duration)
        {
            statusController?.ApplyStun(duration);
        }

        public void ApplySilence(float duration)
        {
            statusController?.ApplySilence(duration);
        }

        public void ReleaseAutoAttackFromAnimation()
        {
            if (!releaseArrowByAnimationEvent) return;
            ReleaseAutoAttack();
        }

        public void MarkDead()
        {
            CancelCurrentAction(false);
            SetState(DuelActionState.Dead);
            motor.SetActionMovementAllowed(false);
            motor.StopImmediately();
            animationController.PlayDeath();
        }

        public void StopForMatchEnd()
        {
            if (CurrentState == DuelActionState.Dead) return;
            CancelCurrentAction(false);
            SetState(DuelActionState.Ready);
        }

        public void ResetActions()
        {
            CancelCurrentAction(false);
            DashCooldownRemaining = 0f;
            ShieldCooldownRemaining = 0f;
            for (int i = 0; i < skillCooldownRemaining.Length; i++)
            {
                skillCooldownRemaining[i] = 0f;
            }
            SetState(DuelActionState.Ready);
        }

        public DuelSkillDefinition GetSkill(int slot)
        {
            return IsValidSkillSlot(slot) ? equippedSkills[slot] : null;
        }

        public float GetSkillCooldownRemaining(int slot)
        {
            return slot >= 0 && slot < skillCooldownRemaining.Length
                ? skillCooldownRemaining[slot]
                : 0f;
        }

        public void SetSkill(int slot, DuelSkillDefinition skill)
        {
            EnsureFourSkillSlots();
            if (slot >= 0 && slot < equippedSkills.Length)
            {
                equippedSkills[slot] = skill;
                skillCooldownRemaining[slot] = 0f;
            }
        }

        private bool BeginAction(
            DuelActionState state,
            float duration,
            float effectTime,
            float cooldownAfterFinish,
            bool allowMovement,
            Action onActive,
            Action onFinish,
            Action onAnimation)
        {
            actionElapsed = 0f;
            actionDuration = Mathf.Max(0.01f, duration);
            activeTime = Mathf.Clamp(effectTime, 0f, actionDuration);
            pendingCooldown = Mathf.Max(0f, cooldownAfterFinish);
            activeCallback = onActive;
            finishCallback = onFinish;
            activeTriggered = false;

            motor.SetActionMovementAllowed(allowMovement);
            SetState(state);
            onAnimation?.Invoke();

            if (activeTime <= 0f)
            {
                TriggerActiveMoment();
            }

            return true;
        }

        private void TriggerActiveMoment()
        {
            activeTriggered = true;
            Action callback = activeCallback;
            activeCallback = null;
            callback?.Invoke();
        }

        private void FinishCurrentAction()
        {
            DuelActionState finishedState = CurrentState;
            int finishedSkillSlot = activeSkillSlot;
            float cooldown = pendingCooldown;

            Action callback = finishCallback;
            ClearRuntimeAction();
            callback?.Invoke();
            motor.SetActionMovementAllowed(true);

            if (finishedState == DuelActionState.Dashing)
            {
                DashCooldownRemaining = cooldown;
            }
            else if (finishedState == DuelActionState.Shielding)
            {
                ShieldCooldownRemaining = cooldown;
            }
            else if (finishedState == DuelActionState.UsingSkill && finishedSkillSlot >= 0)
            {
                skillCooldownRemaining[finishedSkillSlot] = cooldown;
            }

            SetState(DuelActionState.Ready);
        }

        private void CancelCurrentAction(bool returnToReady)
        {
            Action callback = finishCallback;
            ClearRuntimeAction();
            callback?.Invoke();
            IsShieldActive = false;
            motor.EndDash();
            motor.SetActionMovementAllowed(true);

            if (returnToReady)
            {
                SetState(DuelActionState.Ready);
            }
        }

        private void ClearRuntimeAction()
        {
            activeCallback = null;
            finishCallback = null;
            actionElapsed = 0f;
            actionDuration = 0f;
            activeTime = 0f;
            activeTriggered = false;
            pendingCooldown = 0f;
            activeSkillSlot = -1;
            autoAttackReleased = false;
        }

        private void TickCooldowns(float deltaTime)
        {
            DashCooldownRemaining = Mathf.Max(0f, DashCooldownRemaining - deltaTime);
            ShieldCooldownRemaining = Mathf.Max(0f, ShieldCooldownRemaining - deltaTime);
            for (int i = 0; i < skillCooldownRemaining.Length; i++)
            {
                skillCooldownRemaining[i] = Mathf.Max(0f, skillCooldownRemaining[i] - deltaTime);
            }
        }

        private bool CanBeginBasicAttack()
        {
            return CurrentState == DuelActionState.Ready
                && owner != null
                && owner.CanBasicAttack;
        }

        private bool CanBeginSpecialAction()
        {
            return CurrentState == DuelActionState.Ready
                && owner != null
                && owner.CanUseSpecialActions;
        }

        private void ReleaseAutoAttack()
        {
            if (CurrentState != DuelActionState.AutoAttacking || autoAttackReleased) return;
            autoAttackReleased = true;
            owner.SetLoadedArrowVisible(false);
            owner.FireBasicArrow();
        }

        private void SynchronizeStunState()
        {
            bool stunned = statusController != null && statusController.IsStunned;
            if (stunned)
            {
                if (CurrentState != DuelActionState.Dead && CurrentState != DuelActionState.Stunned)
                {
                    CancelCurrentAction(false);
                    motor.SetActionMovementAllowed(false);
                    motor.StopImmediately();
                    SetState(DuelActionState.Stunned);
                    animationController.PlayStun();
                }
                return;
            }

            if (CurrentState == DuelActionState.Stunned)
            {
                motor.SetActionMovementAllowed(true);
                SetState(DuelActionState.Ready);
            }
        }

        private bool IsValidSkillSlot(int slot)
        {
            return equippedSkills != null && slot >= 0 && slot < equippedSkills.Length;
        }

        private void EnsureFourSkillSlots()
        {
            if (equippedSkills != null && equippedSkills.Length == 4) return;

            var resized = new DuelSkillDefinition[4];
            if (equippedSkills != null)
            {
                int count = Mathf.Min(equippedSkills.Length, resized.Length);
                for (int i = 0; i < count; i++) resized[i] = equippedSkills[i];
            }
            equippedSkills = resized;
        }

        private void SetState(DuelActionState state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            StateChanged?.Invoke(state);
        }

        private void OnValidate()
        {
            autoAttackReleaseTime = Mathf.Clamp(autoAttackReleaseTime, 0f, autoAttackDuration);
            EnsureFourSkillSlots();
        }
    }
}
