using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Enhance Arrows", fileName = "Skill_EnhanceArrows")]
    public sealed class PowerArrowSkill : SkillData
    {
        [SerializeField] private ArrowBuffData arrowBuff = new ArrowBuffData();

        public override SkillKind Kind => SkillKind.EnhanceArrow;
        public override SkillMoveMode MoveMode => SkillMoveMode.Free;

        public override void Use(SkillCtx context)
        {
            if (context.Caster != null)
                context.Caster.ApplyArrowBuff(arrowBuff, context.Slot);
        }
    }
}
