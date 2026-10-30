using System;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Kết quả nhận thưởng thẻ sau khi mở rương hoặc nhận quà.
    /// </summary>
    [Serializable]
    public struct CardRewardResult
    {
        public string CardId;
        public int AddedAmount;
        public int TotalCards;
        public int Level;
        public int RequiredCards;
        public bool CanUpgrade;

        public CardRewardResult(string cardId, int added, int total, int level, int required)
        {
            CardId = cardId;
            AddedAmount = added;
            TotalCards = total;
            Level = level;
            RequiredCards = required;
            CanUpgrade = total >= required && required > 0;
        }
    }

    /// <summary>
    /// Quản lý bộ nhớ thẻ (Card Memory) cho cả Kỹ Năng (Skill) và Cung (Bow):
    /// - Lưu trữ số lượng thẻ tích lũy (cards / shards) sau khi mở rương.
    /// - Quản lý Cấp độ (Level 1 đến 10).
    /// - Kiểm tra và thực hiện Nâng cấp thẻ (Upgrade).
    /// - Tự động lưu trữ vĩnh viễn vào PlayerPrefs.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public sealed class PlayerSkillInventory : MonoBehaviour
    {
        private const string PrefKeyCardLevelPrefix = "AOG_SkillLevel_";
        private const string PrefKeyCardCardsPrefix = "AOG_SkillCards_";

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

        private readonly Dictionary<string, int> cardLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> cardCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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

            GameObject go = new GameObject("PlayerCardInventory");
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

        // Bảng số thẻ yêu cầu nâng cấp theo Fibonacci: Cấp 1 cần 2 để lên cấp 2 (0/2 ban đầu). Cấp 10 là tối đa.
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
            89  // Lên Cấp 10: cần 89 thẻ
        };

        private bool initialized;
        public void InitializeIfNeeded()
        {
            if (initialized) return;
            initialized = true;
        }

        /// <summary>
        /// Số thẻ yêu cầu để nâng cấp từ cấp hiện tại lên cấp tiếp theo.
        /// </summary>
        public static int GetRequiredCardsForLevel(int level)
        {
            if (level >= MaxLevel) return 0; // Đạt cấp tối đa
            int index = Mathf.Clamp(level - 1, 0, FibonacciRequiredCards.Length - 1);
            return FibonacciRequiredCards[index];
        }

        /// <summary>
        /// Lấy cấp độ của thẻ (Skill hoặc Bow). Mặc định là Level 1.
        /// </summary>
        public int GetLevel(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return 1;

            if (cardLevels.TryGetValue(cardId, out int level))
            {
                return Mathf.Clamp(level, 1, MaxLevel);
            }

            int savedLevel = PlayerPrefs.GetInt(PrefKeyCardLevelPrefix + cardId, 1);
            savedLevel = Mathf.Clamp(savedLevel, 1, MaxLevel);
            cardLevels[cardId] = savedLevel;
            return savedLevel;
        }

        /// <summary>
        /// Lấy số lượng thẻ đang sở hữu (Skill hoặc Bow). Mặc định là 0.
        /// </summary>
        public int GetCards(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return 0;

            if (cardCounts.TryGetValue(cardId, out int cards))
            {
                return Mathf.Max(0, cards);
            }

            int savedCards = PlayerPrefs.GetInt(PrefKeyCardCardsPrefix + cardId, 0);
            cardCounts[cardId] = savedCards;
            return savedCards;
        }

        public int GetRequiredCards(string cardId)
        {
            return GetRequiredCardsForLevel(GetLevel(cardId));
        }

        /// <summary>
        /// Cộng thêm số thẻ (mở rương, nhận quà event) vào bộ nhớ thẻ.
        /// </summary>
        public CardRewardResult AddCards(string cardId, int count)
        {
            if (string.IsNullOrEmpty(cardId) || count <= 0)
            {
                return new CardRewardResult(cardId, 0, GetCards(cardId), GetLevel(cardId), GetRequiredCards(cardId));
            }

            int current = GetCards(cardId);
            int updated = current + count;
            cardCounts[cardId] = updated;

            PlayerPrefs.SetInt(PrefKeyCardCardsPrefix + cardId, updated);
            PlayerPrefs.Save();

            InventoryChanged?.Invoke(cardId);

            int lv = GetLevel(cardId);
            int req = GetRequiredCardsForLevel(lv);
            return new CardRewardResult(cardId, count, updated, lv, req);
        }

        /// <summary>
        /// Kiểm tra xem thẻ có đủ số lượng để nâng cấp không.
        /// </summary>
        public bool CanUpgrade(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            int lv = GetLevel(cardId);
            if (lv >= MaxLevel) return false;
            return GetCards(cardId) >= GetRequiredCardsForLevel(lv);
        }

        /// <summary>
        /// Nâng cấp thẻ lên cấp tiếp theo nếu đủ thẻ. Trừ số thẻ yêu cầu và tăng Level.
        /// </summary>
        public bool TryUpgrade(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;

            int level = GetLevel(cardId);
            if (level >= MaxLevel) return false;

            int cards = GetCards(cardId);
            int required = GetRequiredCardsForLevel(level);

            if (cards < required) return false;

            int updatedCards = cards - required;
            int updatedLevel = level + 1;

            cardCounts[cardId] = updatedCards;
            cardLevels[cardId] = updatedLevel;

            PlayerPrefs.SetInt(PrefKeyCardCardsPrefix + cardId, updatedCards);
            PlayerPrefs.SetInt(PrefKeyCardLevelPrefix + cardId, updatedLevel);
            PlayerPrefs.Save();

            InventoryChanged?.Invoke(cardId);
            return true;
        }

        // ==================== CÁC HÀM TIỆN ÍCH DÀNH CHO RƯƠNG VÀ ĐỒ TRANG BỊ ====================

        public CardRewardResult AddSkillCards(string skillId, int count) => AddCards(skillId, count);
        public CardRewardResult AddBowCards(string bowId, int count) => AddCards(bowId, count);

        public int GetSkillLevel(string skillId) => GetLevel(skillId);
        public int GetBowLevel(string bowId) => GetLevel(bowId);

        public int GetSkillCards(string skillId) => GetCards(skillId);
        public int GetBowCards(string bowId) => GetCards(bowId);

        public void SetCards(string cardId, int amount)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            cardCounts[cardId] = Mathf.Max(0, amount);
            PlayerPrefs.SetInt(PrefKeyCardCardsPrefix + cardId, cardCounts[cardId]);
            PlayerPrefs.Save();
            InventoryChanged?.Invoke(cardId);
        }

        public void SetLevel(string cardId, int level)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            cardLevels[cardId] = Mathf.Clamp(level, 1, MaxLevel);
            PlayerPrefs.SetInt(PrefKeyCardLevelPrefix + cardId, cardLevels[cardId]);
            PlayerPrefs.Save();
            InventoryChanged?.Invoke(cardId);
        }

        /// <summary>
        /// Xóa sạch dữ liệu thẻ để test hoặc chơi lại từ đầu.
        /// </summary>
        public void ResetAllCards()
        {
            cardCounts.Clear();
            cardLevels.Clear();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            InventoryChanged?.Invoke(string.Empty);
        }
    }
}
