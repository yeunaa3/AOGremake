using UnityEngine;

namespace AOG.Duel
{
    // Dùng cho mọi skill tạo projectile hoặc vật thể ngay lúc thi triển.
    [CreateAssetMenu(menuName = "Skill/SummonObject", fileName = "NewSummonObject")]
    public sealed class SummonObject : SkillData
    {
        [Header("Vật thể được gọi ra")]
        [SerializeField] private SpawnData spawn = new SpawnData();
        [Tooltip("Bật nếu projectile cần xuất hiện trên tay trước lúc bắn.")]
        [SerializeField] private bool showInHand;
        [SerializeField, Range(0f, 180f)] private float heldFanAngle = 35f;

        public override SkillType Type => SkillType.SpawnOnly;

        public override void ShowVisual(SkillCtx context)
        {
            if (showInHand && context.Caster != null)
                context.Caster.ShowHeldArrows(spawn.count, heldFanAngle);
        }

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null) return;
            context.Caster.HideHeldArrows();
            context.Caster.Spawn(spawn, context.Slot);
        }

        public override void End(SkillCtx context) => context.Caster?.HideHeldArrows();
    }
}
