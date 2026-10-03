using UnityEngine;

namespace AOG.Duel
{
    public enum SkillRarity
    {
        Common,
        Rare,
        Heroic,
        Legendary
    }

    public enum SkillKind { EnhanceArrow, Shoot, Summon, Move, Buff, Channel, Passive }

    public enum SkillMoveMode { Free, Locked, Directional, CancelOnMove }

    // Chỉ xác định State nào trong Player Animator sẽ chạy.
    // Clip thật được đặt hoàn toàn trong Animator, không đặt trong SkillData.
    public enum SkillAnimation
    {
        None,
        ArcShot,
        StraightShot,
        SkyShot,
        Flip,
        JumpShot,
        Summon,
        Buff,
        Channel,
        Move,
        Beam
    }

    public enum ProjectilePath { NormalArc, Straight, Falling, Beam }
    public enum ShotFormation { Fan, ParallelVertical, ParallelHorizontal }

    [System.Serializable]
    public sealed class ShotData
    {
        public ProjectilePath path = ProjectilePath.NormalArc;
        public ShotFormation formation = ShotFormation.ParallelVertical;
        [Min(1)] public int count = 1;
        [Min(0f)] public float spacing = 0.6f;
        public bool spawnNearTarget;
        public Vector2 spawnOffset;
        public float arc;
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0.01f)] public float speedMultiplier = 1f;
        public ArrowEffect effect;
        [Min(0)] public int effectPower;
        [Min(0f)] public float effectDuration;
        [Range(0f, 1f)] public float lifeSteal;
        public GameObject worldEffectPrefab;
        public bool resetCooldownOnHit;
        public bool onlyHitAirborne;
    }

    [System.Serializable]
    public sealed class ArrowBuffData
    {
        [Min(1)] public int arrowCount = 1;
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0.01f)] public float speedMultiplier = 1f;
        public ArrowEffect effect;
        [Min(0)] public int effectPower;
        [Min(0f)] public float effectDuration;
        [Range(0f, 1f)] public float lifeSteal;
        public GameObject worldEffectPrefab;
        public bool resetCooldownOnHit;
    }

    [System.Serializable]
    public sealed class BuffData
    {
        [Min(0)] public int heal;
        [Min(0f)] public float duration;
        [Min(0f)] public float moveMultiplier = 1f;
        [Range(0f, 1f)] public float dodgeChance;
        [Min(0.01f)] public float projectileSpeedMultiplier = 1f;
        public bool invulnerable;
        public bool cleanse;
        public bool statusImmune;
    }

    public abstract class SkillData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillId = "skill_id";
        [SerializeField] private string displayName = "Skill";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private SkillRarity rarity = SkillRarity.Common;

        [Header("Cooldown")]
        [Tooltip("Bắt đầu tính ngay khi bấm skill.")]
        [SerializeField, Min(0f)] private float cooldown = 5f;

        [Header("Animation")]
        [Tooltip("Chọn kiểu chuyển động đã được tạo thành State trong Player Animator.")]
        [SerializeField] private SkillAnimation animation = SkillAnimation.ArcShot;

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public SkillRarity Rarity => rarity;
        public float Cooldown => cooldown;
        public SkillAnimation Animation => animation;
        public abstract SkillKind Kind { get; }
        public abstract SkillMoveMode MoveMode { get; }
        public virtual float MoveSpeed => 0f;
        public abstract void Use(SkillCtx context);
    }

    public struct SkillCtx
    {
        public PlayerController Caster;
        public PlayerController Target;
        public int Slot;
        public float Direction;

        public SkillCtx(PlayerController caster, PlayerController target, int slot, float direction)
        {
            Caster = caster;
            Target = target;
            Slot = slot;
            Direction = direction;
        }
    }
}
