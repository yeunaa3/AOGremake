using UnityEngine;

namespace AOG.Duel
{
    public sealed class ChannelSkill : SkillData
    {
        [SerializeField, Min(0)] private int healPerTick = 2;
        [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;

        private float tickLeft;
        public override SkillType Type => SkillType.PlayerOnly;
        public override bool UseOnce => false;

        public override void Begin(SkillCtx context) => tickLeft = 0f;

        public override void Tick(SkillCtx context, float deltaTime)
        {
            tickLeft -= deltaTime;
            if (tickLeft > 0f) return;
            tickLeft = tickInterval;
            Use(context);
        }

        public override void Use(SkillCtx context)
        {
            context.Caster?.Stats.Heal(healPerTick);
        }
    }
}
