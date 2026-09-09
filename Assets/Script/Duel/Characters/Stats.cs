using System;
using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Gear))]
    public sealed class Stats : MonoBehaviour
    {
        [Header("Base stats")]
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0f)] private float baseMoveSpeed = 5f;
        [Tooltip("Số phát bắn thường mỗi giây, trước khi tính cung và buff.")]
        [SerializeField, Min(0.05f)] private float baseAttackSpeed = 1f;
        [Tooltip("Tốc đánh của clip mẫu. Thường để 1.")]
        [SerializeField, Min(0.05f)] private float referenceAnimationAttackSpeed = 1f;

        private Gear gear;
        private float moveSpeedMultiplier = 1f;
        private float attackSpeedBuffMultiplier = 1f;
        private float damageMultiplier = 1f;

        public int MaximumHealth => maximumHealth;
        public float MoveSpeed => baseMoveSpeed * moveSpeedMultiplier;
        public float AttackSpeed => Mathf.Max(0.05f, baseAttackSpeed * gear.BowAttackSpeedMultiplier * attackSpeedBuffMultiplier);
        public float AttackInterval => 1f / AttackSpeed;
        public float AttackAnimationSpeed => Mathf.Max(0.05f, AttackSpeed / referenceAnimationAttackSpeed);
        public int BasicAttackDamage => Mathf.Max(0, Mathf.RoundToInt(gear.BowDamage * damageMultiplier));
        public ArrowData BasicAttackProjectile => gear.BowProjectile;
        public event Action StatsChanged;

        private void Awake() => gear = GetComponent<Gear>();
        private void OnEnable() { if (gear != null) gear.BowChanged += HandleBowChanged; }
        private void OnDisable() { if (gear != null) gear.BowChanged -= HandleBowChanged; }
        public void SetMoveSpeedMultiplier(float value) { moveSpeedMultiplier = Mathf.Max(0f, value); StatsChanged?.Invoke(); }
        public void SetAttackSpeedBuffMultiplier(float value) { attackSpeedBuffMultiplier = Mathf.Max(0.05f, value); StatsChanged?.Invoke(); }
        public void SetDamageMultiplier(float value) { damageMultiplier = Mathf.Max(0f, value); StatsChanged?.Invoke(); }
        public void ResetRuntimeModifiers()
        {
            moveSpeedMultiplier = 1f; attackSpeedBuffMultiplier = 1f; damageMultiplier = 1f; StatsChanged?.Invoke();
        }
        private void HandleBowChanged(BowData _) => StatsChanged?.Invoke();
    }
}