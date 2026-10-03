using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Channel", fileName = "Skill_Channel")]
    public sealed class ChannelSkill : SkillData
    {
        [SerializeField, Min(0)] private int healPerTick = 2;
        [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;

        public float TickInterval => tickInterval;
        public override SkillKind Kind => SkillKind.Channel;
        public override SkillMoveMode MoveMode => SkillMoveMode.CancelOnMove;

        public override void Use(SkillCtx context)
        {
            context.Caster?.Stats.Heal(healPerTick);
        }
    }
}
