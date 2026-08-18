using UnityEngine;

namespace AOG.Duel
{
    public enum DuelMatchState
    {
        Waiting,
        Countdown,
        Playing,
        Finished
    }

    public enum DuelActionState
    {
        Ready,
        AutoAttacking,
        UsingSkill,
        Dashing,
        Shielding,
        Stunned,
        Dead
    }

    public enum DuelDamageResult
    {
        Ignored,
        Blocked,
        Damaged,
        Killed
    }

    public struct DuelPlayerCommand
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

    public struct DuelDamageInfo
    {
        public DuelCharacter Attacker;
        public int Amount;
        public bool IgnoreShield;
        public Vector2 HitPoint;

        public DuelDamageInfo(DuelCharacter attacker, int amount, bool ignoreShield, Vector2 hitPoint)
        {
            Attacker = attacker;
            Amount = Mathf.Max(0, amount);
            IgnoreShield = ignoreShield;
            HitPoint = hitPoint;
        }
    }
}
