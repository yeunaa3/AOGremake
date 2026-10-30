using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Time Freeze", fileName = "TimeFreezePassive")]
    public sealed class TimeFreezePassive : BowPassive
    {
        [Tooltip("Thời gian hồi nội tại (giây).")]
        [SerializeField, Min(1f)] private float cooldown = 25f;
        [Tooltip("Thời gian hiệu lực ngừng đọng thời gian (giây).")]
        [SerializeField, Min(0.5f)] private float duration = 5f;
        [Tooltip("Hệ số nhân tốc độ mũi tên khi bị làm chậm 10x (0.1 = giảm 10 lần).")]
        [SerializeField, Range(0.01f, 1f)] private float slowMultiplier = 0.1f;

        private bool isActive;
        private float activeTimer;

        public float Cooldown => cooldown;
        public float Duration => duration;
        public float SlowMultiplier => slowMultiplier;
        public bool IsActive => isActive;

        public override void OnEquip(PlayerController player, BowRuntimeState state)
        {
            isActive = false;
            activeTimer = 0f;
            state.timer = 0f;
        }

        public override void OnUnequip(PlayerController player, BowRuntimeState state)
        {
            isActive = false;
            activeTimer = 0f;
            state.timer = 0f;
            RestoreArrows(player);
        }

        public override void OnUpdate(PlayerController player, BowRuntimeState state, float deltaTime)
        {
            if (player == null || !player.Alive) return;

            if (isActive)
            {
                activeTimer -= deltaTime;
                SlowPlayerArrows(player);

                if (activeTimer <= 0f)
                {
                    isActive = false;
                    activeTimer = 0f;
                    RestoreArrows(player);
                }
            }
            else
            {
                state.timer += deltaTime;
                if (state.timer >= cooldown)
                {
                    state.timer = 0f;
                    isActive = true;
                    activeTimer = duration;
                    SlowPlayerArrows(player);
                }
            }
        }

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isNormalShot)
        {
            if (isActive && arrow != null && arrow.Owner == player)
            {
                arrow.TimeScale = slowMultiplier;
            }
        }

        private void SlowPlayerArrows(PlayerController player)
        {
            if (player == null || player.Pool == null) return;
            foreach (Arrow arrow in player.Pool.ActiveArrows)
            {
                if (arrow != null && arrow.Owner == player && arrow.IsFlying && !arrow.IsStuckInGround)
                {
                    arrow.TimeScale = slowMultiplier;
                }
            }
        }

        private void RestoreArrows(PlayerController player)
        {
            if (player == null || player.Pool == null) return;
            foreach (Arrow arrow in player.Pool.ActiveArrows)
            {
                if (arrow != null && arrow.Owner == player)
                {
                    arrow.TimeScale = 1.0f;
                }
            }
        }

        public override float GetProgress(BowRuntimeState state)
        {
            if (state == null) return 0f;
            if (isActive) return 1f;
            return cooldown > 0f ? Mathf.Clamp01(state.timer / cooldown) : 1f;
        }
    }
}
