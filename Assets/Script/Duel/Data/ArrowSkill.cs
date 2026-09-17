using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Projectile Volley", fileName = "Skill_ProjectileVolley")]
    public sealed class ArrowSkill : SkillData
    {
        [SerializeField, Min(1)] private int projectileCount = 3;
        [SerializeField, Min(0f)] private float delayBetweenProjectiles = 0.08f;
        [SerializeField] private float arcHeightStep = 0.7f;

        public override void Execute(SkillCtx context)
        {
            if (context.Caster == null || context.Target == null)
            {
                return;
            }

            context.Caster.Volley(
                context.Target,
                projectileCount,
                delayBetweenProjectiles,
                arcHeightStep);
        }
    }
}
