using UnityEngine;

namespace AOG.Duel
{
    // Đặt trên Visual, cùng object có Animator. Không cần kéo Character.
    public sealed class AnimEvent : MonoBehaviour
    {
        private Player player;
        private void Awake() => player = GetComponentInParent<Player>();
        public void ReleaseArrow() => player.ActionController.ReleaseAutoAttackFromAnimation();
    }
}