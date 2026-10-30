using System;
using UnityEngine;

namespace AOG.Duel
{
    // Quản lý Skin của nhân vật, lưu trữ lựa chọn của người chơi vào PlayerPrefs và áp dụng lên nhân vật
    public sealed class CharacterSkinManager : MonoBehaviour
    {
        private const string PrefKeySkinIndex = "AOG_SelectedSkinIndex";

        private static CharacterSkinManager instance;
        public static CharacterSkinManager Instance
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

        [SerializeField] private CharacterSkinDatabase database;
        private int selectedSkinIndex = 0;

        public CharacterSkinDatabase Database
        {
            get
            {
                InitializeIfNeeded();
                return database;
            }
        }

        public int CurrentSkinIndex
        {
            get
            {
                InitializeIfNeeded();
                return selectedSkinIndex;
            }
        }

        public CharacterSkinData CurrentSkin
        {
            get
            {
                InitializeIfNeeded();
                if (database == null || database.Skins == null || database.Skins.Count == 0) return null;
                return database.GetSkinByIndex(selectedSkinIndex);
            }
        }

        public event Action<CharacterSkinData> SkinChanged;

        private static void EnsureInstance()
        {
            CharacterSkinManager found = FindFirstObjectByType<CharacterSkinManager>();
            if (found != null)
            {
                instance = found;
                if (Application.isPlaying) DontDestroyOnLoad(found.gameObject);
                instance.InitializeIfNeeded();
                return;
            }

            GameObject go = new GameObject("CharacterSkinManager");
            instance = go.AddComponent<CharacterSkinManager>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
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
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            InitializeIfNeeded();
        }

        private bool initialized;
        public void InitializeIfNeeded()
        {
            if (initialized) return;
            initialized = true;

            if (database == null)
            {
                database = Resources.Load<CharacterSkinDatabase>("CharacterSkinDatabase");
            }

            int count = database != null && database.Skins != null ? database.Skins.Count : 1;
            selectedSkinIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefKeySkinIndex, 0), 0, Mathf.Max(0, count - 1));
        }

        public CharacterSkinData NextSkin()
        {
            InitializeIfNeeded();
            if (database == null || database.Skins == null || database.Skins.Count == 0) return null;

            selectedSkinIndex = (selectedSkinIndex + 1) % database.Skins.Count;
            PlayerPrefs.SetInt(PrefKeySkinIndex, selectedSkinIndex);
            PlayerPrefs.Save();

            CharacterSkinData skin = CurrentSkin;
            SkinChanged?.Invoke(skin);
            return skin;
        }

        public void SetSkin(int index)
        {
            InitializeIfNeeded();
            if (database == null || database.Skins == null || database.Skins.Count == 0) return;

            selectedSkinIndex = Mathf.Clamp(index, 0, database.Skins.Count - 1);
            PlayerPrefs.SetInt(PrefKeySkinIndex, selectedSkinIndex);
            PlayerPrefs.Save();

            CharacterSkinData skin = CurrentSkin;
            SkinChanged?.Invoke(skin);
        }

        public void ApplyCurrentSkin(GameObject characterRoot)
        {
            if (characterRoot == null) return;
            CharacterSkinData skin = CurrentSkin;
            if (skin != null)
            {
                ApplySkin(characterRoot, skin);
            }
        }

        public static void ApplySkin(GameObject characterRoot, CharacterSkinData skin)
        {
            if (characterRoot == null || skin == null) return;

            Transform visual = characterRoot.transform.Find("Visual");
            if (visual == null) visual = characterRoot.transform;

            ApplyPartSprite(visual, "head", skin.head);
            ApplyPartSprite(visual, "body", skin.body);
            ApplyPartSprite(visual, "arm_L", skin.armL);
            ApplyPartSprite(visual, "arm_R", skin.armR);
            ApplyPartSprite(visual, "hand_L", skin.handL);
            ApplyPartSprite(visual, "hand_R", skin.handR);
            ApplyPartSprite(visual, "shoulder_L", skin.shoulderL);
            ApplyPartSprite(visual, "shoulder_R", skin.shoulderR);

            // Chú ý: trong Character.prefab, phần chân của rig:
            // upperleg_R gắn sprite thighL, leg_R gắn sprite legL
            // upperleg_L gắn sprite thighR, leg_L gắn sprite legR
            ApplyPartSprite(visual, "upperleg_R", skin.thighL);
            ApplyPartSprite(visual, "leg_R", skin.legL);
            ApplyPartSprite(visual, "upperleg_L", skin.thighR);
            ApplyPartSprite(visual, "leg_L", skin.legR);
        }

        private static void ApplyPartSprite(Transform root, string partName, Sprite sprite)
        {
            if (sprite == null) return;
            Transform part = FindDeepChild(root, partName);
            if (part != null)
            {
                var sr = part.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = sprite;
                }
            }
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
