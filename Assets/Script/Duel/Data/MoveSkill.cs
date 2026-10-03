using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Directional Move", fileName = "Skill_DirectionalMove")]
    public sealed class MoveSkill : SkillData
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 10f;
        [SerializeField] private bool shootOnEvent;
        [SerializeField] private ShotData shot = new ShotData();

        public override SkillKind Kind => SkillKind.Move;
        public override SkillMoveMode MoveMode => SkillMoveMode.Directional;
        public override float MoveSpeed => moveSpeed;

        public override void Use(SkillCtx context)
        {
            if (shootOnEvent && context.Caster != null)
                context.Caster.ShootSkill(shot, context.Slot);
        }
    }
}
