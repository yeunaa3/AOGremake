using UnityEngine;

namespace AOG.Duel
{
    public sealed class PowerArrowSkill : SkillData
    {
        [SerializeField] private ArrowBuffData arrowBuff = new ArrowBuffData();

        public override SkillType Type => SkillType.PlayerOnly;

        public override void Use(SkillCtx context)
        {
            if (context.Caster != null)
                context.Caster.ApplyArrowBuff(arrowBuff, context.Slot);
        }
    }
}
