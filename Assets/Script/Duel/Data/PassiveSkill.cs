using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Passive Shield", fileName = "Skill_PassiveShield")]
    public sealed class PassiveSkill : SkillData
    {
        [SerializeField, Min(1)] private int maxLayers = 3;
        [SerializeField, Min(0.1f)] private float rechargeTime = 5f;

        public override SkillKind Kind => SkillKind.Passive;
        public override SkillMoveMode MoveMode => SkillMoveMode.Free;

        public override void Use(SkillCtx context)
        {
            context.Caster?.Stats.StartShieldPassive(maxLayers, rechargeTime);
        }
    }
}
