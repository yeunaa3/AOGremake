using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AOG.Duel
{
    // Popup Cài đặt trò chơi (Âm lượng, Đồ hoạ, Toàn màn hình)
    public sealed class SettingsPopupUI : MonoBehaviour
    {
        [Header("Dữ Liệu")]
        [SerializeField] private LoadoutDatabase database;

        private GameObject canvasRoot;
        private GameObject backdrop;
        private GameObject modal;

        public bool IsOpen => modal != null && modal.activeSelf;

        private void Awake()
        {
            if (database == null) database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            LoadInitialSettings();
            EnsureEventSystem();
            BuildUI();
        }

        private void LoadInitialSettings()
        {
            float master = PlayerPrefs.GetFloat("Master_Volume", 1f);
            AudioListener.volume = master;

            int fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
            Screen.fullScreen = fullscreen == 1;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void BuildUI()
        {
            if (canvasRoot != null) return;

            canvasRoot = CreateUIObject("SettingsCanvas", transform);
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // Luôn hiển thị trên cùng

            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot.AddComponent<GraphicRaycaster>();

            // 1. Backdrop làm mờ màn hình (bấm vào là đóng popup)
            backdrop = CreateUIObject("Backdrop", canvasRoot.transform);
            SetStretchAll(backdrop.GetComponent<RectTransform>());
            var bgImg = backdrop.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.75f);
            var btn = backdrop.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Close);

            // 2. Khung Modal Cài Đặt
            modal = CreateUIObject("SettingsModal", canvasRoot.transform);
            var modalRect = modal.GetComponent<RectTransform>();
            modalRect.anchorMin = modalRect.anchorMax = modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(760, 520);

            var modalImg = modal.AddComponent<Image>();
            modalImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (modalImg.sprite != null && modalImg.sprite.border != Vector4.zero) modalImg.type = Image.Type.Sliced;
            modalImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            // Tiêu đề
            CreateText("Title", modal.transform, "CÀI ĐẶT TRÒ CHƠI",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -35), new Vector2(600, 50),
                32, FontStyles.Bold, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);

            // Nút đóng [X]
            CreateCloseButton(modal.transform, new Vector2(-25, -25), Close);

            // Container chứa các hàng cài đặt
            var container = CreateUIObject("SettingsContainer", modal.transform);
            var contRect = container.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0.5f);
            contRect.anchorMax = new Vector2(0.5f, 0.5f);
            contRect.pivot = new Vector2(0.5f, 0.5f);
            contRect.anchoredPosition = new Vector2(0, -30);
            contRect.sizeDelta = new Vector2(660, 360);

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Hàng 1: Âm lượng tổng (Master Volume)
            CreateSliderRow(container.transform, "Âm lượng tổng (Master)",
                PlayerPrefs.GetFloat("Master_Volume", 1f), (v) =>
                {
                    AudioListener.volume = v;
                    PlayerPrefs.SetFloat("Master_Volume", v);
                });

            // Hàng 2: Nhạc nền (BGM)
            CreateSliderRow(container.transform, "Nhạc nền (BGM)",
                PlayerPrefs.GetFloat("BGM_Volume", 0.5f), (v) =>
                {
                    PlayerPrefs.SetFloat("BGM_Volume", v);
                });

            // Hàng 3: Hiệu ứng âm thanh (SFX)
            CreateSliderRow(container.transform, "Hiệu ứng (SFX)",
                PlayerPrefs.GetFloat("SFX_Volume", 0.8f), (v) =>
                {
                    PlayerPrefs.SetFloat("SFX_Volume", v);
                });

            // Hàng 4: Toàn màn hình (Fullscreen)
            CreateToggleRow(container.transform, "Chế độ toàn màn hình",
                Screen.fullScreen, (val) =>
                {
                    Screen.fullScreen = val;
                    PlayerPrefs.SetInt("Fullscreen", val ? 1 : 0);
                });

            // Mặc định ban đầu ẩn popup
            backdrop.SetActive(false);
            modal.SetActive(false);
        }

        public void Open()
        {
            BuildUI();
            backdrop.SetActive(true);
            modal.SetActive(true);
        }

        public void Close()
        {
            if (backdrop != null) backdrop.SetActive(false);
            if (modal != null) modal.SetActive(false);
            PlayerPrefs.Save();
        }

        #region UI Helpers
        private GameObject CreateSliderRow(Transform parent, string title, float initialVal, System.Action<float> onChange)
        {
            var row = CreateUIObject(title + "_Row", parent);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(620, 50);

            CreateText("Label", row.transform, title,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(230, 40), 20, FontStyles.Bold,
                new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.MidlineLeft);

            var pct = CreateText("Pct", row.transform, $"{Mathf.RoundToInt(initialVal * 100)}%",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-10, 0), new Vector2(60, 40), 18, FontStyles.Normal,
                new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.MidlineRight);

            var sObj = CreateUIObject("Slider", row.transform);
            var sRect = sObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0.5f);
            sRect.anchorMax = new Vector2(1, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(50, 0);
            sRect.sizeDelta = new Vector2(-360, 18);

            var slider = sObj.AddComponent<Slider>();

            var bg = CreateUIObject("BG", sObj.transform);
            SetStretchAll(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(0.18f, 0.22f, 0.3f, 1f);

            var fillArea = CreateUIObject("Fill Area", sObj.transform);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            SetStretchAll(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fill = CreateUIObject("Fill", fillArea.transform);
            var fillRect = fill.GetComponent<RectTransform>();
            SetStretchAll(fillRect);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.7f, 1f, 1f);

            var handleArea = CreateUIObject("Handle Area", sObj.transform);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            SetStretchAll(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(8, 0);
            handleAreaRect.offsetMax = new Vector2(-8, 0);

            var handle = CreateUIObject("Handle", handleArea.transform);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24, 24);
            var handleImg = handle.AddComponent<Image>();
            handleImg.sprite = database != null ? database.CircleSprite : null;
            handleImg.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialVal;

            slider.onValueChanged.AddListener((v) =>
            {
                pct.text = $"{Mathf.RoundToInt(v * 100)}%";
                onChange?.Invoke(v);
            });

            return row;
        }

        private GameObject CreateToggleRow(Transform parent, string title, bool initialVal, System.Action<bool> onChange)
        {
            var row = CreateUIObject(title + "_Row", parent);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(620, 50);

            CreateText("Label", row.transform, title,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(300, 40), 20, FontStyles.Bold,
                new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.MidlineLeft);

            var tObj = CreateUIObject("Toggle", row.transform);
            var tRect = tObj.GetComponent<RectTransform>();
            tRect.anchorMin = tRect.anchorMax = tRect.pivot = new Vector2(1, 0.5f);
            tRect.anchoredPosition = new Vector2(-20, 0);
            tRect.sizeDelta = new Vector2(36, 36);

            var toggle = tObj.AddComponent<Toggle>();
            var bgImg = tObj.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.22f, 0.3f, 1f);

            var check = CreateUIObject("Check", tObj.transform);
            var checkRect = check.GetComponent<RectTransform>();
            SetStretchAll(checkRect);
            checkRect.offsetMin = new Vector2(6, 6);
            checkRect.offsetMax = new Vector2(-6, -6);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.85f, 0.4f, 1f);

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = initialVal;
            toggle.onValueChanged.AddListener((v) => onChange?.Invoke(v));

            return row;
        }

        private void CreateCloseButton(Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var btnObj = CreateUIObject("CloseButton", parent);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(44, 44);

            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.85f, 0.25f, 0.25f, 1f);

            var b = btnObj.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(action);

            CreateText("X", btnObj.transform, "✕",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(44, 44), 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private TMP_Text CreateText(string name, Transform parent, string content,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            if (size != Vector2.zero) rect.sizeDelta = size;

            var txt = go.AddComponent<TextMeshProUGUI>();
            if (database != null && database.FontAsset != null) txt.font = database.FontAsset;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            txt.raycastTarget = false;
            return txt;
        }
        #endregion
    }
}
