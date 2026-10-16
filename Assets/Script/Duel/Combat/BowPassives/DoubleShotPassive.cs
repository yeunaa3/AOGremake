using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Passives/Double Shot", fileName = "DoubleShotPassive")]
    public sealed class DoubleShotPassive : BowPassive
    {
        [Tooltip("Tỉ lệ sinh ra mũi tên bởi mũi tên đi trước (0.3 = 30%).")]
        [SerializeField, Range(0f, 1f)] private float chance = 0.3f;
        [Tooltip("Độ trễ trước khi mũi tên tiếp theo sinh ra bay theo sau mũi tên trước (giây).")]
        [SerializeField, Min(0.01f)] private float delay = 0.08f;
        [Tooltip("Giới hạn số lần sinh liên tiếp tối đa để tránh lặp vô hạn.")]
        [SerializeField, Range(1, 10)] private int maxChainDepth = 5;

        public float Chance => chance;
        public float Delay => delay;
        public int MaxChainDepth => maxChainDepth;

        public override void OnShoot(PlayerController player, BowRuntimeState state, Arrow arrow, bool isSecondaryShot)
        {
            if (arrow == null || player == null || !player.Alive) return;

            // Nội tại ảnh hưởng đến mũi tên: Mũi tên đi trước có 30% sinh ra mũi tên tiếp theo bay ngay sau nó
            TrySpawnFollowerArrow(player, arrow, 1);
        }

        private void TrySpawnFollowerArrow(PlayerController player, Arrow leadingArrow, int currentDepth)
        {
            if (currentDepth > maxChainDepth) return;
            if (Random.value > chance) return;

            player.StartCoroutine(RoutineSpawnFollowerArrow(player, leadingArrow, currentDepth));
        }

        private IEnumerator RoutineSpawnFollowerArrow(PlayerController player, Arrow leadingArrow, int currentDepth)
        {
            yield return new WaitForSeconds(delay);
            if (player == null || !player.Alive || leadingArrow == null) yield break;

            Arrow follower = player.Pool == null ? null : player.Pool.Spawn();
            if (follower == null) yield break;

            Vector2 start = leadingArrow.StartPoint;
            Vector2 end = leadingArrow.EndPoint;
            ProjectilePath path = leadingArrow.Path;
            int damage = leadingArrow.Damage;
            Sprite sprite = leadingArrow.ArrowSprite != null ? leadingArrow.ArrowSprite : player.Stats.ArrowSprite;
            float speed = leadingArrow.ShotSpeedMultiplier;
            ArrowEffect effect = leadingArrow.ArrowEffectType;
            int effectPower = leadingArrow.EffectPower;
            float effectDuration = leadingArrow.EffectDuration;
            float lifeSteal = leadingArrow.LifeSteal;
            GameObject worldEffect = leadingArrow.WorldEffectPrefab;
            int resetSlot = leadingArrow.ResetCooldownSlot;

            follower.Launch(player, start, end, path, 0f, damage, sprite, speed,
                effect, effectPower, effectDuration, lifeSteal, worldEffect, resetSlot);

            // Mũi tên vừa sinh ra tiếp tục có tỉ lệ 30% sinh ra mũi tên tiếp theo sau nó
            TrySpawnFollowerArrow(player, follower, currentDepth + 1);
        }
    }
}
