using UnityEngine;

namespace AOG.Duel
{
    public sealed class MoveSkill : SkillData
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 10f;
        [SerializeField] private bool shootOnEvent;
        [SerializeField] private ShotData shot = new ShotData();

        public override SkillType Type => SkillType.PlayerAndSpawn;

        public override void Begin(SkillCtx context)
        {
            context.Caster?.StartSkillMove(context.Direction, moveSpeed);
        }

        public override void Use(SkillCtx context)
        {
            if (shootOnEvent && context.Caster != null)
                context.Caster.ShootSkill(shot, context.Slot);
        }
    }
}
