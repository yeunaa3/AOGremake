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
        public override int VolleyCount => spawn != null ? Mathf.Max(1, spawn.volleyCount) : 1;
        public override float VolleyInterval => spawn != null ? spawn.volleyInterval : 0f;
        public override float EffectiveDrawSpeed => (spawn != null && spawn.volleyDrawSpeed > 0.05f) ? spawn.volleyDrawSpeed : DrawSpeed;

        public override void ShowVisual(SkillCtx context)
        {
            if (showInHand && context.Caster != null)
                context.Caster.ShowHeldArrows(spawn.count, heldFanAngle);
        }

        public override void Use(SkillCtx context)
        {
            if (context.Caster == null) return;
            context.Caster.HideHeldArrows();

            int level = GetCasterLevel(context);
            if (level <= 1)
            {
                // Cấp 1: giữ nguyên 100% thông số gốc người dùng thiết lập
                context.Caster.SpawnSingleVolley(spawn, context.Slot);
            }
            else
            {
                // Cấp > 1: tính toán theo công thức cấp độ
                SpawnData leveled = CloneLeveledSpawn(spawn, level);
                context.Caster.SpawnSingleVolley(leveled, context.Slot);
            }
        }

        private SpawnData CloneLeveledSpawn(SpawnData src, int level)
        {
            if (src == null) return null;
            return new SpawnData
            {
                prefab = src.prefab,
                spawnPoint = src.spawnPoint,
                count = src.count,
                offset = src.offset,
                targetRadius = src.targetRadius,
                volleyCount = src.volleyCount,
                volleyInterval = src.volleyInterval,
                volleyDrawSpeed = src.volleyDrawSpeed,
                randomizeRain = src.randomizeRain,
                horizontalRandomness = src.horizontalRandomness,
                spawnHeightVariation = src.spawnHeightVariation,
                objectSpacing = src.objectSpacing,
                path = src.path,
                arc = src.arc,
                damageMultiplier = GetDamageMultiplier(src.damageMultiplier, level),
                speedMultiplier = src.speedMultiplier,
                groundDuration = src.groundDuration,
                hasEffect = src.hasEffect,
                effect = src.effect,
                effectPower = GetEffectPower(src.effectPower, level),
                effectDuration = src.effectDuration,
                onHitPrefab = src.onHitPrefab
            };
        }

        public override void End(SkillCtx context) => context.Caster?.HideHeldArrows();
    }
}
