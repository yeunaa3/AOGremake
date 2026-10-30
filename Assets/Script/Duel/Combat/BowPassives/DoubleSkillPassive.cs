using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Double Skill", fileName = "DoubleSkillPassive")]
    public sealed class DoubleSkillPassive : BowPassive
    {
        [Tooltip("Số lần dùng skill trước khi kích hoạt nhân đôi ở lần tiếp theo (mặc định 3).")]
        [SerializeField, Min(1)] private int skillsBeforeDouble = 3;

        private SkillData pendingSkill;
        private float pendingDirection;

        public int SkillsBeforeDouble => skillsBeforeDouble;

        public override void OnEquip(PlayerController player, BowRuntimeState state)
        {
            pendingSkill = null;
            pendingDirection = 0f;
            state.shotCount = 0;
        }

        public override void OnUnequip(PlayerController player, BowRuntimeState state)
        {
            pendingSkill = null;
            pendingDirection = 0f;
            state.shotCount = 0;
        }

        public override void OnSkillUsed(PlayerController player, BowRuntimeState state, SkillData skill, int slot, float direction)
        {
            if (player == null || skill == null || !player.Alive) return;

            state.shotCount++;
            if (state.shotCount > skillsBeforeDouble)
            {
                // Lần dùng skill thứ 4: ghi nhận để ép nhân vật tung tiếp skill này lần nữa
                pendingSkill = skill;
                pendingDirection = direction;
                state.shotCount = 0; // Reset tích lũy cho chu kỳ tiếp theo
            }
        }

        public override void OnSkillEnded(PlayerController player, BowRuntimeState state, SkillData skill)
        {
            if (player == null || !player.Alive || pendingSkill == null) return;

            SkillData toDuplicate = pendingSkill;
            float dir = pendingDirection;
            pendingSkill = null;

            // Ép nhân vật bắn tiếp skill lần thứ 2 liên tiếp ngay lập tức
            player.Action.TriggerSpecialSkill(toDuplicate, toDuplicate.AnimationTrigger, dir);
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            return skillsBeforeDouble > 0 ? Mathf.Clamp01((float)state.shotCount / skillsBeforeDouble) : 1f;
        }
    }
}
