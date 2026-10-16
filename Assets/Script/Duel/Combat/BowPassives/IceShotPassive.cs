using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Ice Shot", fileName = "IceShotPassive")]
    public sealed class IceShotPassive : BowPassive
    {
        [Tooltip("Số phát bắn tích lũy để kích hoạt skill băng.")]
        [SerializeField, Min(1)] private int shotsRequired = 10;
        [Tooltip("Kỹ năng băng được gọi ra khi tích đủ phát bắn (mặc định FreezeArrow).")]
        [SerializeField] private SkillData iceSkill;
        [Tooltip("Trigger animation dùng để thi triển skill băng (mặc định Low).")]
        [SerializeField] private string animationTrigger = "Low";

        public int ShotsRequired => shotsRequired;
        public SkillData IceSkill => iceSkill;
        public string AnimationTrigger => animationTrigger;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isSecondaryShot)
        {
            if (isSecondaryShot || arrow == null || player == null) return;

            state.shotCount++;
            if (state.shotCount >= shotsRequired)
            {
                state.shotCount = 0;
                state.specialSkillReady = true;
            }
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            if (state.specialSkillReady) return 1f;
            return shotsRequired > 0 ? Mathf.Clamp01((float)state.shotCount / shotsRequired) : 1f;
        }
    }
}
