using System;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    [System.Serializable]
    public class CharacterSkinData
    {
        public string skinId = "skin1";
        public string displayName = "Skin 1";

        [Header("Thân & Đầu")]
        public Sprite head;
        public Sprite body;

        [Header("Tay Trái (Trước Ngực/Cầm Tên)")]
        public Sprite shoulderL;
        public Sprite armL;
        public Sprite handL;

        [Header("Tay Phải (Sau Lưng/Cầm Cung)")]
        public Sprite shoulderR;
        public Sprite armR;
        public Sprite handR;

        [Header("Chân")]
        public Sprite thighL;
        public Sprite thighR;
        public Sprite legL;
        public Sprite legR;
    }

    [CreateAssetMenu(menuName = "AOG/Duel/Character Skin Database", fileName = "CharacterSkinDatabase")]
    public sealed class CharacterSkinDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterSkinData> skins = new List<CharacterSkinData>();

        public IReadOnlyList<CharacterSkinData> Skins => skins;

        public CharacterSkinData GetSkin(string skinId)
        {
            if (skins == null || skins.Count == 0) return null;
            if (string.IsNullOrEmpty(skinId)) return skins[0];

            for (int i = 0; i < skins.Count; i++)
            {
                if (string.Equals(skins[i].skinId, skinId, StringComparison.OrdinalIgnoreCase))
                {
                    return skins[i];
                }
            }
            return skins[0];
        }

        public CharacterSkinData GetSkinByIndex(int index)
        {
            if (skins == null || skins.Count == 0) return null;
            int safeIndex = ((index % skins.Count) + skins.Count) % skins.Count;
            return skins[safeIndex];
        }

        public int GetSkinIndex(string skinId)
        {
            if (skins == null || skins.Count == 0 || string.IsNullOrEmpty(skinId)) return 0;
            for (int i = 0; i < skins.Count; i++)
            {
                if (string.Equals(skins[i].skinId, skinId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return 0;
        }
#if UNITY_EDITOR
        [UnityEditor.MenuItem("AOG/Database/Sync All Character Skins")]
        public static void MenuSyncDatabase()
        {
            var db = Resources.Load<CharacterSkinDatabase>("CharacterSkinDatabase");
            if (db != null)
            {
                db.SyncDatabase();
                UnityEditor.EditorUtility.SetDirty(db);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"[CharacterSkinDatabase] Đã đồng bộ: {db.skins.Count} skins.");
            }
        }

        public void SyncDatabase()
        {
            string characterDirectory = System.IO.Path.Combine(Application.dataPath, "Art/Character");
            if (!System.IO.Directory.Exists(characterDirectory)) return;

            string[] subDirectories = System.IO.Directory.GetDirectories(characterDirectory);
            System.Array.Sort(subDirectories);

            foreach (string directoryPath in subDirectories)
            {
                string folderName = System.IO.Path.GetFileName(directoryPath);
                if (folderName.Equals("Animation", StringComparison.OrdinalIgnoreCase)) continue;

                CharacterSkinData skin = skins.Find(s => string.Equals(s.skinId, folderName, StringComparison.OrdinalIgnoreCase));
                if (skin == null)
                {
                    skin = new CharacterSkinData
                    {
                        skinId = folderName,
                        displayName = FormatDisplayName(folderName)
                    };
                    skins.Add(skin);
                }

                string[] pngFiles = System.IO.Directory.GetFiles(directoryPath, "*.png");
                List<string> unmappedFileNames = new List<string>();

                foreach (string filePath in pngFiles)
                {
                    string fileName = System.IO.Path.GetFileName(filePath);
                    if (fileName.EndsWith(" 1.png", StringComparison.OrdinalIgnoreCase)) continue;

                    string assetPath = "Assets/Art/Character/" + folderName + "/" + fileName;
                    Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;

                    string cleanName = fileName.ToLowerInvariant().Replace("_", "");
                    if (cleanName.Contains("head")) skin.head = sprite;
                    else if (cleanName.Contains("body")) skin.body = sprite;
                    else if (cleanName.Contains("shoulderl")) skin.shoulderL = sprite;
                    else if (cleanName.Contains("shoulderr")) skin.shoulderR = sprite;
                    else if (cleanName.Contains("arml")) skin.armL = sprite;
                    else if (cleanName.Contains("armr")) skin.armR = sprite;
                    else if (cleanName.Contains("handl")) skin.handL = sprite;
                    else if (cleanName.Contains("handr")) skin.handR = sprite;
                    else if (cleanName.Contains("thighl") || cleanName.Contains("upperlegl")) skin.thighL = sprite;
                    else if (cleanName.Contains("thighr") || cleanName.Contains("upperlegr")) skin.thighR = sprite;
                    else if (cleanName.Contains("legl")) skin.legL = sprite;
                    else if (cleanName.Contains("legr")) skin.legR = sprite;
                    else unmappedFileNames.Add(fileName);
                }

                foreach (string fileName in unmappedFileNames)
                {
                    string assetPath = "Assets/Art/Character/" + folderName + "/" + fileName;
                    Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite == null) continue;

                    string lowerName = fileName.ToLowerInvariant();
                    if (skin.handR == null && lowerName.Contains("hand")) skin.handR = sprite;
                    else if (skin.armR == null && lowerName.Contains("arm")) skin.armR = sprite;
                }
            }
        }

        private static string FormatDisplayName(string id)
        {
            if (id.StartsWith("skin", StringComparison.OrdinalIgnoreCase))
            {
                string rest = id.Substring(4).TrimStart('_');
                if (int.TryParse(rest, out int num)) return $"Skin {num}";
                if (!string.IsNullOrEmpty(rest)) return $"Skin {char.ToUpper(rest[0]) + rest.Substring(1)}";
            }
            return id;
        }
#endif
    }
}
