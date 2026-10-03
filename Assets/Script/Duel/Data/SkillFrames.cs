using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Skill Frames", fileName = "SkillFrames")]
    public sealed class SkillFrames : ScriptableObject
    {
        [SerializeField] private Sprite common;
        [SerializeField] private Sprite rare;
        [SerializeField] private Sprite heroic;
        [SerializeField] private Sprite legendary;

        public Sprite Get(SkillRarity rarity)
        {
            switch (rarity)
            {
                case SkillRarity.Rare: return rare;
                case SkillRarity.Heroic: return heroic;
                case SkillRarity.Legendary: return legendary;
                default: return common;
            }
        }
    }
}
