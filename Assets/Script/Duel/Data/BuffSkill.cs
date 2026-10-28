using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "Skill/Buff", fileName = "NewBuffSkill")]
    public sealed class BuffSkill : SkillData
    {
        [SerializeField] private BuffData buff = new BuffData();

        public override SkillType Type => SkillType.PlayerOnly;
        public BuffData Buff => buff;

        public override void Begin(SkillCtx context)
        {
            if (context.Caster == null) return;
            if (buff.isRestSkill || buff.healPerSecond > 0)
            {
                context.Caster.StartRestHeal(buff.healPerSecond, buff.healInterval, buff.vfxPrefab);
            }
        }

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null) return;
            context.Caster.Stats.ApplyBuff(buff);
            if (buff.vfxPrefab != null && !buff.isRestSkill && buff.healPerSecond == 0)
            {
                Instantiate(buff.vfxPrefab, context.Caster.transform.position, Quaternion.identity, context.Caster.transform);
            }
        }

        public override void End(SkillCtx context)
        {
            if (context.Caster == null) return;
            if (buff.isRestSkill || buff.healPerSecond > 0)
            {
                context.Caster.StopRestHeal();
            }
        }
    }
}
