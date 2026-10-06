using System;
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
        private float damageBuff = 1f;
        private float stunTime;
        private float freezeTime;
        private float silenceTime;
        private float slowTime;
        private float slowMultiplier = 1f;
        private float buffTime;
        private float timedMoveMultiplier = 1f;
        private float dodgeChance;
        private float projectileSpeedMultiplier = 1f;
        private float invulnerableTime;
        private float immuneTime;
        private float burnTime;
        private float burnTick;
        private int burnDamage;
        private PlayerController burnAttacker;
        private float poisonTime;
        private float poisonTick;
        private int poisonDamage;
        private PlayerController poisonAttacker;
        private int shieldLayers;
        private int maxShieldLayers;
        private float shieldRechargeTime;
        private float shieldRechargeLeft;

        public int Hp { get; private set; }
        public int MaxHp => maxHp;
        public bool Alive => Hp > 0;
        public float MoveSpeed => moveSpeed * moveBuff * timedMoveMultiplier * slowMultiplier;
        public float AttackSpeed => Mathf.Max(.05f, attackSpeed * gear.BowAttackSpeedMultiplier);
        public float ClipSpeed => AttackSpeed;
        public int Damage => Mathf.RoundToInt(gear.BowDamage * damageBuff);
        public Sprite ArrowSprite => gear.ArrowSprite;
        public bool Frozen => freezeTime > 0f;
        public bool Stunned => stunTime > 0f || Frozen;
        public bool Silenced => silenceTime > 0f;
        public bool CanMove => !Stunned;
        public bool CanAttack => !Stunned;
        public bool CanUseSkill => !Stunned && !Silenced;
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public int ShieldLayers => shieldLayers;
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
            freezeTime = Mathf.Max(0f, freezeTime - Time.deltaTime);
            silenceTime = Mathf.Max(0f, silenceTime - Time.deltaTime);
            invulnerableTime = Mathf.Max(0f, invulnerableTime - Time.deltaTime);
            immuneTime = Mathf.Max(0f, immuneTime - Time.deltaTime);
            slowTime = Mathf.Max(0f, slowTime - Time.deltaTime);
            if (slowTime <= 0f) slowMultiplier = 1f;

            buffTime = Mathf.Max(0f, buffTime - Time.deltaTime);
            if (buffTime <= 0f)
            {
                timedMoveMultiplier = 1f;
                dodgeChance = 0f;
                projectileSpeedMultiplier = 1f;
            }

            TickDamage(ref burnTime, ref burnTick, burnDamage, burnAttacker);
            TickDamage(ref poisonTime, ref poisonTick, poisonDamage, poisonAttacker);
            if (burnTime <= 0f) burnDamage = 0;
            if (poisonTime <= 0f) poisonDamage = 0;

            if (maxShieldLayers > 0 && shieldLayers < maxShieldLayers)
            {
                shieldRechargeLeft -= Time.deltaTime;
                if (shieldRechargeLeft <= 0f)
                {
                    shieldLayers++;
                    shieldRechargeLeft = shieldRechargeTime;
                }
            }
        }

        public void ResetAll()
        {
            Hp = maxHp;
            moveBuff = damageBuff = 1f;
            stunTime = freezeTime = silenceTime = slowTime = buffTime = 0f;
            invulnerableTime = immuneTime = burnTime = poisonTime = 0f;
            burnTick = poisonTick = 0f;
            burnDamage = poisonDamage = 0;
            slowMultiplier = timedMoveMultiplier = projectileSpeedMultiplier = 1f;
            dodgeChance = 0f;
            shieldLayers = maxShieldLayers = 0;
            shieldRechargeLeft = 0f;
            HpChanged?.Invoke(Hp, maxHp);
        }
        public int TakeDamage(DamageInfo hit)
        {
            if (!Alive || hit.Amount <= 0 || invulnerableTime > 0f) return 0;
            if (!hit.IgnoreShield && player.Action.ShieldOn) return 0;
            if (!hit.IgnoreShield && shieldLayers > 0)
            {
                shieldLayers--;
                shieldRechargeLeft = shieldRechargeTime;
                return 0;
            }
            if (dodgeChance > 0f && UnityEngine.Random.value < dodgeChance) return 0;

            int dealt = Mathf.Min(Hp, hit.Amount);
            Hp -= dealt;
            HpChanged?.Invoke(Hp, maxHp);
            if (Alive && immuneTime <= 0f)
            {
                ApplyEffect(hit);
            }
            if (Hp == 0) player.Die(); else player.Hit();
            return dealt;
        }
        public void Heal(int amount)
        {
            if (!Alive || amount <= 0) return;
            Hp = Mathf.Min(maxHp, Hp + amount);
            HpChanged?.Invoke(Hp, maxHp);
        }
        public void Stun(float seconds) => stunTime = Mathf.Max(stunTime, seconds);
        public void Silence(float seconds) => silenceTime = Mathf.Max(silenceTime, seconds);
        public void SetBuff(float move, float damage)
        {
            moveBuff = Mathf.Max(0f, move);
            damageBuff = Mathf.Max(0f, damage);
        }

        public void ApplyBuff(BuffData buff)
        {
            if (buff == null) return;
            if (buff.cleanse) Cleanse();
            Heal(buff.heal);
            buffTime = Mathf.Max(buffTime, buff.duration);
            timedMoveMultiplier = Mathf.Max(0f, buff.moveMultiplier);
            dodgeChance = Mathf.Clamp01(buff.dodgeChance);
            projectileSpeedMultiplier = Mathf.Max(0.01f, buff.projectileSpeedMultiplier);
            if (buff.invulnerable) invulnerableTime = Mathf.Max(invulnerableTime, buff.duration);
            if (buff.statusImmune) immuneTime = Mathf.Max(immuneTime, buff.duration);
        }

        public void StartShieldPassive(int layers, float recharge)
        {
            maxShieldLayers = Mathf.Max(1, layers);
            shieldLayers = maxShieldLayers;
            shieldRechargeTime = Mathf.Max(0.1f, recharge);
            shieldRechargeLeft = shieldRechargeTime;
        }

        public void Cleanse()
        {
            stunTime = freezeTime = silenceTime = slowTime = burnTime = poisonTime = 0f;
            poisonDamage = burnDamage = 0;
            slowMultiplier = 1f;
        }

        private void ApplyEffect(DamageInfo hit)
        {
            switch (hit.Effect)
            {
                case ArrowEffect.Burn:
                    burnDamage = hit.EffectPower;
                    burnTime = hit.EffectDuration;
                    burnTick = 1f;
                    burnAttacker = hit.Attacker;
                    break;
                case ArrowEffect.Poison:
                    poisonDamage += hit.EffectPower;
                    poisonTime = hit.EffectDuration;
                    poisonTick = 1f;
                    poisonAttacker = hit.Attacker;
                    break;
                case ArrowEffect.Freeze:
                    freezeTime = Mathf.Max(freezeTime, hit.EffectDuration);
                    break;
                case ArrowEffect.Stun:
                case ArrowEffect.KnockUp:
                    Stun(hit.EffectDuration);
                    break;
                case ArrowEffect.Silence:
                    Silence(hit.EffectDuration);
                    break;
                case ArrowEffect.Slow:
                    slowMultiplier = Mathf.Clamp(hit.EffectPower / 100f, 0.05f, 1f);
                    slowTime = Mathf.Max(slowTime, hit.EffectDuration);
                    break;
            }
        }

        private void TickDamage(ref float remaining, ref float tick, int damage, PlayerController attacker)
        {
            if (remaining <= 0f || damage <= 0) return;
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick += 1f;
            TakeDamage(new DamageInfo(attacker, damage, true, transform.position));
        }
    }
}
