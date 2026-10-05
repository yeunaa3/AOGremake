using UnityEngine;

namespace AOG.Duel
{
    public sealed class ArrowSkill : SkillData
    {
        [SerializeField] private ShotData shot = new ShotData();

        public override SkillType Type => SkillType.SpawnOnly;

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null) return;
            context.Caster.ShootSkill(shot, context.Slot);
        }
    }
}
