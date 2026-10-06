using UnityEngine;

namespace AOG.Duel
{
    // Khi bấm skill, lưu buff và áp dụng lần lượt cho N phát đánh thường tiếp theo.
    [CreateAssetMenu(menuName = "Skill/EnhanceArrows", fileName = "NewEnhanceArrows")]
    public sealed class EnhanceArrows : SkillData
    {
        [Header("Các mũi tên tiếp theo")]
        [SerializeField] private ArrowBuffData nextArrows = new ArrowBuffData();

        public override SkillType Type => SkillType.PlayerOnly;

        public override void Use(SkillCtx context)
        {
            if (context.Caster != null)
                context.Caster.ApplyArrowBuff(nextArrows, context.Slot);
        }
    }
}
