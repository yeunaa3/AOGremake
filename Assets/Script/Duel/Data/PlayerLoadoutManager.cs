using System;
using UnityEngine;

namespace AOG.Duel
{
    // Quản lý cung và 5 kỹ năng người chơi đã chọn, tồn tại xuyên suốt qua các màn chơi.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerLoadoutManager : MonoBehaviour
    {
        private const string PrefKeyBow = "AOG_EquippedBow";
        private const string PrefKeySkillPrefix = "AOG_EquippedSkill_";
        private const string PrefKeyActiveSet = "AOG_ActiveSkillSet";
        private const string PrefKeySkillSetPrefix = "AOG_SkillSet_";

        public const int MaxSkillSets = 4;
        private int activeSkillSet = 0;
        public int ActiveSkillSet { get { InitializeIfNeeded(); return activeSkillSet; } }

        private static PlayerLoadoutManager instance;
        public static PlayerLoadoutManager Instance
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

        [Header("Cơ Sở Dữ Liệu Loadout")]
        [SerializeField] private LoadoutDatabase database;

        [Header("Trang Bị Hiện Tại")]
        [SerializeField] private BowData equippedBow;
        [SerializeField] private SkillData[] equippedSkills = new SkillData[5];

        public BowData EquippedBow { get { InitializeIfNeeded(); return equippedBow; } }
        public SkillData[] EquippedSkills { get { InitializeIfNeeded(); return equippedSkills; } }
        public LoadoutDatabase Database { get { InitializeIfNeeded(); return database; } }

        public event Action<BowData> BowChanged;
        public event Action<int, SkillData> SkillChanged;
        public event Action LoadoutChanged;
        public event Action<int> ActiveSkillSetChanged;


        private static void EnsureInstance()
        {
            PlayerLoadoutManager found = FindFirstObjectByType<PlayerLoadoutManager>();
            if (found != null)
            {
                instance = found;
                DontDestroyOnLoad(found.gameObject);
                instance.InitializeIfNeeded();
                return;
            }

            GameObject go = new GameObject("PlayerLoadoutManager");
            instance = go.AddComponent<PlayerLoadoutManager>();
            DontDestroyOnLoad(go);
            instance.InitializeIfNeeded();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeIfNeeded();
        }

        private bool initialized;
        public void InitializeIfNeeded()
        {
            if (initialized) return;
            initialized = true;

            if (database == null)
            {
                database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            }

            if (equippedSkills == null || equippedSkills.Length != 5)
            {
                equippedSkills = new SkillData[5];
            }

            LoadFromPreferences();
        }

        public void EquipBow(BowData bow)
        {
            if (bow == null) return;
            equippedBow = bow;
            PlayerPrefs.SetString(PrefKeyBow, !string.IsNullOrEmpty(bow.BowId) ? bow.BowId : bow.name);
            PlayerPrefs.Save();

            BowChanged?.Invoke(equippedBow);
            LoadoutChanged?.Invoke();
        }

        public void SetActiveSkillSet(int setIndex)
        {
            if (setIndex < 0 || setIndex >= MaxSkillSets) return;
            if (setIndex == activeSkillSet) return;

            // 1. Lưu set hiện tại
            SaveCurrentSet();

            // 2. Chuyển sang set mới
            activeSkillSet = setIndex;
            PlayerPrefs.SetInt(PrefKeyActiveSet, activeSkillSet);
            PlayerPrefs.Save();

            // 3. Tải các kỹ năng của set mới
            LoadSkillsForActiveSet();

            ActiveSkillSetChanged?.Invoke(activeSkillSet);
            LoadoutChanged?.Invoke();
        }

        public void SetSkill(int slot, SkillData skill)
        {
            if (slot < 0 || slot >= 5) return;
            if (equippedSkills == null || equippedSkills.Length != 5) equippedSkills = new SkillData[5];

            equippedSkills[slot] = skill;

            // Lưu vào set đang chọn
            string setKey = $"{PrefKeySkillSetPrefix}{activeSkillSet}_Slot_{slot}";
            string legacyKey = PrefKeySkillPrefix + slot;

            if (skill != null)
            {
                string id = !string.IsNullOrEmpty(skill.SkillId) ? skill.SkillId : skill.name;
                PlayerPrefs.SetString(setKey, id);
                PlayerPrefs.SetString(legacyKey, id);
            }
            else
            {
                PlayerPrefs.DeleteKey(setKey);
                PlayerPrefs.DeleteKey(legacyKey);
            }
            PlayerPrefs.Save();

            SkillChanged?.Invoke(slot, skill);
            LoadoutChanged?.Invoke();
        }

        // Trang bị kỹ năng vào slot chỉ định. Nếu kỹ năng đã có ở slot khác thì tự động hoán đổi để tránh trùng lặp.
        public void EquipSkillToSlot(int targetSlot, SkillData skill)
        {
            if (targetSlot < 0 || targetSlot >= 5 || skill == null) return;
            if (equippedSkills == null || equippedSkills.Length != 5) equippedSkills = new SkillData[5];

            int existingSlot = -1;
            for (int i = 0; i < 5; i++)
            {
                if (i != targetSlot && equippedSkills[i] == skill)
                {
                    existingSlot = i;
                    break;
                }
            }

            if (existingSlot >= 0)
            {
                SkillData prevTargetSkill = equippedSkills[targetSlot];
                SetSkill(existingSlot, prevTargetSkill);
            }

            SetSkill(targetSlot, skill);
        }

        public SkillData GetEquippedSkill(int slot)
        {
            InitializeIfNeeded();
            if (equippedSkills == null || slot < 0 || slot >= equippedSkills.Length) return null;
            return equippedSkills[slot];
        }

        public void ApplyToPlayer(PlayerController player)
        {
            if (player == null) return;

            InitializeIfNeeded();

            if (equippedBow != null && player.Gear != null)
            {
                player.Gear.EquipBow(equippedBow);
            }

            if (player.Action != null && equippedSkills != null)
            {
                player.Action.SetSkills(equippedSkills);
            }
        }

        private void SaveCurrentSet()
        {
            if (equippedSkills == null) return;
            for (int i = 0; i < 5; i++)
            {
                SkillData skill = i < equippedSkills.Length ? equippedSkills[i] : null;
                string setKey = $"{PrefKeySkillSetPrefix}{activeSkillSet}_Slot_{i}";
                string legacyKey = PrefKeySkillPrefix + i;

                if (skill != null)
                {
                    string id = !string.IsNullOrEmpty(skill.SkillId) ? skill.SkillId : skill.name;
                    PlayerPrefs.SetString(setKey, id);
                    PlayerPrefs.SetString(legacyKey, id);
                }
                else
                {
                    PlayerPrefs.DeleteKey(setKey);
                    PlayerPrefs.DeleteKey(legacyKey);
                }
            }
            PlayerPrefs.Save();
        }

        private void LoadSkillsForActiveSet()
        {
            if (database == null) return;
            if (equippedSkills == null || equippedSkills.Length != 5) equippedSkills = new SkillData[5];

            for (int i = 0; i < 5; i++)
            {
                string setKey = $"{PrefKeySkillSetPrefix}{activeSkillSet}_Slot_{i}";
                string savedSkillId = string.Empty;

                if (PlayerPrefs.HasKey(setKey))
                {
                    savedSkillId = PlayerPrefs.GetString(setKey, string.Empty);
                }
                else if (activeSkillSet == 0 && PlayerPrefs.HasKey(PrefKeySkillPrefix + i))
                {
                    // Fallback tương thích với Set 0 trước đây
                    savedSkillId = PlayerPrefs.GetString(PrefKeySkillPrefix + i, string.Empty);
                }

                SkillData skill = database.FindSkill(savedSkillId);
                if (skill == null && database.DefaultSkills != null && i < database.DefaultSkills.Count)
                {
                    skill = database.DefaultSkills[i];
                }
                equippedSkills[i] = skill;

                // Đồng bộ sang legacy key
                if (skill != null)
                {
                    PlayerPrefs.SetString(PrefKeySkillPrefix + i, !string.IsNullOrEmpty(skill.SkillId) ? skill.SkillId : skill.name);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PrefKeySkillPrefix + i);
                }
            }
            PlayerPrefs.Save();
        }

        private void LoadFromPreferences()
        {
            if (database == null) return;

            // Load Bow
            string savedBowId = PlayerPrefs.GetString(PrefKeyBow, string.Empty);
            BowData bow = database.FindBow(savedBowId);
            equippedBow = bow != null ? bow : database.DefaultBow;

            // Load Active Set Index
            activeSkillSet = Mathf.Clamp(PlayerPrefs.GetInt(PrefKeyActiveSet, 0), 0, MaxSkillSets - 1);

            // Load Skills for Active Set
            LoadSkillsForActiveSet();
        }
    }
}
