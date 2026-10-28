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
        [SerializeField] private BowFrames bowFrames;
        [SerializeField] private Sprite roundedBoxSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private TMP_FontAsset fontAsset;

        public IReadOnlyList<BowData> Bows => bows;
        public IReadOnlyList<SkillData> Skills => skills;
        public BowData DefaultBow => defaultBow;
        public IReadOnlyList<SkillData> DefaultSkills => defaultSkills;
        public SkillFrames Frames => frames;
        public BowFrames BowFrames => bowFrames;
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

#if UNITY_EDITOR
        [UnityEditor.MenuItem("AOG/Database/Sync All Skills and Bows")]
        public static void MenuSyncDatabase()
        {
            var db = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            if (db != null)
            {
                db.SyncDatabase();
                UnityEditor.EditorUtility.SetDirty(db);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"[LoadoutDatabase] Đã đồng bộ: {db.Skills.Count} kỹ năng, {db.Bows.Count} cung tên.");
            }
        }

        public void SyncDatabase()
        {
            bool changed = false;

            // 1. Quét file .asset trong thư mục Assets/Art/Skill
            string skillDir = System.IO.Path.Combine(Application.dataPath, "Art/Skill");
            if (System.IO.Directory.Exists(skillDir))
            {
                string[] files = System.IO.Directory.GetFiles(skillDir, "*.asset", System.IO.SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    string relPath = "Assets/Art/Skill/" + fileName;

                    SkillData s = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(relPath);
                    if (s != null && !skills.Contains(s))
                    {
                        skills.Add(s);
                        changed = true;
                    }
                }
            }

            // Fallback bằng AssetDatabase.FindAssets
            string[] extraSkillGuids = UnityEditor.AssetDatabase.FindAssets("t:SkillData t:SummonObject t:MoveSkill");
            if (extraSkillGuids != null)
            {
                foreach (string g in extraSkillGuids)
                {
                    string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    if (!string.IsNullOrEmpty(p))
                    {
                        SkillData s = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(p);
                        if (s != null && !skills.Contains(s))
                        {
                            skills.Add(s);
                            changed = true;
                        }
                    }
                }
            }

            for (int i = skills.Count - 1; i >= 0; i--)
            {
                if (skills[i] == null)
                {
                    skills.RemoveAt(i);
                    changed = true;
                }
            }

            // 2. Quét file .asset trong thư mục Assets/Art/Weapon
            string weaponDir = System.IO.Path.Combine(Application.dataPath, "Art/Weapon");
            if (System.IO.Directory.Exists(weaponDir))
            {
                string[] files = System.IO.Directory.GetFiles(weaponDir, "*.asset", System.IO.SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    string relPath = "Assets/Art/Weapon/" + fileName;

                    BowData b = UnityEditor.AssetDatabase.LoadAssetAtPath<BowData>(relPath);
                    if (b != null && !bows.Contains(b))
                    {
                        bows.Add(b);
                        changed = true;
                    }
                }
            }

            string[] extraBowGuids = UnityEditor.AssetDatabase.FindAssets("t:BowData");
            if (extraBowGuids != null)
            {
                foreach (string g in extraBowGuids)
                {
                    string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    if (!string.IsNullOrEmpty(p))
                    {
                        BowData b = UnityEditor.AssetDatabase.LoadAssetAtPath<BowData>(p);
                        if (b != null && !bows.Contains(b))
                        {
                            bows.Add(b);
                            changed = true;
                        }
                    }
                }
            }

            for (int i = bows.Count - 1; i >= 0; i--)
            {
                if (bows[i] == null)
                {
                    bows.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
