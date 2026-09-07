using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class CharacterStats : MonoBehaviour
    {
        [Header("Base stats")]
        [SerializeField, Min(1)] private int maximumHealth = 100;
        [SerializeField, Min(0f)] private float baseMoveSpeed = 5f;
        [Tooltip("Số phát bắn thường mỗi giây trước khi tính cung và buff.")]
        [SerializeField, Min(0.05f)] private float baseAttackSpeed = 1f;
        [Tooltip("Tốc độ đánh mà clip bắn mẫu được thiết kế. Thường để 1.")]
        [SerializeField, Min(0.05f)] private float referenceAnimationAttackSpeed = 1f;

        [Header("Fallback when no bow is equipped")]
        [SerializeField, Min(0)] private int fallbackDamage = 10;

        [Header("References")]
        [SerializeField] private CharacterEquipment equipment;

        private float moveSpeedMultiplier = 1f;
        private float attackSpeedBuffMultiplier = 1f;
        private float damageMultiplier = 1f;

        public int MaximumHealth => maximumHealth;
        public float MoveSpeed => baseMoveSpeed * moveSpeedMultiplier;
        public float AttackSpeed => Mathf.Max(
            0.05f,
            baseAttackSpeed
            * (equipment != null ? equipment.BowAttackSpeedMultiplier : 1f)
            * attackSpeedBuffMultiplier);
        public float AttackInterval => 1f / AttackSpeed;
        public float AttackAnimationSpeed => Mathf.Max(0.05f, AttackSpeed / referenceAnimationAttackSpeed);
        public int BasicAttackDamage => Mathf.Max(
            0,
            Mathf.RoundToInt(
                (equipment != null && equipment.HasBow ? equipment.BowDamage : fallbackDamage)
                * damageMultiplier));
        public DuelProjectileDefinition BasicAttackProjectile => equipment != null
            ? equipment.BowProjectile
            : null;

        public event Action StatsChanged;

        private void Awake()
        {
            if (equipment == null) equipment = GetComponent<CharacterEquipment>();
        }

        private void OnEnable()
        {
            if (equipment != null) equipment.BowChanged += HandleBowChanged;
        }

        private void OnDisable()
        {
            if (equipment != null) equipment.BowChanged -= HandleBowChanged;
        }

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier = Mathf.Max(0f, multiplier);
            StatsChanged?.Invoke();
        }

        public void SetAttackSpeedBuffMultiplier(float multiplier)
        {
            attackSpeedBuffMultiplier = Mathf.Max(0.05f, multiplier);
            StatsChanged?.Invoke();
        }

        public void SetDamageMultiplier(float multiplier)
        {
            damageMultiplier = Mathf.Max(0f, multiplier);
            StatsChanged?.Invoke();
        }

        public void ResetRuntimeModifiers()
        {
            moveSpeedMultiplier = 1f;
            attackSpeedBuffMultiplier = 1f;
            damageMultiplier = 1f;
            StatsChanged?.Invoke();
        }

        private void HandleBowChanged(BowDefinition _)
        {
            StatsChanged?.Invoke();
        }
    }
}
