using UnityEngine;

namespace AOG.Duel
{
    public sealed class PassiveSkill : SkillData
    {
        [SerializeField, Min(1)] private int maxLayers = 3;
        [SerializeField, Min(0.1f)] private float rechargeTime = 5f;

        public override SkillType Type => SkillType.PlayerOnly;
        public override bool Passive => true;

        public override void Use(SkillCtx context)
        {
            context.Caster?.Stats.StartShieldPassive(maxLayers, rechargeTime);
        }
    }
}
