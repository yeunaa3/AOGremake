using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AOG.Duel
{
    /// <summary>
    /// Hiển thị đồng hồ đếm ngược trận đấu bằng bộ số sprite ở vị trí chính giữa 2 thanh máu trên map.
    /// </summary>
    public sealed class MatchTimerDisplayUI : MonoBehaviour
    {
        [Header("Match Manager")]
        [SerializeField] private GameManager matchManager;

        [Header("Sprites Chữ Số (0 đến 9)")]
        [Tooltip("Gán 10 sprite số từ Digit_0 đến Digit_9")]
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];

        [Header("UI Layout")]
        [Tooltip("Vị trí hiển thị ở giữa 2 thanh máu (mặc định X: 0, Y: -40 từ đỉnh màn hình)")]
        [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -40f);
        [Tooltip("Chiều rộng mỗi chữ số")]
        [SerializeField] private float digitWidth = 44f;
        [Tooltip("Chiều cao mỗi chữ số")]
        [SerializeField] private float digitHeight = 60f;
        [Tooltip("Khoảng cách giữa các chữ số (âm để overlap nhẹ như ảnh thiết kế)")]
        [SerializeField] private float digitSpacing = -6f;

        [Header("Hiển Thị")]
        [Tooltip("Nếu true: chỉ hiển thị số giây (ví dụ: 90, 60, 59... 0). Nếu false: hiển thị mm:ss")]
        [SerializeField] private bool displaySecondsOnly = true;
        [Tooltip("Luôn hiển thị tối thiểu 2 chữ số (ví dụ 09, 08... 00)")]
        [SerializeField] private bool twoDigitsMinimum = true;

        [Header("Container Tự Động")]
        [SerializeField] private RectTransform container;

        private readonly List<Image> digitImages = new List<Image>();
        private int lastDisplayedSeconds = -1;

        private void Awake()
        {
            if (matchManager == null)
            {
                matchManager = GetComponent<GameManager>() ?? FindFirstObjectByType<GameManager>();
            }

            LoadDefaultSprites();
            EnsureContainer();
        }

        private void OnEnable()
        {
            if (matchManager != null)
            {
                matchManager.MatchTimeChanged -= HandleTimeChanged;
                matchManager.MatchTimeChanged += HandleTimeChanged;
                matchManager.StateChanged -= HandleStateChanged;
                matchManager.StateChanged += HandleStateChanged;
            }

            // Ẩn Text timer cũ nếu có trong GameUI để tránh bị đè chữ
            GameUI gameUI = FindFirstObjectByType<GameUI>();
            if (gameUI != null)
            {
                var timerField = typeof(GameUI).GetField("timerText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (timerField != null)
                {
                    var textObj = timerField.GetValue(gameUI) as TMPro.TMP_Text;
                    if (textObj != null) textObj.gameObject.SetActive(false);
                }
            }
        }

        private void OnDisable()
        {
            if (matchManager != null)
            {
                matchManager.MatchTimeChanged -= HandleTimeChanged;
                matchManager.StateChanged -= HandleStateChanged;
            }
        }

        private void Start()
        {
            if (matchManager != null)
            {
                UpdateDisplay(Mathf.CeilToInt(matchManager.MatchTimeRemaining));
            }
        }

        private void LoadDefaultSprites()
        {
            bool needLoad = false;
            if (digitSprites == null || digitSprites.Length < 10)
            {
                digitSprites = new Sprite[10];
                needLoad = true;
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    if (digitSprites[i] == null) { needLoad = true; break; }
                }
            }

            if (needLoad)
            {
#if UNITY_EDITOR
                for (int i = 0; i < 10; i++)
                {
                    string path = $"Assets/Art/UI/TimerDigits/Digit_{i}.png";
                    digitSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
#endif
            }
        }

        private void EnsureContainer()
        {
            if (container != null) return;

            // Tìm Canvas chính trong Scene
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("MatchTimerDigitsContainer");
            if (existing != null)
            {
                container = existing.GetComponent<RectTransform>();
            }
            else
            {
                GameObject obj = new GameObject("MatchTimerDigitsContainer", typeof(RectTransform));
                obj.transform.SetParent(canvas.transform, false);
                container = obj.GetComponent<RectTransform>();
            }

            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.anchoredPosition = anchoredPosition;
            container.sizeDelta = new Vector2(200f, digitHeight);
        }

        private void HandleStateChanged(GameState state)
        {
            if (container != null)
            {
                container.gameObject.SetActive(state == GameState.Playing || state == GameState.Countdown || state == GameState.Finished);
            }
        }

        private void HandleTimeChanged(float remaining)
        {
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds != lastDisplayedSeconds)
            {
                lastDisplayedSeconds = seconds;
                UpdateDisplay(seconds);
            }
        }

        public void UpdateDisplay(int totalSeconds)
        {
            EnsureContainer();
            if (container == null) return;

            string textToDisplay;
            if (displaySecondsOnly)
            {
                if (twoDigitsMinimum)
                {
                    textToDisplay = totalSeconds < 10 ? $"0{Mathf.Max(0, totalSeconds)}" : totalSeconds.ToString();
                }
                else
                {
                    textToDisplay = Mathf.Max(0, totalSeconds).ToString();
                }
            }
            else
            {
                int mins = totalSeconds / 60;
                int secs = totalSeconds % 60;
                textToDisplay = $"{mins:00}{secs:00}";
            }

            int charCount = textToDisplay.Length;
            while (digitImages.Count < charCount)
            {
                GameObject digitObj = new GameObject($"Digit_{digitImages.Count}", typeof(RectTransform), typeof(Image));
                digitObj.transform.SetParent(container, false);
                Image img = digitObj.GetComponent<Image>();
                img.raycastTarget = false;
                digitImages.Add(img);
            }

            // Tính toán bề rộng tổng thể để căn giữa
            float totalWidth = charCount * digitWidth + (charCount - 1) * digitSpacing;
            float startX = -totalWidth * 0.5f + digitWidth * 0.5f;

            for (int i = 0; i < digitImages.Count; i++)
            {
                if (i < charCount)
                {
                    digitImages[i].gameObject.SetActive(true);
                    char c = textToDisplay[i];
                    int digitIndex = c - '0';
                    if (digitIndex >= 0 && digitIndex <= 9 && digitIndex < digitSprites.Length && digitSprites[digitIndex] != null)
                    {
                        digitImages[i].sprite = digitSprites[digitIndex];
                        digitImages[i].color = Color.white;
                    }

                    RectTransform rt = digitImages[i].rectTransform;
                    rt.sizeDelta = new Vector2(digitWidth, digitHeight);
                    rt.anchoredPosition = new Vector2(startX + i * (digitWidth + digitSpacing), -digitHeight * 0.5f);
                }
                else
                {
                    digitImages[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
