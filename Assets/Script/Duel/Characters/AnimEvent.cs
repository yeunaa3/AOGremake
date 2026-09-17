using UnityEngine;
namespace AOG.Duel
{
    // Gắn trên Visual, ngay object có Animator. Animation Event gọi ReleaseArrow.
    public sealed class AnimEvent : MonoBehaviour
    {
        private PlayerController player;
        private void Awake() => player = GetComponentInParent<PlayerController>();
        public void ReleaseArrow() => player.Action.ReleaseArrowFromClip();
    }
}