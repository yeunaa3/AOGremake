using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Stone Arrow", fileName = "StoneArrowPassive")]
    public sealed class StoneArrowPassive : BowPassive
    {
        [Tooltip("Hệ số tốc độ bay ban đầu (nhỏ hơn 1 để bay chậm).")]
        [SerializeField, Range(0.1f, 1f)] private float initialSpeedMultiplier = 0.6f;
        [Tooltip("Tốc độ nở to mỗi giây (ví dụ: 0.5 nghĩa là tăng 50% kích thước mỗi giây).")]
        [SerializeField, Min(0f)] private float sizeGrowthRate = 0.5f;
        [Tooltip("Kích thước tối đa mũi tên có thể nở to.")]
        [SerializeField, Min(1f)] private float maxSizeMultiplier = 2.5f;
        [Tooltip("Tốc độ tăng sát thương mỗi giây (ví dụ: 0.6 nghĩa là tăng 60% sát thương mỗi giây).")]
        [SerializeField, Min(0f)] private float damageGrowthRate = 0.6f;
        [Tooltip("Sát thương tối đa so với gốc.")]
        [SerializeField, Min(1f)] private float maxDamageMultiplier = 3.0f;

        public float InitialSpeedMultiplier => initialSpeedMultiplier;
        public float SizeGrowthRate => sizeGrowthRate;
        public float MaxSizeMultiplier => maxSizeMultiplier;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot)
        {
            if (arrow == null) return;

            arrow.ShotSpeedMultiplier *= initialSpeedMultiplier;
            Vector3 baseScale = arrow.transform.localScale;
            int baseDamage = arrow.Damage;
            float flightTime = 0f;

            arrow.OnFlightUpdate = (a, dt) =>
            {
                flightTime += dt;
                float sizeMult = Mathf.Min(maxSizeMultiplier, 1f + flightTime * sizeGrowthRate);
                a.transform.localScale = baseScale * sizeMult;

                float dmgMult = Mathf.Min(maxDamageMultiplier, 1f + flightTime * damageGrowthRate);
                a.Damage = Mathf.RoundToInt(baseDamage * dmgMult);
            };
        }
    }
}
