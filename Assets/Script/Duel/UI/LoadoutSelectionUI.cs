using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AOG.Duel
{
    // Giao diện chọn Cung và Kỹ Năng dạng popup (Modal) tại màn hình Menu/Lobby.
    public sealed class LoadoutSelectionUI : MonoBehaviour
    {
        [Header("Dữ Liệu")]
        [SerializeField] private LoadoutDatabase database;

        private GameObject canvasRoot;
        private Canvas canvas;
        private GameObject backdrop;
        private GameObject bowModal;
        private GameObject skillModal;

        // Skill Modal elements
        private int selectedSkillSlot = 0;
        private readonly List<GameObject> slotCardObjects = new List<GameObject>();
        private readonly List<GameObject> skillCardObjects = new List<GameObject>();
        private readonly List<GameObject> bowCardObjects = new List<GameObject>();

        public bool IsOpen => (bowModal != null && bowModal.activeSelf) || (skillModal != null && skillModal.activeSelf);

        private void Awake()
        {
            if (database == null)
            {
                database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            }

            EnsureEventSystem();
            BuildUI();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                var inputModule = esGo.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }
        }

        private void BuildUI()
        {
            if (canvasRoot != null) return;

            canvasRoot = new GameObject("LoadoutSelectionCanvas", typeof(RectTransform));
            canvasRoot.transform.SetParent(transform, false);

            canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot.AddComponent<GraphicRaycaster>();

            // 1. Backdrop
            backdrop = CreateUIObject("Backdrop", canvasRoot.transform);
            RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
            SetStretchAll(backdropRect);
            Image backdropImg = backdrop.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.78f);
            Button backdropBtn = backdrop.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.onClick.AddListener(CloseAll);

            // 2. Bow Modal
            BuildBowModal();

            // 3. Skill Modal
            BuildSkillModal();

            // Start closed
            backdrop.SetActive(false);
            bowModal.SetActive(false);
            skillModal.SetActive(false);
        }

        #region Bow Modal
        private void BuildBowModal()
        {
            bowModal = CreateUIObject("BowModal", canvasRoot.transform);
            RectTransform rect = bowModal.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1360, 720);

            // Panel Background
            Image bg = bowModal.AddComponent<Image>();
            bg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (bg.sprite != null && bg.sprite.border != Vector4.zero) bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            // Title
            CreateText("Title", bowModal.transform, "CHỌN CUNG CHIẾN ĐẤU",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -35), new Vector2(800, 50),
                36, FontStyles.Bold, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);

            CreateText("Subtitle", bowModal.transform, "Chọn cây cung bạn muốn mang vào trận đấu",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -85), new Vector2(800, 30),
                20, FontStyles.Normal, new Color(0.75f, 0.8f, 0.9f), TextAlignmentOptions.Center);

            // Close Button [X]
            CreateCloseButton(bowModal.transform, new Vector2(640, 320), CloseAll);

            // Cards Container
            GameObject cardsContainer = CreateUIObject("CardsContainer", bowModal.transform);
            RectTransform cardsRect = cardsContainer.GetComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.5f, 0f);
            cardsRect.anchorMax = new Vector2(0.5f, 0f);
            cardsRect.pivot = new Vector2(0.5f, 0f);
            cardsRect.anchoredPosition = new Vector2(0, 35);
            cardsRect.sizeDelta = new Vector2(1300, 560);

            HorizontalLayoutGroup layout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Generate bow cards
            bowCardObjects.Clear();
            if (database != null && database.Bows != null)
            {
                foreach (BowData bow in database.Bows)
                {
                    if (bow == null) continue;
                    GameObject card = CreateBowCard(cardsContainer.transform, bow);
                    bowCardObjects.Add(card);
                }
            }
        }

        private GameObject CreateBowCard(Transform parent, BowData bow)
        {
            GameObject card = CreateUIObject("BowCard_" + bow.BowId, parent);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(170, 540);

            Image bg = card.AddComponent<Image>();
            bg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (bg.sprite != null && bg.sprite.border != Vector4.zero) bg.type = Image.Type.Sliced;
            bg.color = new Color(0.12f, 0.16f, 0.23f, 1f);

            // Bow Image Container
            GameObject iconObj = CreateUIObject("Icon", card.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0, -110);
            iconRect.sizeDelta = new Vector2(120, 150);

            Image img = iconObj.AddComponent<Image>();
            img.sprite = bow.BowSprite != null ? bow.BowSprite : bow.Icon;
            img.preserveAspect = true;

            // Bow Name
            CreateText("Name", card.transform, bow.DisplayName,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -200), new Vector2(160, 45),
                18, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            // Stats
            string stats = $"Sát thương: <color=#FFDD44>{bow.Damage}</color>\nTốc độ: <color=#66DDFF>{bow.AttackSpeedMultiplier:0.##}x</color>";
            CreateText("Stats", card.transform, stats,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -250), new Vector2(160, 45),
                16, FontStyles.Normal, new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.Center);

            // Passive Description
            string passiveDesc = GetBowPassiveDescription(bow);
            CreateText("Passive", card.transform, passiveDesc,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -350), new Vector2(155, 130),
                14, FontStyles.Italic, new Color(0.7f, 0.85f, 1f), TextAlignmentOptions.Center);

            // Equip Button
            GameObject btnObj = CreateUIObject("EquipButton", card.transform);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = btnRect.anchorMax = btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0, 15);
            btnRect.sizeDelta = new Vector2(150, 46);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (btnImg.sprite != null && btnImg.sprite.border != Vector4.zero) btnImg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            CreateText("BtnText", btnObj.transform, "TRANG BỊ",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                16, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            btn.onClick.AddListener(() =>
            {
                if (PlayerLoadoutManager.HasInstance)
                {
                    PlayerLoadoutManager.Instance.EquipBow(bow);
                    RefreshBowModal();
                }
            });

            return card;
        }

        private void RefreshBowModal()
        {
            if (database == null || database.Bows == null) return;
            BowData equipped = PlayerLoadoutManager.HasInstance ? PlayerLoadoutManager.Instance.EquippedBow : database.DefaultBow;

            for (int i = 0; i < bowCardObjects.Count && i < database.Bows.Count; i++)
            {
                GameObject card = bowCardObjects[i];
                BowData bow = database.Bows[i];
                if (card == null || bow == null) continue;

                bool isEquipped = equipped != null && equipped.BowId == bow.BowId;

                Transform btnT = card.transform.Find("EquipButton");
                if (btnT != null)
                {
                    Button btn = btnT.GetComponent<Button>();
                    Image btnImg = btnT.GetComponent<Image>();
                    TMP_Text txt = btnT.GetComponentInChildren<TMP_Text>();

                    if (isEquipped)
                    {
                        btnImg.color = new Color(0.2f, 0.72f, 0.35f, 1f); // Green
                        if (txt != null) txt.text = "ĐÃ TRANG BỊ";
                        btn.interactable = false;
                    }
                    else
                    {
                        btnImg.color = new Color(0.18f, 0.55f, 0.9f, 1f); // Blue
                        if (txt != null) txt.text = "TRANG BỊ";
                        btn.interactable = true;
                    }
                }

                // Highlight card border
                Image cardBg = card.GetComponent<Image>();
                if (cardBg != null)
                {
                    cardBg.color = isEquipped
                        ? new Color(0.16f, 0.26f, 0.36f, 1f)
                        : new Color(0.12f, 0.16f, 0.23f, 1f);
                }
            }
        }

        private static string GetBowPassiveDescription(BowData bow)
        {
            if (bow == null) return string.Empty;
            string id = bow.BowId != null ? bow.BowId.ToLowerInvariant() : string.Empty;

            if (id.Contains("double")) return "Bắn 2 mũi tên mỗi đòn đánh";
            if (id.Contains("ice")) return "Bắn tên băng làm chậm và đóng băng mục tiêu";
            if (id.Contains("stone")) return "Mũi tên đá nặng đẩy lùi đối thủ mạnh";
            if (id.Contains("lifesteal")) return "Hút máu đối thủ khi bắn trúng";
            if (id.Contains("magnet")) return "Hút đối thủ về tâm quỹ đạo tên";
            if (id.Contains("trap")) return "Đặt bẫy gai trói chân đối thủ";
            if (id.Contains("engle") || id.Contains("eagle")) return "Mũi tên đại bàng uy lực bay cực nhanh";

            return bow.Passive != null ? bow.Passive.name : "Cung cơ bản";
        }
        #endregion

        #region Skill Modal
        private void BuildSkillModal()
        {
            skillModal = CreateUIObject("SkillModal", canvasRoot.transform);
            RectTransform rect = skillModal.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1380, 780);

            // Background
            Image bg = skillModal.AddComponent<Image>();
            bg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (bg.sprite != null && bg.sprite.border != Vector4.zero) bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            // Title
            CreateText("Title", skillModal.transform, "CHỌN KỸ NĂNG CHIẾN ĐẤU",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -30), new Vector2(800, 45),
                34, FontStyles.Bold, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);

            CreateText("Subtitle", skillModal.transform, "Chọn một ô ở hàng trên, sau đó bấm 'Trang bị' kỹ năng ở hàng dưới",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -70), new Vector2(900, 25),
                18, FontStyles.Normal, new Color(0.75f, 0.8f, 0.9f), TextAlignmentOptions.Center);

            // Close Button [X]
            CreateCloseButton(skillModal.transform, new Vector2(650, 350), CloseAll);

            // Top Row: 5 Equipped Slots
            GameObject slotsContainer = CreateUIObject("SlotsContainer", skillModal.transform);
            RectTransform slotsRect = slotsContainer.GetComponent<RectTransform>();
            slotsRect.anchorMin = slotsRect.anchorMax = slotsRect.pivot = new Vector2(0.5f, 1f);
            slotsRect.anchoredPosition = new Vector2(0, -100);
            slotsRect.sizeDelta = new Vector2(1200, 150);

            HorizontalLayoutGroup slotsLayout = slotsContainer.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.spacing = 20;
            slotsLayout.childAlignment = TextAnchor.MiddleCenter;
            slotsLayout.childControlWidth = false;
            slotsLayout.childControlHeight = false;

            slotCardObjects.Clear();
            for (int i = 0; i < 5; i++)
            {
                int slotIdx = i;
                GameObject slotCard = CreateSlotCard(slotsContainer.transform, slotIdx);
                slotCardObjects.Add(slotCard);
            }

            // Divider / Section Header
            CreateText("DividerHeader", skillModal.transform, "KỸ NĂNG KHẢ DỤNG",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -270), new Vector2(500, 30),
                20, FontStyles.Bold, new Color(0.9f, 0.75f, 0.3f), TextAlignmentOptions.Center);

            // Bottom Row: All 7 Available Skills
            GameObject skillsContainer = CreateUIObject("SkillsContainer", skillModal.transform);
            RectTransform skillsRect = skillsContainer.GetComponent<RectTransform>();
            skillsRect.anchorMin = skillsRect.anchorMax = skillsRect.pivot = new Vector2(0.5f, 0f);
            skillsRect.anchoredPosition = new Vector2(0, 25);
            skillsRect.sizeDelta = new Vector2(1320, 460);

            HorizontalLayoutGroup skillsLayout = skillsContainer.AddComponent<HorizontalLayoutGroup>();
            skillsLayout.spacing = 14;
            skillsLayout.childAlignment = TextAnchor.MiddleCenter;
            skillsLayout.childControlWidth = false;
            skillsLayout.childControlHeight = false;

            skillCardObjects.Clear();
            if (database != null && database.Skills != null)
            {
                foreach (SkillData skill in database.Skills)
                {
                    if (skill == null) continue;
                    GameObject card = CreateSkillCard(skillsContainer.transform, skill);
                    skillCardObjects.Add(card);
                }
            }
        }

        private GameObject CreateSlotCard(Transform parent, int slotIndex)
        {
            GameObject card = CreateUIObject("Slot_" + slotIndex, parent);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(210, 135);

            Image bg = card.AddComponent<Image>();
            bg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (bg.sprite != null && bg.sprite.border != Vector4.zero) bg.type = Image.Type.Sliced;
            bg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

            Button btn = card.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() =>
            {
                selectedSkillSlot = slotIndex;
                RefreshSkillModal();
            });

            // Slot Label
            CreateText("SlotLabel", card.transform, $"Ô KỸ NĂNG {slotIndex + 1}",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -12), new Vector2(190, 24),
                16, FontStyles.Bold, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);

            // Icon
            GameObject iconObj = CreateUIObject("Icon", card.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0, -6);
            iconRect.sizeDelta = new Vector2(56, 56);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;

            // Frame
            GameObject frameObj = CreateUIObject("Frame", card.transform);
            RectTransform frameRect = frameObj.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(0, -6);
            frameRect.sizeDelta = new Vector2(64, 64);
            Image frameImg = frameObj.AddComponent<Image>();
            frameImg.preserveAspect = true;

            // Skill Name
            CreateText("SkillName", card.transform, "Chưa chọn",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 10), new Vector2(190, 22),
                15, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

            return card;
        }

        private GameObject CreateSkillCard(Transform parent, SkillData skill)
        {
            GameObject card = CreateUIObject("SkillCard_" + skill.SkillId, parent);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(174, 440);

            Image bg = card.AddComponent<Image>();
            bg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (bg.sprite != null && bg.sprite.border != Vector4.zero) bg.type = Image.Type.Sliced;
            bg.color = new Color(0.12f, 0.16f, 0.23f, 1f);

            // Skill Icon & Frame
            GameObject iconObj = CreateUIObject("Icon", card.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0, -55);
            iconRect.sizeDelta = new Vector2(64, 64);
            Image img = iconObj.AddComponent<Image>();
            img.sprite = skill.Icon;
            img.preserveAspect = true;

            GameObject frameObj = CreateUIObject("Frame", card.transform);
            RectTransform frameRect = frameObj.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(0.5f, 1f);
            frameRect.anchoredPosition = new Vector2(0, -55);
            frameRect.sizeDelta = new Vector2(74, 74);
            Image frameImg = frameObj.AddComponent<Image>();
            frameImg.sprite = database != null && database.Frames != null ? database.Frames.Get(skill.Rarity) : null;
            frameImg.enabled = frameImg.sprite != null;
            frameImg.preserveAspect = true;

            // Name
            CreateText("Name", card.transform, skill.DisplayName,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -115), new Vector2(165, 40),
                17, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            // Rarity
            Color rarityColor = GetRarityColor(skill.Rarity);
            CreateText("Rarity", card.transform, skill.Rarity.ToString().ToUpperInvariant(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -150), new Vector2(160, 24),
                13, FontStyles.Bold, rarityColor, TextAlignmentOptions.Center);

            // Cooldown & Description
            string desc = $"Hồi chiêu: {skill.Cooldown}s\n\n" + (!string.IsNullOrEmpty(skill.Description) ? skill.Description : GetSkillSummary(skill));
            CreateText("Desc", card.transform, desc,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -260), new Vector2(160, 180),
                13, FontStyles.Normal, new Color(0.8f, 0.88f, 0.95f), TextAlignmentOptions.Center);

            // Equip Button
            GameObject btnObj = CreateUIObject("EquipButton", card.transform);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = btnRect.anchorMax = btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0, 14);
            btnRect.sizeDelta = new Vector2(154, 46);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = database != null ? database.RoundedBoxSprite : null;
            if (btnImg.sprite != null && btnImg.sprite.border != Vector4.zero) btnImg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            CreateText("BtnText", btnObj.transform, "GÁN VÀO Ô",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                15, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            btn.onClick.AddListener(() =>
            {
                if (PlayerLoadoutManager.HasInstance)
                {
                    PlayerLoadoutManager.Instance.SetSkill(selectedSkillSlot, skill);
                    RefreshSkillModal();
                }
            });

            return card;
        }

        private void RefreshSkillModal()
        {
            if (!PlayerLoadoutManager.HasInstance || database == null) return;

            SkillData[] equipped = PlayerLoadoutManager.Instance.EquippedSkills;

            // 1. Refresh Top 5 Slot Cards
            for (int i = 0; i < 5 && i < slotCardObjects.Count; i++)
            {
                GameObject slotCard = slotCardObjects[i];
                if (slotCard == null) continue;

                SkillData skill = equipped != null && i < equipped.Length ? equipped[i] : null;

                bool isSelected = selectedSkillSlot == i;
                Image bg = slotCard.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = isSelected
                        ? new Color(0.22f, 0.35f, 0.52f, 1f)
                        : new Color(0.11f, 0.15f, 0.22f, 1f);
                }

                TMP_Text labelTxt = slotCard.transform.Find("SlotLabel")?.GetComponent<TMP_Text>();
                if (labelTxt != null)
                {
                    labelTxt.text = isSelected ? $"► Ô KỸ NĂNG {i + 1} ◄" : $"Ô KỸ NĂNG {i + 1}";
                    labelTxt.color = isSelected ? new Color(1f, 0.88f, 0.3f) : new Color(0.7f, 0.75f, 0.85f);
                }

                Image icon = slotCard.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = skill != null ? skill.Icon : null;
                    icon.enabled = icon.sprite != null;
                }

                Image frame = slotCard.transform.Find("Frame")?.GetComponent<Image>();
                if (frame != null)
                {
                    frame.sprite = skill != null && database.Frames != null ? database.Frames.Get(skill.Rarity) : null;
                    frame.enabled = frame.sprite != null;
                }

                TMP_Text nameTxt = slotCard.transform.Find("SkillName")?.GetComponent<TMP_Text>();
                if (nameTxt != null)
                {
                    nameTxt.text = skill != null ? skill.DisplayName : "(Trống)";
                }
            }

            // 2. Refresh Bottom Available Skill Cards
            for (int i = 0; i < skillCardObjects.Count && i < database.Skills.Count; i++)
            {
                GameObject card = skillCardObjects[i];
                SkillData skill = database.Skills[i];
                if (card == null || skill == null) continue;

                int equippedAt = -1;
                if (equipped != null)
                {
                    for (int s = 0; s < equipped.Length; s++)
                    {
                        if (equipped[s] != null && equipped[s].SkillId == skill.SkillId)
                        {
                            equippedAt = s;
                            break;
                        }
                    }
                }

                Transform btnT = card.transform.Find("EquipButton");
                if (btnT != null)
                {
                    Button btn = btnT.GetComponent<Button>();
                    Image btnImg = btnT.GetComponent<Image>();
                    TMP_Text txt = btnT.GetComponentInChildren<TMP_Text>();

                    if (equippedAt == selectedSkillSlot)
                    {
                        // Already in the active selected slot
                        btnImg.color = new Color(0.2f, 0.7f, 0.35f, 1f); // Green
                        if (txt != null) txt.text = $"ĐANG Ở Ô {selectedSkillSlot + 1}";
                        btn.interactable = false;
                    }
                    else if (equippedAt >= 0)
                    {
                        // In another slot
                        btnImg.color = new Color(0.85f, 0.5f, 0.15f, 1f); // Orange
                        if (txt != null) txt.text = $"ĐỔI SANG Ô {selectedSkillSlot + 1}";
                        btn.interactable = true;
                    }
                    else
                    {
                        // Not equipped anywhere
                        btnImg.color = new Color(0.18f, 0.55f, 0.9f, 1f); // Cyan
                        if (txt != null) txt.text = $"GÁN VÀO Ô {selectedSkillSlot + 1}";
                        btn.interactable = true;
                    }
                }
            }
        }

        private static Color GetRarityColor(SkillRarity rarity)
        {
            switch (rarity)
            {
                case SkillRarity.Common: return new Color(0.7f, 0.8f, 0.9f);
                case SkillRarity.Rare: return new Color(0.3f, 0.65f, 1f);
                case SkillRarity.Heroic: return new Color(0.8f, 0.4f, 1f);
                case SkillRarity.Legendary: return new Color(1f, 0.8f, 0.2f);
                default: return Color.white;
            }
        }

        private static string GetSkillSummary(SkillData skill)
        {
            if (skill == null) return string.Empty;
            string id = skill.SkillId != null ? skill.SkillId.ToLowerInvariant() : string.Empty;

            if (id.Contains("freeze")) return "Bắn mũi tên băng gây sát thương và làm chậm đối thủ.";
            if (id.Contains("flame")) return "Bắn luồng lửa thiêu đốt đối thủ trong tầm.";
            if (id.Contains("rain")) return "Gọi trận mưa tên rải đều từ trên không xuống đối phương.";
            if (id.Contains("jump")) return "Bật nhảy lên không trung và bắn tên uy lực.";
            if (id.Contains("poison")) return "Bắn tên độc tạo vũng độc gây sát thương liên tục.";
            if (id.Contains("dash")) return "Lướt nhanh né tránh đồng thời phản công bằng đòn bắn.";
            if (id.Contains("many")) return "Bắn ra nhiều mũi tên cùng lúc theo hình cánh quạt.";

            return "Kỹ năng đặc biệt của xạ thủ.";
        }
        #endregion

        #region Public Open / Close
        public void OpenBowSelection()
        {
            BuildUI();
            backdrop.SetActive(true);
            bowModal.SetActive(true);
            skillModal.SetActive(false);
            RefreshBowModal();
        }

        public void OpenSkillSelection(int targetSlot = 0)
        {
            BuildUI();
            selectedSkillSlot = Mathf.Clamp(targetSlot, 0, 4);
            backdrop.SetActive(true);
            bowModal.SetActive(false);
            skillModal.SetActive(true);
            RefreshSkillModal();
        }

        public void CloseAll()
        {
            if (backdrop != null) backdrop.SetActive(false);
            if (bowModal != null) bowModal.SetActive(false);
            if (skillModal != null) skillModal.SetActive(false);
        }
        #endregion

        #region Helpers
        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
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
            Vector2 anchoredPosition, Vector2 sizeDelta,
            float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            if (sizeDelta != Vector2.zero) rect.sizeDelta = sizeDelta;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (database != null && database.FontAsset != null) text.font = database.FontAsset;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private void CreateCloseButton(Transform parent, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = CreateUIObject("CloseButton", parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(44, 44);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = database != null ? database.CircleSprite : null;
            img.color = new Color(0.85f, 0.25f, 0.25f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(action);

            CreateText("X", btnObj.transform, "X",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        }
        #endregion
    }
}
