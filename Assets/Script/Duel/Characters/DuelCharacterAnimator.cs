using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelCharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;

        private float visualScaleX = 1f;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visualRoot == null && animator != null) visualRoot = animator.transform;
            if (visualRoot != null) visualScaleX = Mathf.Abs(visualRoot.localScale.x);
        }

        public void SetMoving(bool moving)
        {
            SetBoolIfPresent("IsMoving", moving);
            SetFloatIfPresent("MoveSpeed", moving ? 1f : 0f);
        }

        public void SetAttackSpeed(float multiplier)
        {
            SetFloatIfPresent("AttackSpeed", Mathf.Max(0.05f, multiplier));
        }

        public void SetFacing(float direction)
        {
            if (visualRoot == null || Mathf.Approximately(direction, 0f)) return;

            Vector3 scale = visualRoot.localScale;
            scale.x = visualScaleX * Mathf.Sign(direction);
            visualRoot.localScale = scale;
        }

        public void PlayAutoAttack() => SetTriggerIfPresent("AutoAttack");
        public void PlayDash() => SetTriggerIfPresent("Dash");
        public void PlayShield() => SetTriggerIfPresent("Shield");
        public void PlayHit() => SetTriggerIfPresent("Hit");
        public void PlayStun() => SetTriggerIfPresent("Stun");
        public void PlayDeath() => SetTriggerIfPresent("Die");

        public void PlaySkill(string triggerName, int slot)
        {
            string resolvedTrigger = string.IsNullOrWhiteSpace(triggerName)
                ? $"Skill{slot + 1}"
                : triggerName;
            SetTriggerIfPresent(resolvedTrigger);
        }

        private void SetTriggerIfPresent(string parameterName)
        {
            if (animator != null && HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(parameterName);
            }
        }

        private void SetBoolIfPresent(string parameterName, bool value)
        {
            if (animator != null && HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }

        private void SetFloatIfPresent(string parameterName, float value)
        {
            if (animator != null && HasParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == type && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
