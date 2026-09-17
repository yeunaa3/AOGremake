using System;
using System.Collections;
using UnityEngine;

namespace AOG.Duel
{
    // Toàn bộ dữ liệu của nhân vật: HP, chỉ số, buff, stun và silence.
    [RequireComponent(typeof(Gear))]
    public sealed class PlayerStats : MonoBehaviour
    {
        [Header("Chỉ số gốc")]
        [SerializeField, Min(1)] private int maxHp = 100;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(.05f)] private float attackSpeed = 1f;

        private PlayerController player;
        private Gear gear;
        private float moveBuff = 1f;
        private float attackBuff = 1f;
        private float damageBuff = 1f;
        private float stunTime;
        private float silenceTime;
        private Coroutine burnJob;

        public int Hp { get; private set; }
        public int MaxHp => maxHp;
        public bool Alive => Hp > 0;
        public float MoveSpeed => moveSpeed * moveBuff;
        public float AttackSpeed => Mathf.Max(.05f, attackSpeed * gear.BowAttackSpeedMultiplier * attackBuff);
        public float ClipSpeed => AttackSpeed;
        public int Damage => Mathf.RoundToInt(gear.BowDamage * damageBuff);
        public Sprite ArrowSprite => gear.ArrowSprite;
        public bool Stunned => stunTime > 0f;
        public bool Silenced => silenceTime > 0f;
        public bool CanMove => !Stunned;
        public bool CanAttack => !Stunned;
        public bool CanUseSkill => !Stunned && !Silenced;
        public event Action<int, int> HpChanged;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            gear = GetComponent<Gear>();
            Hp = maxHp;
        }
        private void Update()
        {
            stunTime = Mathf.Max(0f, stunTime - Time.deltaTime);
            silenceTime = Mathf.Max(0f, silenceTime - Time.deltaTime);
        }

        public void ResetAll()
        {
            if (burnJob != null) StopCoroutine(burnJob);
            burnJob = null;
            Hp = maxHp;
            moveBuff = attackBuff = damageBuff = 1f;
            stunTime = silenceTime = 0f;
            HpChanged?.Invoke(Hp, maxHp);
        }
        public void TakeDamage(DamageInfo hit)
        {
            if (!Alive || hit.Amount <= 0 || (!hit.IgnoreShield && player.Action.ShieldOn)) return;
            Hp = Mathf.Max(0, Hp - hit.Amount);
            HpChanged?.Invoke(Hp, maxHp);
            if (Alive && hit.Effect == ArrowEffect.Burn)
            {
                if (burnJob != null) StopCoroutine(burnJob);
                burnJob = StartCoroutine(Burn(hit.Attacker, hit.EffectPower, hit.EffectDuration));
            }
            else if (Alive && hit.Effect == ArrowEffect.Freeze)
            {
                Stun(hit.EffectDuration);
            }
            if (Hp == 0) player.Die(); else player.Hit();
        }
        public void Heal(int amount)
        {
            if (!Alive || amount <= 0) return;
            Hp = Mathf.Min(maxHp, Hp + amount);
            HpChanged?.Invoke(Hp, maxHp);
        }
        public void Stun(float seconds) => stunTime = Mathf.Max(stunTime, seconds);
        public void Silence(float seconds) => silenceTime = Mathf.Max(silenceTime, seconds);
        public void SetBuff(float move, float attack, float damage)
        {
            moveBuff = Mathf.Max(0f, move);
            attackBuff = Mathf.Max(.05f, attack);
            damageBuff = Mathf.Max(0f, damage);
        }

        private IEnumerator Burn(PlayerController attacker, int damage, float duration)
        {
            int ticks = Mathf.CeilToInt(duration);
            for (int i = 0; i < ticks && Alive; i++)
            {
                yield return new WaitForSeconds(1f);
                TakeDamage(new DamageInfo(attacker, damage, false, transform.position));
            }
            burnJob = null;
        }
    }
}
