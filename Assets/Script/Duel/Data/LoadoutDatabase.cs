using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AOG.Duel
{
    [CreateAssetMenu(menuName = "AOG/Duel/Loadout Database", fileName = "LoadoutDatabase")]
    public sealed class LoadoutDatabase : ScriptableObject
    {
        [Header("Tất Cả Bows")]
        [SerializeField] private List<BowData> bows = new List<BowData>();

        [Header("Tất Cả Skills")]
        [SerializeField] private List<SkillData> skills = new List<SkillData>();

        [Header("Mặc Định")]
        [SerializeField] private BowData defaultBow;
        [SerializeField] private SkillData[] defaultSkills = new SkillData[5];

        [Header("Khung Rarity & UI Assets")]
        [SerializeField] private SkillFrames frames;
        [SerializeField] private Sprite roundedBoxSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private TMP_FontAsset fontAsset;

        public IReadOnlyList<BowData> Bows => bows;
        public IReadOnlyList<SkillData> Skills => skills;
        public BowData DefaultBow => defaultBow;
        public IReadOnlyList<SkillData> DefaultSkills => defaultSkills;
        public SkillFrames Frames => frames;
        public Sprite RoundedBoxSprite => roundedBoxSprite;
        public Sprite CircleSprite => circleSprite;
        public Sprite ButtonSprite => buttonSprite;
        public TMP_FontAsset FontAsset => fontAsset;

        public BowData FindBow(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return null;
            foreach (BowData bow in bows)
            {
                if (bow == null) continue;
                if (string.Equals(bow.BowId, idOrName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(bow.name, idOrName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(bow.DisplayName, idOrName, StringComparison.OrdinalIgnoreCase))
                {
                    return bow;
                }
            }
            return null;
        }

        public SkillData FindSkill(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return null;
            foreach (SkillData skill in skills)
            {
                if (skill == null) continue;
                if (string.Equals(skill.SkillId, idOrName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(skill.name, idOrName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(skill.DisplayName, idOrName, StringComparison.OrdinalIgnoreCase))
                {
                    return skill;
                }
            }
            return null;
        }
    }
}
