using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;

        private DuelCharacter owner;
        private CharacterStats stats;

        public int CurrentHealth { get; private set; }
        public int MaximumHealth => stats != null ? stats.MaximumHealth : maximumHealth;
        public bool IsAlive => CurrentHealth > 0;

        public event Action<int, int> HealthChanged;
        public event Action<DuelDamageInfo, DuelDamageResult> DamageResolved;
        public event Action Died;

        private void Awake()
        {
            owner = GetComponentInParent<DuelCharacter>();
            stats = GetComponentInParent<CharacterStats>();
            CurrentHealth = MaximumHealth;
        }

        public void ResetHealth()
        {
            CurrentHealth = MaximumHealth;
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
        }

        public int Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;

            int before = CurrentHealth;
            CurrentHealth = Mathf.Min(MaximumHealth, CurrentHealth + amount);
            int healed = CurrentHealth - before;
            if (healed > 0) HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            return healed;
        }

        public DuelDamageResult TakeDamage(DuelDamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0)
            {
                DamageResolved?.Invoke(damage, DuelDamageResult.Ignored);
                return DuelDamageResult.Ignored;
            }

            if (!damage.IgnoreShield && owner != null && owner.ActionController.IsShieldActive)
            {
                DamageResolved?.Invoke(damage, DuelDamageResult.Blocked);
                return DuelDamageResult.Blocked;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage.Amount);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);

            DuelDamageResult result = CurrentHealth == 0
                ? DuelDamageResult.Killed
                : DuelDamageResult.Damaged;
            DamageResolved?.Invoke(damage, result);

            if (result == DuelDamageResult.Killed)
            {
                Died?.Invoke();
                owner?.HandleDeath();
            }
            else
            {
                owner?.AnimationController.PlayHit();
            }

            return result;
        }
    }
}
