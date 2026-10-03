using UnityEngine;

namespace AOG.Duel
{
    // Gắn behaviour này vào từng State skill trong Animator.
    // Khi animation rời State, skill tự kết thúc nên clip chỉ cần một Event: UseSkillEffect.
    public sealed class SkillStateEnd : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            PlayerAction action = animator.GetComponentInParent<PlayerAction>();
            if (action != null) action.EndSkillFromClip();
        }
    }
}
