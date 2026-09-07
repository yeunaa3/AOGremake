using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Đặt component này cùng GameObject với Animator để Animation Event gọi được gameplay ở Character root.
    /// </summary>
    public sealed class DuelAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private DuelCharacter character;

        public void ReleaseArrow()
        {
            character?.ActionController.ReleaseAutoAttackFromAnimation();
        }
    }
}
