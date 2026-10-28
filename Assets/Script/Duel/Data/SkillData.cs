using UnityEngine;
using UnityEngine.Serialization;

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
        [Tooltip("Bán kính điểm đích quanh địch. Falling rải theo chiều ngang; các đường bay khác rải theo chiều dọc.")]
        [Min(0f)] public float targetRadius;
        [Header("Bắn Nhiều Đợt (Multi-Volley / Multi-Burst)")]
        [Tooltip("Số đợt bắn liên tiếp (M lần). 1 = bắn 1 đợt như cũ. Nếu > 1, sẽ bắn N mũi tên M lần.")]
        [Min(1)] public int volleyCount = 1;
        [Tooltip("Khoảng cách thời gian giữa mỗi đợt bắn (giây).")]
        [Min(0.01f)] public float volleyInterval = 0.15f;
        [Header("Arrow Rain (Falling Path Only)")]
        [FormerlySerializedAs("randomizeFalling")]
        public bool randomizeRain;
        [FormerlySerializedAs("fallingScatter")]
        [Tooltip("Chỉ áp dụng khi bật ngẫu nhiên mưa tên. 0: mỗi tên nằm giữa ô. 1: chọn vị trí bất kỳ trong ô. Không thay đổi độ rộng vùng mưa.")]
        [Range(0f, 1f)] public float horizontalRandomness = .8f;
        [FormerlySerializedAs("fallingHeightJitter")]
        [Tooltip("Chỉ áp dụng khi bật ngẫu nhiên mưa tên. Ví dụ 2: mỗi tên xuất hiện cao hoặc thấp hơn tối đa 2 đơn vị. Tạo cùng lúc nhưng chạm đất lệch nhịp, không đổi tốc độ bay.")]
        [Min(0f)] public float spawnHeightVariation = 1.5f;
        [Header("Summoned Objects (Not Arrows)")]
        [FormerlySerializedAs("spacing")]
        [Tooltip("Khoảng cách tâm giữa các vật thể triệu hồi. Không tác dụng với RainArrow hoặc prefab có script Arrow; độ rộng mưa tên chỉnh bằng Target Radius.")]
        [Min(0f)] public float objectSpacing;
        [Header("Flight And Damage")]
        public ProjectilePath path = ProjectilePath.NormalArc;
        public float arc;
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0.01f)] public float speedMultiplier = 1f;
        [Header("Khi cắm xuống đất")]
        [Tooltip("Thời gian mũi tên nằm trên đất. Mặc định 2 giây.")]
        [Min(0.1f)] public float groundDuration = 2f;
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
        [Tooltip("Hồi máu tức thì cho bản thân ngay khi ấn.")]
        [Min(0)] public int heal;
        [Tooltip("Thời lượng hiệu lực của buff (giây).")]
        [Min(0f)] public float duration;
        [Min(0f)] public float moveMultiplier = 1f;
        [Range(0f, 1f)] public float dodgeChance;
        [Min(0.01f)] public float projectileSpeedMultiplier = 1f;
        [Tooltip("Bất tử: Không bị ảnh hưởng bởi sát thương hay hiệu ứng trong thời gian buff.")]
        public bool invulnerable;
        [Tooltip("Xóa sạch toàn bộ hiệu ứng bất lợi đang có trên người.")]
        public bool cleanse;
        [Tooltip("Miễn nhiễm mọi hiệu ứng bất lợi mới trong thời gian buff.")]
        public bool statusImmune;
        [Header("Hồi Máu Nghỉ Ngơi / Theo Chu Kỳ (Rest / HoT)")]
        [Tooltip("Bật nếu đây là chiêu nghỉ ngơi (Rest) hồi máu định kỳ.")]
        public bool isRestSkill;
        [Tooltip("Lượng máu hồi mỗi 1 giây (hoặc mỗi chu kỳ).")]
        [Min(0)] public int healPerSecond;
        [Tooltip("Chu kỳ hồi máu (mặc định 1 giây).")]
        [Min(0.1f)] public float healInterval = 1f;
        [Tooltip("Prefab hiệu ứng VFX khi hồi máu hoặc khi có buff.")]
        public GameObject vfxPrefab;
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
        public virtual int VolleyCount => 1;
        public virtual float VolleyInterval => 0f;
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
