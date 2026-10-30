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

            int level = GetCasterLevel(context);
            ShotData activeShot = (level <= 1) ? shot : CloneLeveledShot(shot, level);
            context.Caster.ShootSkill(activeShot, context.Slot);
        }

        private ShotData CloneLeveledShot(ShotData src, int level)
        {
            if (src == null) return null;
            return new ShotData
            {
                path = src.path,
                formation = src.formation,
                count = src.count,
                spacing = src.spacing,
                spawnNearTarget = src.spawnNearTarget,
                spawnOffset = src.spawnOffset,
                arc = src.arc,
                damageMultiplier = GetDamageMultiplier(src.damageMultiplier, level),
                speedMultiplier = src.speedMultiplier,
                effect = src.effect,
                effectPower = GetEffectPower(src.effectPower, level),
                effectDuration = src.effectDuration,
                lifeSteal = src.lifeSteal,
                worldEffectPrefab = src.worldEffectPrefab,
                resetCooldownOnHit = src.resetCooldownOnHit,
                onlyHitAirborne = src.onlyHitAirborne
            };
        }

        public override void End(SkillCtx context) => context.Caster?.HideHeldArrows();
    }
}
