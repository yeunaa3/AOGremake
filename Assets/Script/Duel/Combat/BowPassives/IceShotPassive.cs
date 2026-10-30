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
        [Tooltip("Trigger animation dùng để thi triển skill băng (mặc định Mid theo yêu cầu).")]
        [SerializeField] private string animationTrigger = "Mid";

        public int ShotsRequired => shotsRequired;
        public SkillData IceSkill => iceSkill;
        public string AnimationTrigger => animationTrigger;

        public override bool HasChargeRelease => true;
        public override string SpecialAnimationTrigger => "Mid";
        public override SkillData SpecialSkill => iceSkill;

        public override void ExecuteSpecialRelease(PlayerController player, BowRuntimeState state)
        {
            if (state != null)
            {
                state.shotCount = 0;
                state.specialSkillReady = false;
            }

            if (iceSkill != null)
            {
                iceSkill.Use(player.Action.CurrentSkillContext());
            }
            else
            {
                player.ShootArrow(ProjectilePath.Straight, 1f, 1.2f, ArrowEffect.Freeze, 0, 2f);
            }
        }

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot)
        {
            if (arrow == null || player == null || state == null) return;

            if (state.shotCount < shotsRequired)
            {
                state.shotCount++;
                if (state.shotCount >= shotsRequired)
                {
                    state.specialSkillReady = true;
                }
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
