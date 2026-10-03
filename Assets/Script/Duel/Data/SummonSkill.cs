using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Summon", fileName = "Skill_Summon")]
    public sealed class SummonSkill : SkillData
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private bool spawnNearTarget;
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField, Min(0f)] private float spacing = 1f;

        public override SkillKind Kind => SkillKind.Summon;
        public override SkillMoveMode MoveMode => SkillMoveMode.Locked;

        public override void Use(SkillCtx context)
        {
            context.Caster?.SpawnSkillObject(prefab, spawnNearTarget, offset, count, spacing);
        }
    }
}
