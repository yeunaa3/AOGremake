using System;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    // Quản lý bộ nhớ thẻ chiêu thức và cấp độ kỹ năng của người chơi (mở rương, nâng cấp thẻ).
    [DefaultExecutionOrder(-95)]
    public sealed class PlayerSkillInventory : MonoBehaviour
    {
        private const string PrefKeySkillLevelPrefix = "AOG_SkillLevel_";
        private const string PrefKeySkillCardsPrefix = "AOG_SkillCards_";
        private const string PrefKeyInitialized = "AOG_SkillInventory_Initialized";

        private static PlayerSkillInventory instance;
        public static PlayerSkillInventory Instance
        {
            get
            {
                if (instance == null)
                {
                    EnsureInstance();
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        private readonly Dictionary<string, int> skillLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> skillCards = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public event Action<string> InventoryChanged;

        private static void EnsureInstance()
        {
            PlayerSkillInventory found = FindFirstObjectByType<PlayerSkillInventory>();
            if (found != null)
            {
                instance = found;
                if (Application.isPlaying) DontDestroyOnLoad(found.gameObject);
                instance.InitializeIfNeeded();
                return;
            }

            GameObject go = new GameObject("PlayerSkillInventory");
            instance = go.AddComponent<PlayerSkillInventory>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
            instance.InitializeIfNeeded();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }

            instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            InitializeIfNeeded();
        }

        public const int MaxLevel = 10;

        // Bảng số thẻ yêu cầu theo Fibonacci (Cấp 1 cần 2 để lên cấp 2 -> mặc định ban đầu là 0/2; Cấp 10 là tối đa)
        private static readonly int[] FibonacciRequiredCards = new int[]
        {
            2,  // Lên Cấp 2: cần 2 thẻ (ban đầu 0/2)
            3,  // Lên Cấp 3: cần 3 thẻ
            5,  // Lên Cấp 4: cần 5 thẻ
            8,  // Lên Cấp 5: cần 8 thẻ
            13, // Lên Cấp 6: cần 13 thẻ
            21, // Lên Cấp 7: cần 21 thẻ
            34, // Lên Cấp 8: cần 34 thẻ
            55, // Lên Cấp 9: cần 55 thẻ
            89  // Lên Cấp 10: cần 89 thẻ (Cấp 10 là tối đa)
        };

        private const string PrefKeyFibonacciV1 = "AOG_SkillFibonacci_v2";

        private bool initialized;
        public void InitializeIfNeeded()
        {
            if (initialized) return;
            initialized = true;

            // Xóa dữ liệu mẫu cũ (ví dụ 2/5 trước đây) để áp dụng mặc định mới 0/2
            if (!PlayerPrefs.HasKey(PrefKeyFibonacciV1))
            {
                PlayerPrefs.SetInt(PrefKeyFibonacciV1, 1);
                skillCards.Clear();
                skillLevels.Clear();
                // Xóa các key demo cũ nếu có
                var db = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
                if (db != null && db.Skills != null)
                {
                    foreach (var s in db.Skills)
                    {
                        if (s == null) continue;
                        string sid = !string.IsNullOrEmpty(s.SkillId) ? s.SkillId : s.name;
                        PlayerPrefs.DeleteKey(PrefKeySkillCardsPrefix + sid);
                        PlayerPrefs.DeleteKey(PrefKeySkillLevelPrefix + sid);
                    }
                }
                PlayerPrefs.Save();
            }
        }

        public static int GetRequiredCardsForLevel(int level)
        {
            if (level >= MaxLevel) return 0; // Đạt cấp tối đa
            int index = Mathf.Clamp(level - 1, 0, FibonacciRequiredCards.Length - 1);
            return FibonacciRequiredCards[index];
        }

        public int GetLevel(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return 1;

            if (skillLevels.TryGetValue(skillId, out int level))
            {
                return Mathf.Clamp(level, 1, MaxLevel);
            }

            int savedLevel = PlayerPrefs.GetInt(PrefKeySkillLevelPrefix + skillId, 1);
            savedLevel = Mathf.Clamp(savedLevel, 1, MaxLevel);
            skillLevels[skillId] = savedLevel;
            return savedLevel;
        }

        public int GetCards(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return 0;

            if (skillCards.TryGetValue(skillId, out int cards))
            {
                return Mathf.Max(0, cards);
            }

            // Mặc định ban đầu là 0 thẻ (kết hợp cấp 1 cần 2 -> hiển thị 0/2)
            int savedCards = PlayerPrefs.GetInt(PrefKeySkillCardsPrefix + skillId, 0);
            skillCards[skillId] = savedCards;
            return savedCards;
        }

        public int GetRequiredCards(string skillId)
        {
            return GetRequiredCardsForLevel(GetLevel(skillId));
        }

        public void AddCards(string skillId, int count)
        {
            if (string.IsNullOrEmpty(skillId) || count <= 0) return;

            int current = GetCards(skillId);
            int updated = current + count;
            skillCards[skillId] = updated;

            PlayerPrefs.SetInt(PrefKeySkillCardsPrefix + skillId, updated);
            PlayerPrefs.Save();

            InventoryChanged?.Invoke(skillId);
        }

        public bool CanUpgrade(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return false;
            return GetCards(skillId) >= GetRequiredCards(skillId);
        }

        public bool TryUpgrade(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return false;

            int level = GetLevel(skillId);
            int cards = GetCards(skillId);
            int required = GetRequiredCardsForLevel(level);

            if (cards < required) return false;

            int updatedCards = cards - required;
            int updatedLevel = level + 1;

            skillCards[skillId] = updatedCards;
            skillLevels[skillId] = updatedLevel;

            PlayerPrefs.SetInt(PrefKeySkillCardsPrefix + skillId, updatedCards);
            PlayerPrefs.SetInt(PrefKeySkillLevelPrefix + skillId, updatedLevel);
            PlayerPrefs.Save();

            InventoryChanged?.Invoke(skillId);
            return true;
        }

        public void SetCards(string skillId, int amount)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            skillCards[skillId] = Mathf.Max(0, amount);
            PlayerPrefs.SetInt(PrefKeySkillCardsPrefix + skillId, skillCards[skillId]);
            PlayerPrefs.Save();
            InventoryChanged?.Invoke(skillId);
        }

        public void SetLevel(string skillId, int level)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            skillLevels[skillId] = Mathf.Max(1, level);
            PlayerPrefs.SetInt(PrefKeySkillLevelPrefix + skillId, skillLevels[skillId]);
            PlayerPrefs.Save();
            InventoryChanged?.Invoke(skillId);
        }
    }
}
