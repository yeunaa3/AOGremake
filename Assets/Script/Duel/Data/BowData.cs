using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Bow Definition", fileName = "BowData")]
    public sealed class BowData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string bowId = "bow_basic";
        [SerializeField] private string displayName = "Basic Bow";
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite bowSprite;

        [Header("Basic attack")]
        [SerializeField] private ArrowData projectile;
        [SerializeField, Min(0)] private int damage = 10;
        [Tooltip("1 = không đổi, 1.5 = nhanh hơn 50%, 0.7 = chậm hơn 30%.")]
        [SerializeField, Min(0.05f)] private float attackSpeedMultiplier = 1f;

        public string BowId => bowId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Sprite BowSprite => bowSprite;
        public ArrowData Projectile => projectile;
        public int Damage => damage;
        public float AttackSpeedMultiplier => attackSpeedMultiplier;
    }
}
