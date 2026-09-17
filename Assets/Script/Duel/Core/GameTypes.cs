using UnityEngine;

namespace AOG.Duel
{
    public enum GameState
    {
        Waiting,
        Countdown,
        Playing,
        Finished
    }

    public enum ActionState
    {
        Ready,
        AutoAttacking,
        UsingSkill,
        Dashing,
        Shielding,
        Stunned,
        Dead
    }

    public enum DamageResult
    {
        Ignored,
        Blocked,
        Damaged,
        Killed
    }

    public enum ArrowEffect
    {
        None,
        Burn,
        Freeze
    }

    public struct InputCmd
    {
        public float Move;
        public bool DashPressed;
        public bool ShieldPressed;
        public bool Skill1Pressed;
        public bool Skill2Pressed;
        public bool Skill3Pressed;
        public bool Skill4Pressed;

        public bool TryGetSkillSlot(out int slot)
        {
            if (Skill1Pressed) { slot = 0; return true; }
            if (Skill2Pressed) { slot = 1; return true; }
            if (Skill3Pressed) { slot = 2; return true; }
            if (Skill4Pressed) { slot = 3; return true; }

            slot = -1;
            return false;
        }
    }

    public struct DamageInfo
    {
        public PlayerController Attacker;
        public int Amount;
        public bool IgnoreShield;
        public Vector2 HitPoint;
        public ArrowEffect Effect;
        public int EffectPower;
        public float EffectDuration;

        public DamageInfo(PlayerController attacker, int amount, bool ignoreShield, Vector2 hitPoint,
            ArrowEffect effect = ArrowEffect.None, int effectPower = 0, float effectDuration = 0f)
        {
            Attacker = attacker;
            Amount = Mathf.Max(0, amount);
            IgnoreShield = ignoreShield;
            HitPoint = hitPoint;
            Effect = effect;
            EffectPower = Mathf.Max(0, effectPower);
            EffectDuration = Mathf.Max(0f, effectDuration);
        }
    }
}
