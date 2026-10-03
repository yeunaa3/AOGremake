using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Bow Definition", fileName = "BowData")]
    public sealed class BowData : ScriptableObject
    {
        [Header("Bow and arrow set")]
        [SerializeField] private string bowId = "bow_basic";
        [SerializeField] private string displayName = "Basic Bow";
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite bowSprite;

        [Tooltip("Mọi mũi tên thường và mũi tên từ skill đều lấy sprite này.")]
        [SerializeField] private Sprite arrowSprite;

        [Header("Stats")]
        [SerializeField, Min(0)] private int damage = 10;
        [Tooltip("1 = không đổi, 1.5 = nhanh hơn 50%, 0.7 = chậm hơn 30%.")]
        [SerializeField, Min(0.05f)] private float attackSpeedMultiplier = 1f;

        public string BowId => bowId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Sprite BowSprite => bowSprite;
        public Sprite ArrowSprite => arrowSprite;
        public int Damage => damage;
        public float AttackSpeedMultiplier => attackSpeedMultiplier;
    }
}
