using UnityEngine;

namespace AOG.Duel
{
    public enum MovementShotType { Jump, Dash }

    [CreateAssetMenu(menuName = "Skill/Movement Shot", fileName = "NewMovementShot")]
    public sealed class MoveSkill : SkillData
    {
        [Header("Movement")]
        [SerializeField] private MovementShotType movement = MovementShotType.Jump;
        [SerializeField, Min(0.1f)] private float dashSpeed = 12f;
        [SerializeField, Min(0f)] private float jumpHorizontalSpeed;
        [SerializeField, Min(0.1f)] private float jumpForce = 12f;
        [Header("Shot")]
        [SerializeField] private bool showArrowInHand = true;
        [SerializeField] private ShotData shot = new ShotData();

        public MovementShotType Movement => movement;
        public override SkillType Type => SkillType.PlayerAndSpawn;

        public override void Begin(SkillCtx context)
        {
            if (context.Caster == null) return;
            if (movement == MovementShotType.Jump)
                context.Caster.StartSkillJump(context.Direction, jumpHorizontalSpeed, jumpForce);
            else
                context.Caster.StartSkillMove(context.Direction, dashSpeed);
        }

        public override void ShowVisual(SkillCtx context)
        {
            if (showArrowInHand && context.Caster != null)
                context.Caster.ShowHeldArrows(shot.count, 0f);
        }

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null) return;
            context.Caster.HideHeldArrows();
            context.Caster.ShootSkill(shot, context.Slot);
        }

        public override void End(SkillCtx context) => context.Caster?.HideHeldArrows();
    }
}
