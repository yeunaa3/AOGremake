using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Bow Frames", fileName = "BowFrames")]
    public sealed class BowFrames : ScriptableObject
    {
        [Header("Khung Viền Theo Độ Hiếm (Kéo ảnh khung vào đây)")]
        [SerializeField] private Sprite common;
        [SerializeField] private Sprite rare;
        [SerializeField] private Sprite heroic;
        [SerializeField] private Sprite legendary;

        public Sprite Common => common;
        public Sprite Rare => rare;
        public Sprite Heroic => heroic;
        public Sprite Legendary => legendary;

        public Sprite Get(BowRarity rarity, SkillFrames fallbackFrames = null)
        {
            Sprite result = null;
            switch (rarity)
            {
                case BowRarity.Rare: result = rare; break;
                case BowRarity.Heroic: result = heroic; break;
                case BowRarity.Legendary: result = legendary; break;
                default: result = common; break;
            }

            if (result == null && fallbackFrames != null)
            {
                result = fallbackFrames.Get((SkillRarity)rarity);
            }
            return result;
        }

        public Sprite Get(SkillRarity rarity, SkillFrames fallbackFrames = null)
        {
            return Get((BowRarity)rarity, fallbackFrames);
        }
    }
}
