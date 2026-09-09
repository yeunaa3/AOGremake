using System;
using UnityEngine;

namespace AOG.Duel
{
    [RequireComponent(typeof(Player), typeof(Stats))]
    public sealed class Health : MonoBehaviour
    {
        private Player player;
        private Stats stats;
        public int CurrentHealth { get; private set; }
        public int MaximumHealth => stats.MaximumHealth;
        public bool IsAlive => CurrentHealth > 0;
        public event Action<int, int> HealthChanged;
        public event Action<DamageInfo, DamageResult> DamageResolved;
        public event Action Died;

        private void Awake()
        {
            player = GetComponent<Player>();
            stats = GetComponent<Stats>();
            CurrentHealth = MaximumHealth;
        }
        public void ResetHealth() { CurrentHealth = MaximumHealth; HealthChanged?.Invoke(CurrentHealth, MaximumHealth); }
        public int Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;
            int before = CurrentHealth;
            CurrentHealth = Mathf.Min(MaximumHealth, CurrentHealth + amount);
            int healed = CurrentHealth - before;
            if (healed > 0) HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            return healed;
        }
        public DamageResult TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0) { DamageResolved?.Invoke(damage, DamageResult.Ignored); return DamageResult.Ignored; }
            if (!damage.IgnoreShield && player.ActionController.IsShieldActive) { DamageResolved?.Invoke(damage, DamageResult.Blocked); return DamageResult.Blocked; }
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage.Amount);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            DamageResult result = CurrentHealth == 0 ? DamageResult.Killed : DamageResult.Damaged;
            DamageResolved?.Invoke(damage, result);
            if (result == DamageResult.Killed) { Died?.Invoke(); player.HandleDeath(); }
            else player.Anim.PlayHit();
            return result;
        }
    }
}