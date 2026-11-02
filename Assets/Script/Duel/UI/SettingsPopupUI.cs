using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AOG.Duel
{
    // Popup Cài đặt trò chơi (Âm lượng, Đồ hoạ, và Đổi phím điều khiển PVP)
    public sealed class SettingsPopupUI : MonoBehaviour
    {
        [Header("Dữ Liệu")]
        [SerializeField] private LoadoutDatabase database;

        private GameObject canvasRoot;
        private GameObject backdrop;
        private GameObject modal;

        // Tabs
        private GameObject audioTabContent;
        private GameObject controlsTabContent;
        private Image audioTabBtnImg;
        private Image controlsTabBtnImg;
        private TMP_Text audioTabBtnTxt;
        private TMP_Text controlsTabBtnTxt;
        private int currentTab = 0; // 0 = Audio/System, 1 = Controls

        // P2 Loadout Mode UI
        private TMP_Text p2ModeBtnTxt;

        // Keybinding Rebind State
        private bool isRebinding = false;
        private int rebindPlayerIndex = 0;
        private int rebindActionType = 0; // 0 = MoveLeft, 1 = MoveRight, 2 = Skill
        private int rebindSkillSlot = 0;
        private TMP_Text activeRebindBtnTxt;
        private Button activeRebindBtn;

        // Button label references for refreshing
        private readonly List<(int player, int action, int slot, TMP_Text txt)> keyLabels = new List<(int, int, int, TMP_Text)>();

        public bool IsOpen => modal != null && modal.activeSelf;

        private void Awake()
        {
            if (database == null) database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            LoadInitialSettings();
            EnsureEventSystem();
            BuildUI();
        }

        private void Update()
        {
            if (!isRebinding || Keyboard.current == null) return;

            // Bấm Escape để hủy đổi phím
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelRebind();
                return;
            }

            // Quét phím bấm bất kỳ
            foreach (KeyControl k in Keyboard.current.allKeys)
            {
                if (k.wasPressedThisFrame)
                {
                    Key pressed = k.keyCode;
                    if (pressed != Key.None)
                    {
                        ApplyRebind(pressed);
                        break;
                    }
                }
            }
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
            modalRect.sizeDelta = new Vector2(880, 620);

            var modalImg = modal.AddComponent<Image>();
            modalImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (modalImg.sprite != null && modalImg.sprite.border != Vector4.zero) modalImg.type = Image.Type.Sliced;
            modalImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            // Tiêu đề
            CreateText("Title", modal.transform, "CÀI ĐẶT TRÒ CHƠI",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -32), new Vector2(600, 45),
                30, FontStyles.Bold, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);

            // Nút đóng [X]
            CreateCloseButton(modal.transform, new Vector2(-22, -22), Close);

            // 3. Tab Bar (Hệ thống & Âm thanh | Phím Điều Khiển PVP)
            CreateTabBar(modal.transform);

            // 4. Tab Content 1: Âm thanh & Toàn màn hình
            BuildAudioTabContent(modal.transform);

            // 5. Tab Content 2: Điều khiển Bàn phím (P1 & P2 Local PVP)
            BuildControlsTabContent(modal.transform);

            // Chuyển sang Tab mặc định
            SelectTab(0);

            // Mặc định ban đầu ẩn popup
            backdrop.SetActive(false);
            modal.SetActive(false);
        }

        private void CreateTabBar(Transform parent)
        {
            var tabRoot = CreateUIObject("TabBar", parent);
            var rect = tabRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -78);
            rect.sizeDelta = new Vector2(800, 42);

            var layout = tabRoot.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Tab 0: Hệ thống / Âm thanh
            var t0 = CreateUIObject("TabBtn_Audio", tabRoot.transform);
            audioTabBtnImg = t0.AddComponent<Image>();
            audioTabBtnImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (audioTabBtnImg.sprite != null && audioTabBtnImg.sprite.border != Vector4.zero) audioTabBtnImg.type = Image.Type.Sliced;
            var b0 = t0.AddComponent<Button>();
            b0.onClick.AddListener(() => SelectTab(0));
            audioTabBtnTxt = CreateText("Label", t0.transform, "ÂM THANH & HỆ THỐNG",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 17, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            // Tab 1: Phím điều khiển PVP
            var t1 = CreateUIObject("TabBtn_Controls", tabRoot.transform);
            controlsTabBtnImg = t1.AddComponent<Image>();
            controlsTabBtnImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (controlsTabBtnImg.sprite != null && controlsTabBtnImg.sprite.border != Vector4.zero) controlsTabBtnImg.type = Image.Type.Sliced;
            var b1 = t1.AddComponent<Button>();
            b1.onClick.AddListener(() => SelectTab(1));
            controlsTabBtnTxt = CreateText("Label", t1.transform, "ĐIỀU KHIỂN & ĐỒ PVP",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 17, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        }

        private void SelectTab(int tabIndex)
        {
            CancelRebind();
            currentTab = tabIndex;

            if (audioTabContent != null) audioTabContent.SetActive(tabIndex == 0);
            if (controlsTabContent != null) controlsTabContent.SetActive(tabIndex == 1);

            Color activeColor = new Color(0.2f, 0.55f, 0.9f, 1f);
            Color inactiveColor = new Color(0.12f, 0.16f, 0.22f, 0.8f);

            if (audioTabBtnImg != null) audioTabBtnImg.color = tabIndex == 0 ? activeColor : inactiveColor;
            if (controlsTabBtnImg != null) controlsTabBtnImg.color = tabIndex == 1 ? activeColor : inactiveColor;

            if (audioTabBtnTxt != null) audioTabBtnTxt.color = tabIndex == 0 ? Color.white : new Color(0.7f, 0.75f, 0.8f);
            if (controlsTabBtnTxt != null) controlsTabBtnTxt.color = tabIndex == 1 ? Color.white : new Color(0.7f, 0.75f, 0.8f);

            if (tabIndex == 1)
            {
                RefreshP2ModeButton();
                RefreshAllKeyLabels();
            }
        }

        private void BuildAudioTabContent(Transform parent)
        {
            audioTabContent = CreateUIObject("AudioTabContent", parent);
            var rect = audioTabContent.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -35);
            rect.sizeDelta = new Vector2(760, 380);

            var layout = audioTabContent.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Hàng 1: Âm lượng tổng (Master Volume)
            CreateSliderRow(audioTabContent.transform, "Âm lượng tổng (Master)",
                PlayerPrefs.GetFloat("Master_Volume", 1f), (v) =>
                {
                    AudioListener.volume = v;
                    PlayerPrefs.SetFloat("Master_Volume", v);
                });

            // Hàng 2: Nhạc nền (BGM)
            CreateSliderRow(audioTabContent.transform, "Nhạc nền (BGM)",
                PlayerPrefs.GetFloat("BGM_Volume", 0.5f), (v) =>
                {
                    PlayerPrefs.SetFloat("BGM_Volume", v);
                });

            // Hàng 3: Hiệu ứng âm thanh (SFX)
            CreateSliderRow(audioTabContent.transform, "Hiệu ứng (SFX)",
                PlayerPrefs.GetFloat("SFX_Volume", 0.8f), (v) =>
                {
                    PlayerPrefs.SetFloat("SFX_Volume", v);
                });

            // Hàng 4: Toàn màn hình (Fullscreen)
            CreateToggleRow(audioTabContent.transform, "Chế độ toàn màn hình",
                Screen.fullScreen, (val) =>
                {
                    Screen.fullScreen = val;
                    PlayerPrefs.SetInt("Fullscreen", val ? 1 : 0);
                });
        }

        private void BuildControlsTabContent(Transform parent)
        {
            controlsTabContent = CreateUIObject("ControlsTabContent", parent);
            var rect = controlsTabContent.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -35);
            rect.sizeDelta = new Vector2(830, 450);

            // 1. Thanh chọn chế độ trang bị cho Player 2
            var p2ModeBar = CreateUIObject("P2ModeBar", controlsTabContent.transform);
            var modeRect = p2ModeBar.GetComponent<RectTransform>();
            modeRect.anchorMin = new Vector2(0.5f, 1f);
            modeRect.anchorMax = new Vector2(0.5f, 1f);
            modeRect.pivot = new Vector2(0.5f, 1f);
            modeRect.anchoredPosition = Vector2.zero;
            modeRect.sizeDelta = new Vector2(800, 38);

            CreateText("Label", p2ModeBar.transform, "Trang bị Người chơi 2 (PVP):",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(5, 0), new Vector2(240, 36), 16, FontStyles.Bold,
                new Color(0.9f, 0.92f, 0.95f), TextAlignmentOptions.MidlineLeft);

            var p2BtnObj = CreateUIObject("P2ModeBtn", p2ModeBar.transform);
            var p2BtnRect = p2BtnObj.GetComponent<RectTransform>();
            p2BtnRect.anchorMin = new Vector2(1, 0.5f);
            p2BtnRect.anchorMax = new Vector2(1, 0.5f);
            p2BtnRect.pivot = new Vector2(1, 0.5f);
            p2BtnRect.anchoredPosition = new Vector2(-5, 0);
            p2BtnRect.sizeDelta = new Vector2(540, 34);

            var p2BtnImg = p2BtnObj.AddComponent<Image>();
            p2BtnImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (p2BtnImg.sprite != null && p2BtnImg.sprite.border != Vector4.zero) p2BtnImg.type = Image.Type.Sliced;
            p2BtnImg.color = new Color(0.18f, 0.35f, 0.55f, 1f);

            var p2Btn = p2BtnObj.AddComponent<Button>();
            p2Btn.targetGraphic = p2BtnImg;
            p2Btn.onClick.AddListener(CycleP2Mode);

            p2ModeBtnTxt = CreateText("Txt", p2BtnObj.transform, GetP2ModeText(GameModeManager.P2Mode),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 15, FontStyles.Bold, new Color(1f, 0.9f, 0.3f), TextAlignmentOptions.Center);

            // 2. Container 2 cột: Cột trái P1, Cột phải P2
            var columnsContainer = CreateUIObject("ColumnsContainer", controlsTabContent.transform);
            var colRect = columnsContainer.GetComponent<RectTransform>();
            colRect.anchorMin = new Vector2(0.5f, 1f);
            colRect.anchorMax = new Vector2(0.5f, 1f);
            colRect.pivot = new Vector2(0.5f, 1f);
            colRect.anchoredPosition = new Vector2(0, -46);
            colRect.sizeDelta = new Vector2(810, 325);

            var horiz = columnsContainer.AddComponent<HorizontalLayoutGroup>();
            horiz.spacing = 25;
            horiz.childAlignment = TextAnchor.UpperCenter;
            horiz.childControlWidth = true;
            horiz.childControlHeight = true;
            horiz.childForceExpandWidth = true;
            horiz.childForceExpandHeight = true;

            keyLabels.Clear();

            // Cột 1: NGƯỜI CHƠI 1 (P1)
            BuildPlayerKeyColumn(columnsContainer.transform, 0, "NGƯỜI CHƠI 1 (P1 - BÊN TRÁI)", new Color(0.25f, 0.7f, 1f));

            // Cột 2: NGƯỜI CHƠI 2 (P2)
            BuildPlayerKeyColumn(columnsContainer.transform, 1, "NGƯỜI CHƠI 2 (P2 - BÊN PHẢI)", new Color(1f, 0.45f, 0.35f));

            // 3. Hàng dưới cùng: Nút Đặt lại mặc định + Hướng dẫn
            var bottomBar = CreateUIObject("BottomBar", controlsTabContent.transform);
            var botRect = bottomBar.GetComponent<RectTransform>();
            botRect.anchorMin = new Vector2(0, 0);
            botRect.anchorMax = new Vector2(1, 0);
            botRect.pivot = new Vector2(0.5f, 0f);
            botRect.anchoredPosition = Vector2.zero;
            botRect.sizeDelta = new Vector2(0, 50);

            // Nút Reset Default
            var resetBtnObj = CreateUIObject("ResetBtn", bottomBar.transform);
            var resetRect = resetBtnObj.GetComponent<RectTransform>();
            resetRect.anchorMin = new Vector2(0.5f, 0.5f);
            resetRect.anchorMax = new Vector2(0.5f, 0.5f);
            resetRect.pivot = new Vector2(0.5f, 0.5f);
            resetRect.anchoredPosition = new Vector2(-190, 0);
            resetRect.sizeDelta = new Vector2(200, 36);

            var rImg = resetBtnObj.AddComponent<Image>();
            rImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (rImg.sprite != null && rImg.sprite.border != Vector4.zero) rImg.type = Image.Type.Sliced;
            rImg.color = new Color(0.45f, 0.2f, 0.2f, 1f);

            var rBtn = resetBtnObj.AddComponent<Button>();
            rBtn.targetGraphic = rImg;
            rBtn.onClick.AddListener(() =>
            {
                CancelRebind();
                KeybindingManager.ResetToDefaults();
                RefreshAllKeyLabels();
            });

            CreateText("Label", resetBtnObj.transform, "↺ ĐẶT LẠI PHÍM",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            // Hướng dẫn
            CreateText("Hint", bottomBar.transform,
                "• Click ô phím để đổi (Escape để hủy). P2 hỗ trợ cả J/K/L/U/I và Numpad 1..5.\n• Đổi chế độ trang bị P2 ở trên để mỗi ván 1 bộ chiêu ngẫu nhiên hoặc dùng Set II.",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(130, 0), new Vector2(450, 42), 12, FontStyles.Normal,
                new Color(0.8f, 0.85f, 0.9f), TextAlignmentOptions.MidlineLeft);
        }

        private void CycleP2Mode()
        {
            P2LoadoutMode mode = GameModeManager.P2Mode;
            int next = ((int)mode + 1) % 3;
            GameModeManager.P2Mode = (P2LoadoutMode)next;
            RefreshP2ModeButton();
        }

        private void RefreshP2ModeButton()
        {
            if (p2ModeBtnTxt == null) return;
            p2ModeBtnTxt.text = GetP2ModeText(GameModeManager.P2Mode);
        }

        private string GetP2ModeText(P2LoadoutMode mode)
        {
            switch (mode)
            {
                case P2LoadoutMode.Random:
                    return "🎲 NGẪU NHIÊN (Mỗi trận 1 bộ chiêu & cung khác biệt)";
                case P2LoadoutMode.UseSet2:
                    return "📋 DÙNG SET KỸ NĂNG II (Tự chọn trong màn Loadout)";
                case P2LoadoutMode.Mirror:
                    return "🪞 GIỐNG PLAYER 1 (Solo Mirror cùng chiêu)";
                default:
                    return "🎲 NGẪU NHIÊN";
            }
        }

        private void BuildPlayerKeyColumn(Transform parent, int playerIndex, string headerTitle, Color headerColor)
        {
            var col = CreateUIObject($"P{playerIndex}_Column", parent);
            var img = col.AddComponent<Image>();
            img.sprite = database != null ? database.RoundedBoxSprite : null;
            if (img.sprite != null && img.sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            img.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);

            var layout = col.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Tiêu đề cột
            CreateText("Header", col.transform, headerTitle,
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(0, 26), 15, FontStyles.Bold, headerColor, TextAlignmentOptions.Center);

            // 1. Di chuyển Trái
            CreateKeybindRow(col.transform, playerIndex, 0, 0, "Di chuyển Trái", () => KeybindingManager.GetMoveLeft(playerIndex));

            // 2. Di chuyển Phải
            CreateKeybindRow(col.transform, playerIndex, 1, 0, "Di chuyển Phải", () => KeybindingManager.GetMoveRight(playerIndex));

            // 3. Kỹ năng 1..5
            for (int s = 0; s < 5; s++)
            {
                int slot = s;
                CreateKeybindRow(col.transform, playerIndex, 2, slot, $"Kỹ năng ô {slot + 1}", () => KeybindingManager.GetSkill(playerIndex, slot));
            }
        }

        private void CreateKeybindRow(Transform parent, int playerIndex, int actionType, int slot, string label, System.Func<Key> getKey)
        {
            var row = CreateUIObject(label + "_Row", parent);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            CreateText("ActionLabel", row.transform, label,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(5, 0), new Vector2(170, 28), 13, FontStyles.Normal,
                new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.MidlineLeft);

            var btnObj = CreateUIObject("KeyBtn", row.transform);
            var btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-5, 0);
            btnRect.sizeDelta = new Vector2(130, 26);

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (btnImg.sprite != null && btnImg.sprite.border != Vector4.zero) btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.16f, 0.22f, 0.32f, 1f);

            var keyBtn = btnObj.AddComponent<Button>();
            keyBtn.targetGraphic = btnImg;

            var keyTxt = CreateText("KeyTxt", btnObj.transform, KeybindingManager.GetReadableKeyName(getKey()),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 13, FontStyles.Bold, new Color(1f, 0.9f, 0.35f), TextAlignmentOptions.Center);

            keyLabels.Add((playerIndex, actionType, slot, keyTxt));

            keyBtn.onClick.AddListener(() =>
            {
                StartRebind(playerIndex, actionType, slot, keyBtn, keyTxt);
            });
        }

        private void StartRebind(int playerIndex, int actionType, int slot, Button btn, TMP_Text txt)
        {
            CancelRebind();

            isRebinding = true;
            rebindPlayerIndex = playerIndex;
            rebindActionType = actionType;
            rebindSkillSlot = slot;
            activeRebindBtn = btn;
            activeRebindBtnTxt = txt;

            if (activeRebindBtn != null && activeRebindBtn.targetGraphic is Image img)
            {
                img.color = new Color(0.7f, 0.5f, 0.1f, 1f);
            }
            if (activeRebindBtnTxt != null)
            {
                activeRebindBtnTxt.text = "[ Bấm phím... ]";
                activeRebindBtnTxt.color = Color.white;
            }
        }

        private void CancelRebind()
        {
            if (!isRebinding) return;
            isRebinding = false;
            RefreshAllKeyLabels();
            activeRebindBtn = null;
            activeRebindBtnTxt = null;
        }

        private void ApplyRebind(Key newKey)
        {
            if (rebindActionType == 0)
            {
                KeybindingManager.SetMoveLeft(rebindPlayerIndex, newKey);
            }
            else if (rebindActionType == 1)
            {
                KeybindingManager.SetMoveRight(rebindPlayerIndex, newKey);
            }
            else
            {
                KeybindingManager.SetSkill(rebindPlayerIndex, rebindSkillSlot, newKey);
            }

            isRebinding = false;
            activeRebindBtn = null;
            activeRebindBtnTxt = null;
            RefreshAllKeyLabels();
        }

        private void RefreshAllKeyLabels()
        {
            foreach (var item in keyLabels)
            {
                if (item.txt == null) continue;
                Key key;
                if (item.action == 0) key = KeybindingManager.GetMoveLeft(item.player);
                else if (item.action == 1) key = KeybindingManager.GetMoveRight(item.player);
                else key = KeybindingManager.GetSkill(item.player, item.slot);

                item.txt.text = KeybindingManager.GetReadableKeyName(key);
                item.txt.color = new Color(1f, 0.9f, 0.35f);

                var img = item.txt.transform.parent.GetComponent<Image>();
                if (img != null) img.color = new Color(0.16f, 0.22f, 0.32f, 1f);
            }
        }

        public void Open()
        {
            BuildUI();
            backdrop.SetActive(true);
            modal.SetActive(true);
            SelectTab(currentTab);
        }

        public void Close()
        {
            CancelRebind();
            if (backdrop != null) backdrop.SetActive(false);
            if (modal != null) modal.SetActive(false);
            PlayerPrefs.Save();
        }

        #region UI Helpers
        private GameObject CreateSliderRow(Transform parent, string title, float initialVal, System.Action<float> onChange)
        {
            var row = CreateUIObject(title + "_Row", parent);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 50);

            CreateText("Label", row.transform, title,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(240, 40), 20, FontStyles.Bold,
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
            sRect.sizeDelta = new Vector2(-400, 18);

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
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 50);

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
