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

        public BowData EquippedBow => equippedBow;
        public SkillData[] EquippedSkills => equippedSkills;
        public LoadoutDatabase Database => database;

        public event Action<BowData> BowChanged;
        public event Action<int, SkillData> SkillChanged;
        public event Action LoadoutChanged;


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
            if (bow == null || equippedBow == bow) return;
            equippedBow = bow;
            PlayerPrefs.SetString(PrefKeyBow, !string.IsNullOrEmpty(bow.BowId) ? bow.BowId : bow.name);
            PlayerPrefs.Save();

            BowChanged?.Invoke(equippedBow);
            LoadoutChanged?.Invoke();
        }

        public void SetSkill(int slot, SkillData skill)
        {
            if (slot < 0 || slot >= 5) return;
            if (equippedSkills == null || equippedSkills.Length != 5) equippedSkills = new SkillData[5];

            equippedSkills[slot] = skill;
            string key = PrefKeySkillPrefix + slot;
            if (skill != null)
            {
                PlayerPrefs.SetString(key, !string.IsNullOrEmpty(skill.SkillId) ? skill.SkillId : skill.name);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();

            SkillChanged?.Invoke(slot, skill);
            LoadoutChanged?.Invoke();
        }

        public SkillData GetEquippedSkill(int slot)
        {
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

        private void LoadFromPreferences()
        {
            if (database == null) return;

            // Load Bow
            string savedBowId = PlayerPrefs.GetString(PrefKeyBow, string.Empty);
            BowData bow = database.FindBow(savedBowId);
            equippedBow = bow != null ? bow : database.DefaultBow;

            // Load Skills
            for (int i = 0; i < 5; i++)
            {
                string savedSkillId = PlayerPrefs.GetString(PrefKeySkillPrefix + i, string.Empty);
                SkillData skill = database.FindSkill(savedSkillId);
                if (skill == null && database.DefaultSkills != null && i < database.DefaultSkills.Count)
                {
                    skill = database.DefaultSkills[i];
                }
                equippedSkills[i] = skill;
            }
        }
    }
}
