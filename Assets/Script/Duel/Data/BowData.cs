using UnityEngine;

namespace AOG.Duel
{
    public enum BowRarity
    {
        Common = 0,
        Rare = 1,
        Heroic = 2,
        Legendary = 3
    }

    [CreateAssetMenu(menuName = "AOG/Duel/Bow Definition", fileName = "BowData")]
    public sealed class BowData : ScriptableObject
    {
        [Header("Bow and arrow set")]
        [SerializeField] private string bowId = "bow_basic";
        [SerializeField] private string displayName = "Basic Bow";
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite bowSprite;

        [Header("Độ Hiếm (Rarity)")]
        [SerializeField] private BowRarity rarity = BowRarity.Common;

        [Tooltip("Mọi mũi tên thường và mũi tên từ skill đều lấy sprite này.")]
        [SerializeField] private Sprite arrowSprite;

        [Header("Chỉ Số Gốc (Level 1 Mặc Định)")]
        [Tooltip("Sát thương ở Cấp 1 (thông số bạn đặt ban đầu).")]
        [SerializeField, Min(0)] private int damage = 10;
        [Tooltip("Tốc độ bắn / kéo cung ở Cấp 1 (1 = không đổi, 1.5 = nhanh hơn 50%, 0.7 = chậm hơn 30%).")]
        [SerializeField, Min(0.05f)] private float attackSpeedMultiplier = 1f;
        [Tooltip("Tốc độ bay của mũi tên ở Cấp 1 (mặc định: 9).")]
        [SerializeField, Min(0.1f)] private float arrowSpeed = 9f;

        [Header("Công Thức Tăng Trưởng Theo Level")]
        [Tooltip("Tỉ lệ tăng sát thương mỗi level (mặc định 0.1 = tăng 10% sát thương gốc mỗi level).")]
        [SerializeField, Range(0f, 1f)] private float damageGrowthPerLevel = 0.10f;
        [Tooltip("Tỉ lệ tăng tốc độ đánh mỗi level (mặc định 0 = giữ nguyên tốc độ gốc).")]
        [SerializeField, Range(0f, 0.5f)] private float attackSpeedGrowthPerLevel = 0f;
        [Tooltip("Tỉ lệ tăng tốc độ bay mũi tên mỗi level (mặc định 0 = giữ nguyên tốc độ gốc).")]
        [SerializeField, Range(0f, 0.5f)] private float arrowSpeedGrowthPerLevel = 0f;

        [Header("Passive")]
        [SerializeField] private BowPassive passive;

        public string BowId => bowId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Sprite BowSprite => bowSprite;
        public BowRarity Rarity => rarity;
        public SkillRarity SkillRarity => (SkillRarity)rarity;
        public Sprite ArrowSprite => arrowSprite;
        public BowPassive Passive => passive;

        // Chỉ số Level 1 nguyên bản (được giữ nguyên theo thiết lập ban đầu)
        public int BaseDamage => damage;
        public float BaseAttackSpeedMultiplier => attackSpeedMultiplier;
        public float BaseArrowSpeed => arrowSpeed;
        public float DamageGrowthPerLevel => damageGrowthPerLevel;
        public float AttackSpeedGrowthPerLevel => attackSpeedGrowthPerLevel;
        public float ArrowSpeedGrowthPerLevel => arrowSpeedGrowthPerLevel;

        /// <summary>
        /// Tính toán sát thương cho một cấp độ bất kỳ theo công thức:
        /// Level 1: giữ nguyên damage gốc.
        /// Level > 1: baseDamage * (1 + growthRate * (level - 1)).
        /// </summary>
        public int GetDamage(int level)
        {
            if (level <= 1 || damageGrowthPerLevel <= 0f) return damage;
            return Mathf.RoundToInt(damage * (1f + damageGrowthPerLevel * (level - 1)));
        }

        public float GetAttackSpeedMultiplier(int level)
        {
            if (level <= 1 || attackSpeedGrowthPerLevel <= 0f) return attackSpeedMultiplier;
            return attackSpeedMultiplier * (1f + attackSpeedGrowthPerLevel * (level - 1));
        }

        public float GetArrowSpeed(int level)
        {
            if (level <= 1 || arrowSpeedGrowthPerLevel <= 0f) return arrowSpeed;
            return arrowSpeed * (1f + arrowSpeedGrowthPerLevel * (level - 1));
        }

        /// <summary>
        /// Cấp độ hiện tại của cây cung lấy từ bộ nhớ thẻ PlayerSkillInventory.
        /// </summary>
        public int CurrentLevel
        {
            get
            {
                if (PlayerSkillInventory.HasInstance)
                {
                    return PlayerSkillInventory.Instance.GetLevel(bowId);
                }
                return 1;
            }
        }

        // Tự động trả về chỉ số tương ứng theo Level hiện tại của người chơi
        public int Damage => GetDamage(CurrentLevel);
        public float AttackSpeedMultiplier => GetAttackSpeedMultiplier(CurrentLevel);
        public float ArrowSpeed => GetArrowSpeed(CurrentLevel);
    }
}
