using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Shoot", fileName = "Skill_Shoot")]
    public sealed class ArrowSkill : SkillData
    {
        [SerializeField] private ShotData shot = new ShotData();

        public override SkillKind Kind => SkillKind.Shoot;
        public override SkillMoveMode MoveMode => SkillMoveMode.Locked;

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null) return;
            context.Caster.ShootSkill(shot, context.Slot);
        }
    }
}
