using UnityEngine;

namespace AOG.Duel
{
    public class TauntTotem : SkillObject
    {
        [Header("Taunt Settings")]
        [Tooltip("Hiệu ứng vòng hoặc hào quang khi vật thể đang thu hút đòn đánh")]
        [SerializeField] private GameObject activeIndicator;

        public override void Setup(PlayerController source)
        {
            base.Setup(source);
            if (owner != null)
            {
                owner.ActiveTauntEntity = this;
            }
            if (activeIndicator != null)
            {
                activeIndicator.SetActive(true);
            }
        }

        public override void Die()
        {
            ClearTaunt();
            base.Die();
        }

        private void OnDestroy()
        {
            ClearTaunt();
        }

        private void ClearTaunt()
        {
            if (owner != null && owner.ActiveTauntEntity == this)
            {
                owner.ActiveTauntEntity = null;
            }
        }
    }
}
