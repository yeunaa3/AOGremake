using UnityEngine;

namespace AOG.Duel
{
    public sealed class SummonSkill : SkillData
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private bool spawnNearTarget;
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField, Min(0f)] private float spacing = 1f;

        public override SkillType Type => SkillType.SpawnOnly;

        public override void Use(SkillCtx context)
        {
            context.Caster?.SpawnSkillObject(prefab, spawnNearTarget, offset, count, spacing);
        }
    }
}
