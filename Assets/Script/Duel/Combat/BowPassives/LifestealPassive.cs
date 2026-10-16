using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Lifesteal", fileName = "LifestealPassive")]
    public sealed class LifestealPassive : BowPassive
    {
        [Tooltip("Tỉ lệ hồi máu theo sát thương gây ra (0.5 = 50%).")]
        [SerializeField, Range(0f, 1f)] private float lifestealRate = 0.5f;

        public float LifestealRate => lifestealRate;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isSecondaryShot)
        {
            if (arrow == null) return;
            arrow.LifeSteal = Mathf.Max(arrow.LifeSteal, lifestealRate);
        }
    }
}
