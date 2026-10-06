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

    public enum SkillType { PlayerOnly, SpawnOnly, PlayerAndSpawn }

    public enum SpawnPoint { Bow, AbovePlayer, AboveEnemy }

    public enum ProjectilePath { NormalArc, Straight, Falling, Beam }
    public enum ShotFormation { Fan, ParallelVertical, ParallelHorizontal }

    [System.Serializable]
    public sealed class SpawnData
    {
        public GameObject prefab;
        public SpawnPoint spawnPoint = SpawnPoint.Bow;
        [Min(1)] public int count = 1;
        public Vector2 offset;
        [Tooltip("Bán kính các điểm đích quanh Enemy. N tên sẽ chia đều từ -Radius đến +Radius.")]
        [Min(0f)] public float targetRadius;
        [Tooltip("Khoảng cách giữa các vật thể không phải projectile.")]
        [Min(0f)] public float spacing;
        public ProjectilePath path = ProjectilePath.NormalArc;
        public float arc;
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0.01f)] public float speedMultiplier = 1f;
        [Header("Hiệu ứng khi trúng")]
        public bool hasEffect;
        public ArrowEffect effect;
        [Tooltip("Sát thương mỗi lần của độc/lửa, hoặc phần trăm tốc độ còn lại của Slow.")]
        [Min(0)] public int effectPower;
        [Min(0f)] public float effectDuration;
    }

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

        [Header("Use")]
        [Tooltip("Tên Trigger của State animation skill. Để trống nếu skill không có animation.")]
        [SerializeField] private string animationTrigger;
        [SerializeField] private bool lockMovement = true;
        [SerializeField] private bool cancelOnMove;
        [Header("Timing")]
        [InspectorName("Show Arrow Time")]
        [Tooltip("Giây trong clip mà hình mũi tên xuất hiện. Đặt -1 nếu skill không có hình cần hiện.")]
        [SerializeField, Min(-1f)] private float showTime = -1f;
        [InspectorName("Shoot Time")]
        [Tooltip("Giây trong clip mà tác dụng skill xảy ra hoặc projectile được bắn.")]
        [SerializeField, Min(0f)] private float effectTime = .25f;
        [InspectorName("Duration")]
        [Tooltip("Tổng thời lượng clip trước khi được dùng hành động khác.")]
        [SerializeField, Min(.01f)] private float duration = .6f;

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public SkillRarity Rarity => rarity;
        public float Cooldown => cooldown;
        public string AnimationTrigger => animationTrigger;
        public bool LockMovement => lockMovement;
        public bool CancelOnMove => cancelOnMove;
        public float EffectTime => effectTime;
        public float Duration => duration;
        public virtual bool Passive => false;
        public virtual bool UseOnce => true;
        public float ShowVisualTime => showTime;
        public abstract SkillType Type { get; }
        public virtual void Begin(SkillCtx context) { }
        public virtual void ShowVisual(SkillCtx context) { }
        public virtual void Tick(SkillCtx context, float deltaTime) { }
        public abstract void Use(SkillCtx context);
        public virtual void End(SkillCtx context) { }
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
