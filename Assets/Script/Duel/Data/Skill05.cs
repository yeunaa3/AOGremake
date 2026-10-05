using UnityEngine;

namespace AOG.Duel
{
    // Skill 05: bắn N projectile cùng lúc, chia đều quanh điểm ngắm của Enemy.
    [CreateAssetMenu(menuName = "AOG/Duel/Skills/Spawn Only/05 Multi Arrow", fileName = "Skill05_MultiArrow")]
    public sealed class Skill05 : SkillData
    {
        [Header("Vật thể được bắn")]
        [SerializeField] private SpawnData spawn = new SpawnData();
        [Tooltip("Góc xòe của N hình mũi tên đang được cầm trên tay.")]
        [SerializeField, Range(0f, 180f)] private float heldFanAngle = 35f;

        public override SkillType Type => SkillType.SpawnOnly;

        public override void ShowVisual(SkillCtx context)
        {
            if (context.Caster != null)
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
