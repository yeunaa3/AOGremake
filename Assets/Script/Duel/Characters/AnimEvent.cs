using UnityEngine;
namespace AOG.Duel
{
    // Gắn trên Visual, ngay object có Animator.
    public sealed class AnimEvent : MonoBehaviour
    {
        private PlayerController player;
        private void Awake() => player = GetComponentInParent<PlayerController>();
        public void ReleaseArrow() => player.Action.ReleaseArrowFromClip();
        public void UseSkillEffect() => player.Action.UseSkillFromClip();
        public void EndSkill() => player.Action.EndSkillFromClip();
    }
}
