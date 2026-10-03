using UnityEngine;

namespace AOG.Duel
{
    // Script này chỉ làm 2 việc: lật nhân vật và gửi Parameter vào Animator.
    public sealed class PlayerAnim : MonoBehaviour
    {
        private Animator animator;
        private Transform visual;
        private float normalX;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            visual = animator == null ? null : animator.transform;
            if (visual != null) normalX = Mathf.Abs(visual.localScale.x);
        }

        public void Face(float direction)
        {
            if (visual == null || direction == 0f) return;
            Vector3 scale = visual.localScale;
            scale.x = normalX * Mathf.Sign(direction);
            visual.localScale = scale;
        }

        public void Values(bool moving, bool stunned, bool silenced, bool shielding, float attackSpeed)
        {
            Bool("IsMoving", moving);
            Bool("IsStunned", stunned);
            Bool("IsSilenced", silenced);
            Bool("IsShielding", shielding);
            Float("MoveSpeed", moving ? 1f : 0f);
            Float("AttackSpeed", attackSpeed);
        }

        // Dùng trực tiếp các hàm này khi bạn thêm Parameter mới.
        public void Trigger(string name) { if (Has(name, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(name); }
        public void Bool(string name, bool value) { if (Has(name, AnimatorControllerParameterType.Bool)) animator.SetBool(name, value); }
        public void Float(string name, float value) { if (Has(name, AnimatorControllerParameterType.Float)) animator.SetFloat(name, value); }
        public void Int(string name, int value) { if (Has(name, AnimatorControllerParameterType.Int)) animator.SetInteger(name, value); }

        public void PlaySkill(SkillAnimation animation)
        {
            Int("SkillAnim", (int)animation);
            Trigger("UseSkill");
        }

        private bool Has(string name, AnimatorControllerParameterType type)
        {
            if (animator == null) return false;
            foreach (AnimatorControllerParameter item in animator.parameters)
                if (item.name == name && item.type == type) return true;
            return false;
        }
    }
}
