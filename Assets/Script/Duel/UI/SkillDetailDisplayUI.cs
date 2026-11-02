using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    // Hiển thị chi tiết kỹ năng đang được chọn trong màn hình Loadout:
    // - Ô thẻ kỹ năng (Card Slot) với icon, khung độ hiếm, cấp độ và thanh tiến độ.
    // - Tên kỹ năng & độ hiếm (màu sắc theo cấp độ hiếm).
    // - Ô thông số Cooldown kèm icon đồng hồ.
    // - Ô mô tả kỹ năng kèm phân loại (Ability / Magic / Summon / Buff...) và nội dung chi tiết.
    // - Nút Back để đóng panel lại cho đỡ vướng tầm nhìn.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SkillDetailDisplayUI : MonoBehaviour
    {
        [Header("Card Slot Hiển Thị Kỹ Năng Đang Chọn")]
        [SerializeField] private SkillCardSlotUI selectedSkillSlot;

        [Header("Tiêu Đề & Độ Hiếm")]
        [SerializeField] private TextMeshPro rarityText;
        [SerializeField] private TextMeshPro skillNameText;

        [Header("Ô Cooldown")]
        [SerializeField] private GameObject cooldownBox;
        [SerializeField] private SpriteRenderer cooldownIconRenderer;
        [SerializeField] private TextMeshPro cooldownLabelText;
        [SerializeField] private TextMeshPro cooldownValueText;

        [Header("Ô Mô Tả Kỹ Năng (Description Box)")]
        [SerializeField] private GameObject descriptionBox;
        [SerializeField] private SpriteRenderer typeIconRenderer;
        [SerializeField] private TextMeshPro categoryLabelText;
        [SerializeField] private TextMeshPro typeText;
        [SerializeField] private TextMeshPro descriptionText;

        [Header("Nút Đóng / Quay Lại")]
        [SerializeField] private GameObject backButton;

        [Header("Material Viền & Đổ Bóng Chữ (Drop Shadow / Outline)")]
        [SerializeField] private Material titleOutlineMaterial;

        [Header("Dữ Liệu Database")]
        [SerializeField] private LoadoutDatabase database;

        private SkillData currentDisplayedSkill;

        public SkillData CurrentSkill => currentDisplayedSkill;

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            EnsureComponents();

            // Khi mới vào game, ẩn panel này đi để không che vướng màn hình Loadout
            if (Application.isPlaying)
            {
                gameObject.SetActive(false);
            }
            else
            {
                DisplayDefaultSkill();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (!gameObject.activeInHierarchy) return;

            // Xử lý trực tiếp click vào nút Back trên panel
            if (TryGetPointerDown(out Vector2 screenPosition))
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector2 worldPoint = cam.ScreenToWorldPoint(screenPosition);
                    if (IsBackButtonClicked(worldPoint))
                    {
                        Hide();
                    }
                }
            }
        }

        // Mở panel chi tiết và hiển thị kỹ năng được bấm
        public void Show(SkillData skill)
        {
            if (skill == null) return;
            EnsureComponents();
            gameObject.SetActive(true);
            DisplaySkill(skill);
        }

        // Tắt panel chi tiết cho đỡ vướng
        public void Hide()
        {
            ButtonAudioManager.PlayClick();
            gameObject.SetActive(false);
        }

        // Kiểm tra vị trí click có trúng nút Back hay không
        public bool IsBackButtonClicked(Vector2 worldPoint)
        {
            if (backButton == null) return false;

            var col = backButton.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(worldPoint)) return true;

            var rend = backButton.GetComponent<SpriteRenderer>();
            if (rend != null)
            {
                Bounds b = rend.bounds;
                b.Expand(0.35f); // Bán kính bấm mở rộng để dễ chạm trên mobile/chuột
                if (b.Contains(new Vector3(worldPoint.x, worldPoint.y, backButton.transform.position.z)))
                {
                    return true;
                }
            }

            return Vector2.Distance(worldPoint, backButton.transform.position) <= 0.45f;
        }

        // Kiểm tra xử lý click từ bộ điều khiển chung (LoadoutSceneController)
        public bool TryHandlePointerClick(Vector2 worldPoint)
        {
            if (!gameObject.activeSelf) return false;

            // 1. Click trúng nút Back
            if (IsBackButtonClicked(worldPoint))
            {
                Hide();
                return true;
            }

            // 2. Click nằm trong khung nền SkillDetail -> chặn click xuyên xuống slot phía sau
            var bgRend = GetComponent<SpriteRenderer>();
            if (bgRend != null && bgRend.bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, transform.position.z)))
            {
                return true;
            }

            return false;
        }

        public void DisplaySkill(SkillData skill)
        {
            EnsureComponents();

            if (skill == null)
            {
                if (database != null && database.Skills != null && database.Skills.Count > 0)
                {
                    skill = database.Skills[0];
                }
            }

            if (skill == null) return;
            currentDisplayedSkill = skill;

            string skillId = !string.IsNullOrEmpty(skill.SkillId) ? skill.SkillId : skill.name;
            int level = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetLevel(skillId) : 1;
            int cards = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetCards(skillId) : 0;
            int requiredCards = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetRequiredCards(skillId) : 5;

            // 1. Đồng bộ ô thẻ kỹ năng đang chọn (selectedSkillSlot)
            if (selectedSkillSlot != null)
            {
                selectedSkillSlot.gameObject.SetActive(true);

                selectedSkillSlot.Setup(
                    skill,
                    level,
                    cards,
                    requiredCards,
                    database != null ? database.Frames : null,
                    database != null ? database.FontAsset : null
                );

                selectedSkillSlot.SetSortingOrder(8);
                selectedSkillSlot.SetMaskInteraction(SpriteMaskInteraction.None);
            }

            // Áp dụng Material viền & đổ bóng nếu có để chữ nổi bật, sắc nét
            if (titleOutlineMaterial != null)
            {
                if (rarityText != null && rarityText.fontSharedMaterial != titleOutlineMaterial)
                {
                    rarityText.fontSharedMaterial = titleOutlineMaterial;
                }
                if (skillNameText != null && skillNameText.fontSharedMaterial != titleOutlineMaterial)
                {
                    skillNameText.fontSharedMaterial = titleOutlineMaterial;
                }
                if (cooldownValueText != null && cooldownValueText.fontSharedMaterial != titleOutlineMaterial)
                {
                    cooldownValueText.fontSharedMaterial = titleOutlineMaterial;
                }
                if (typeText != null && typeText.fontSharedMaterial != titleOutlineMaterial)
                {
                    typeText.fontSharedMaterial = titleOutlineMaterial;
                }
            }

            // 2. Độ hiếm & Tên kỹ năng - Giữ nguyên toàn bộ phông chữ, cỡ chữ & cấu hình trong Inspector
            if (rarityText != null)
            {
                rarityText.text = skill.Rarity.ToString();
                rarityText.color = GetRarityColor(skill.Rarity);
                rarityText.sortingOrder = 10;
            }

            if (skillNameText != null)
            {
                skillNameText.text = !string.IsNullOrEmpty(skill.DisplayName) ? skill.DisplayName : skill.name;
                skillNameText.sortingOrder = 10;
            }

            // 3. Ô Cooldown
            if (cooldownLabelText != null)
            {
                cooldownLabelText.text = "Cooldown";
                cooldownLabelText.sortingOrder = 10;
            }

            if (cooldownValueText != null)
            {
                float cooldownValue = skill.Cooldown;
                cooldownValueText.text = $"{cooldownValue:0.#}s";
                cooldownValueText.sortingOrder = 10;
            }

            // 4. Ô Mô tả (Description Box)
            if (categoryLabelText != null)
            {
                categoryLabelText.text = "Ability";
                categoryLabelText.sortingOrder = 10;
            }

            if (typeText != null)
            {
                typeText.text = GetSkillCategoryName(skill);
                typeText.sortingOrder = 10;
            }

            if (descriptionText != null)
            {
                descriptionText.text = !string.IsNullOrEmpty(skill.Description)
                    ? skill.Description
                    : "No skill description available.";
                descriptionText.sortingOrder = 10;
            }
        }

        private void DisplayDefaultSkill()
        {
            SkillData initialSkill = null;
            var gridUI = FindFirstObjectByType<SkillCollectionGridUI>(FindObjectsInactive.Include);
            if (gridUI != null && gridUI.StagedSkill != null)
            {
                initialSkill = gridUI.StagedSkill;
            }

            if (initialSkill == null && PlayerLoadoutManager.HasInstance)
            {
                initialSkill = PlayerLoadoutManager.Instance.GetEquippedSkill(0);
            }

            if (initialSkill == null && database != null && database.Skills != null && database.Skills.Count > 0)
            {
                initialSkill = database.Skills[0];
            }

            DisplaySkill(initialSkill);
        }

        public static Color GetRarityColor(SkillRarity rarity)
        {
            switch (rarity)
            {
                case SkillRarity.Common:
                    return new Color(0.85f, 0.85f, 0.85f, 1f);
                case SkillRarity.Rare:
                    return new Color(0.29f, 0.64f, 1f, 1f); // Xanh dương
                case SkillRarity.Heroic:
                    return new Color(0.72f, 0.42f, 1f, 1f); // Tím
                case SkillRarity.Legendary:
                    return new Color(1f, 0.72f, 0.12f, 1f); // Vàng cam
                default:
                    return Color.white;
            }
        }

        public static string GetSkillCategoryName(SkillData skill)
        {
            if (skill == null) return "Ability";

            if (skill is SummonObject)
            {
                string lowerName = skill.name.ToLower();
                if (lowerName.Contains("turret") || lowerName.Contains("decoy") || lowerName.Contains("roller") || lowerName.Contains("healer"))
                {
                    return "Summon";
                }
                return "Magic";
            }

            if (skill is BuffSkill) return "Buff";
            if (skill is MoveSkill) return "Movement";
            if (skill is TimeStopSkill) return "Magic";

            return "Magic";
        }

        public void EnsureComponents()
        {
            if (database == null)
            {
                database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            }

            if (titleOutlineMaterial == null)
            {
                titleOutlineMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Drop Shadow");
            }

            // Tìm selectedSkillSlot
            if (selectedSkillSlot == null)
            {
                Transform slotT = transform.Find("grid_slot1");
                if (slotT == null) slotT = transform.Find("grid_slot");
                if (slotT != null)
                {
                    selectedSkillSlot = slotT.GetComponent<SkillCardSlotUI>();
                    if (selectedSkillSlot == null) selectedSkillSlot = slotT.gameObject.AddComponent<SkillCardSlotUI>();
                }
            }

            // Tìm CooldownBox
            if (cooldownBox == null)
            {
                Transform cbT = transform.Find("CooldownBox");
                if (cbT != null) cooldownBox = cbT.gameObject;
            }

            if (cooldownBox != null)
            {
                if (cooldownIconRenderer == null)
                {
                    Transform iconT = cooldownBox.transform.Find("Icon");
                    if (iconT != null) cooldownIconRenderer = iconT.GetComponent<SpriteRenderer>();
                }
                if (cooldownLabelText == null)
                {
                    Transform labelT = cooldownBox.transform.Find("Label");
                    if (labelT != null) cooldownLabelText = labelT.GetComponent<TextMeshPro>();
                }
                if (cooldownValueText == null)
                {
                    Transform valT = cooldownBox.transform.Find("Value");
                    if (valT != null) cooldownValueText = valT.GetComponent<TextMeshPro>();
                }
            }

            // Tìm DescriptionBox
            if (descriptionBox == null)
            {
                Transform dbT = transform.Find("DescriptionBox");
                if (dbT != null) descriptionBox = dbT.gameObject;
            }

            if (descriptionBox != null)
            {
                if (typeIconRenderer == null)
                {
                    Transform iconT = descriptionBox.transform.Find("TypeIcon");
                    if (iconT != null) typeIconRenderer = iconT.GetComponent<SpriteRenderer>();
                }
                if (categoryLabelText == null)
                {
                    Transform catT = descriptionBox.transform.Find("CategoryLabel");
                    if (catT != null) categoryLabelText = catT.GetComponent<TextMeshPro>();
                }
                if (typeText == null)
                {
                    Transform typeT = descriptionBox.transform.Find("TypeText");
                    if (typeT != null) typeText = typeT.GetComponent<TextMeshPro>();
                }
                if (descriptionText == null)
                {
                    Transform descT = descriptionBox.transform.Find("DescriptionText");
                    if (descT != null) descriptionText = descT.GetComponent<TextMeshPro>();
                }
            }

            // Tìm RarityText & SkillNameText
            if (rarityText == null)
            {
                Transform rT = transform.Find("RarityText");
                if (rT != null) rarityText = rT.GetComponent<TextMeshPro>();
            }

            if (skillNameText == null)
            {
                Transform nT = transform.Find("SkillNameText");
                if (nT != null) skillNameText = nT.GetComponent<TextMeshPro>();
            }

            // Tìm Back button
            if (backButton == null)
            {
                Transform bT = transform.Find("Back");
                if (bT != null) backButton = bT.gameObject;
            }

            if (backButton != null)
            {
                var col = backButton.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = backButton.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(0.5f, 0.5f);
                }
            }
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }
            return false;
        }
    }
}
