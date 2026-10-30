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
    }
}
