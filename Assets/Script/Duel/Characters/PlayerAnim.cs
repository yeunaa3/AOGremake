using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    // Script này chỉ làm 2 việc: lật nhân vật và gửi Parameter vào Animator.
    public sealed class PlayerAnim : MonoBehaviour
    {
        private Animator animator;
        private Animator effectAnimator;
        private Transform visual;
        private float normalX;
        private Transform[] frozenParts;
        private Vector3[] frozenPositions;
        private Quaternion[] frozenRotations;
        private Vector3[] frozenScales;
        private bool poseFrozen;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            visual = animator == null ? null : animator.transform;
            if (visual == null) return;

            Transform effect = visual.Find("Effect");
            if (effect != null) effectAnimator = effect.GetComponent<Animator>();

            normalX = Mathf.Abs(visual.localScale.x);

            // Giữ toàn bộ cơ thể, nhưng bỏ nhánh Effect để các clip hiệu ứng vẫn chạy riêng.
            var parts = new List<Transform>();
            foreach (Transform part in visual.GetComponentsInChildren<Transform>(true))
                if (!IsEffectPart(part)) parts.Add(part);

            frozenParts = parts.ToArray();
            frozenPositions = new Vector3[frozenParts.Length];
            frozenRotations = new Quaternion[frozenParts.Length];
            frozenScales = new Vector3[frozenParts.Length];
        }

        public void Face(float direction)
        {
            if (visual == null || direction == 0f) return;
            Vector3 scale = visual.localScale;
            scale.x = normalX * Mathf.Sign(direction);
            visual.localScale = scale;
        }

        public void Values(bool moving, bool stunned, bool frozen, bool silenced, bool shielding, float attackSpeed)
        {
            Bool("IsMoving", moving);
            Bool("IsStunned", stunned);
            Bool("IsSilenced", silenced);
            Bool("IsShielding", shielding);
            Float("MoveSpeed", moving ? 1f : 0f);
            Float("AttackSpeed", attackSpeed);
            EffectBool("IsFrozen", frozen);

            if (frozen && !poseFrozen)
            {
                poseFrozen = true;
                for (int i = 0; i < frozenParts.Length; i++)
                {
                    frozenPositions[i] = frozenParts[i].localPosition;
                    frozenRotations[i] = frozenParts[i].localRotation;
                    frozenScales[i] = frozenParts[i].localScale;
                }
            }
            else if (!frozen)
            {
                poseFrozen = false;
            }
        }

        // Animator chạy trước LateUpdate, nên đặt lại tư thế tại đây sẽ giữ nguyên từng khớp.
        private void LateUpdate()
        {
            if (!poseFrozen) return;

            for (int i = 0; i < frozenParts.Length; i++)
            {
                if (frozenParts[i] == null) continue;
                frozenParts[i].localPosition = frozenPositions[i];
                frozenParts[i].localRotation = frozenRotations[i];
                frozenParts[i].localScale = frozenScales[i];
            }
        }

        // Dùng trực tiếp các hàm này khi bạn thêm Parameter mới.
        public void Trigger(string name) { if (Has(animator, name, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(name); }
        public void Bool(string name, bool value) { if (Has(animator, name, AnimatorControllerParameterType.Bool)) animator.SetBool(name, value); }
        public void Float(string name, float value) { if (Has(animator, name, AnimatorControllerParameterType.Float)) animator.SetFloat(name, value); }

        // Parameter của Animator riêng nằm trên object Effect.
        public void EffectTrigger(string name) { if (Has(effectAnimator, name, AnimatorControllerParameterType.Trigger)) effectAnimator.SetTrigger(name); }
        public void EffectBool(string name, bool value) { if (Has(effectAnimator, name, AnimatorControllerParameterType.Bool)) effectAnimator.SetBool(name, value); }

        // Code chủ động thoát clip skill, không phụ thuộc transition dễ bị kẹt trong Animator.
        public void EndSkillAnimation()
        {
            if (animator != null) animator.CrossFade("Character_shot", .05f, 0);
        }

        private bool Has(Animator target, string name, AnimatorControllerParameterType type)
        {
            if (target == null) return false;
            foreach (AnimatorControllerParameter item in target.parameters)
                if (item.name == name && item.type == type) return true;
            return false;
        }

        private bool IsEffectPart(Transform part)
        {
            for (Transform current = part; current != null && current != visual; current = current.parent)
                if (current.name == "Effect" || current.name == "Effects" || current.name == "FreezeEffect")
                    return true;

            return false;
        }
    }
}
