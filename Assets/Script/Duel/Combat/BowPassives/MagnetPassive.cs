using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Magnet", fileName = "MagnetPassive")]
    public sealed class MagnetPassive : BowPassive
    {
        [Tooltip("Chu kỳ kích hoạt nam châm (giây).")]
        [SerializeField, Min(1f)] private float interval = 30f;
        [Tooltip("Hệ số nhân tốc độ khi tên phóng thẳng vào địch.")]
        [SerializeField, Min(1f)] private float redirectSpeedMultiplier = 2f;

        public float Interval => interval;
        public float RedirectSpeedMultiplier => redirectSpeedMultiplier;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot)
        {
            if (arrow == null || player == null) return;

            // Giữ nguyên thời gian biến mất bình thường của mũi tên, KHÔNG can thiệp ActiveGroundLifetime.
            // Chỉ theo dõi những mũi tên do chính player này bắn ra.
            if (arrow.Owner == player && !state.activeArrows.Contains(arrow))
            {
                state.activeArrows.Add(arrow);
            }
        }

        public override void OnUpdate(PlayerController player, BowRuntimeState state, float deltaTime)
        {
            if (player == null || !player.Alive || player.Enemy == null) return;

            state.timer += deltaTime;
            if (state.timer >= interval)
            {
                state.timer = 0f;
                Vector2 targetAim = player.Enemy.Aim;
                for (int i = state.activeArrows.Count - 1; i >= 0; i--)
                {
                    Arrow arrow = state.activeArrows[i];
                    // Chỉ hút những mũi tên CỦA MÌNH bắn ra mà CHƯA BIẾN MẤT (đang bay hoặc đang cắm đất chưa biến mất)
                    if (arrow == null || !arrow.gameObject.activeInHierarchy || (!arrow.IsFlying && !arrow.IsStuckInGround) || arrow.Owner != player)
                    {
                        state.activeArrows.RemoveAt(i);
                        continue;
                    }

                    arrow.RedirectStraight(targetAim, redirectSpeedMultiplier);
                }
            }
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            return interval > 0f ? Mathf.Clamp01(state.timer / interval) : 1f;
        }
    }
}
