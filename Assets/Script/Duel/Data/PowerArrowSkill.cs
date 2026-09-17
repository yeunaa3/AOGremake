using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Power Next Arrow", fileName = "Skill_PowerArrow")]
    public sealed class PowerArrowSkill : SkillData
    {
        [SerializeField] private ArrowEffect effect = ArrowEffect.Burn;
        [SerializeField, Min(0)] private int effectPower = 3;
        [SerializeField, Min(0f)] private float effectDuration = 3f;

        public override void Execute(SkillCtx context)
        {
            if (context.Caster != null)
                context.Caster.PowerNextArrow(effect, effectPower, effectDuration);
        }
    }
}
