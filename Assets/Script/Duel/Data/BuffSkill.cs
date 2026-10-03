using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Buff", fileName = "Skill_Buff")]
    public sealed class BuffSkill : SkillData
    {
        [SerializeField] private BuffData buff = new BuffData();

        public override SkillKind Kind => SkillKind.Buff;
        public override SkillMoveMode MoveMode => SkillMoveMode.Free;

        public override void Use(SkillCtx context)
        {
            if (context.Caster != null) context.Caster.Stats.ApplyBuff(buff);
        }
    }
}
