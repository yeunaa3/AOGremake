using System;
using UnityEngine;

namespace AOG.Duel
{
    public sealed class DuelHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;

        private DuelCharacter owner;

        public int CurrentHealth { get; private set; }
        public int MaximumHealth => maximumHealth;
        public bool IsAlive => CurrentHealth > 0;

        public event Action<int, int> HealthChanged;
        public event Action<DuelDamageInfo, DuelDamageResult> DamageResolved;
        public event Action Died;

        private void Awake()
        {
            owner = GetComponentInParent<DuelCharacter>();
            CurrentHealth = maximumHealth;
        }

        public void ResetHealth()
        {
            CurrentHealth = maximumHealth;
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
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
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);

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
