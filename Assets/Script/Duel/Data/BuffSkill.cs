using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "Skill/Buff", fileName = "NewBuffSkill")]
    public sealed class BuffSkill : SkillData
    {
        [SerializeField] private BuffData buff = new BuffData();

        public override SkillType Type => SkillType.PlayerOnly;

        public override void Use(SkillCtx context)
        {
            if (context.Caster != null) context.Caster.Stats.ApplyBuff(buff);
        }
    }
}
