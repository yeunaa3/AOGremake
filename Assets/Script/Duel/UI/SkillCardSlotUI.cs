using System;
using TMPro;
using UnityEngine;

namespace AOG.Duel
{
    // Quản lý hiển thị cho 1 slot kỹ năng.
    // Dùng cho cả 5 slot trang bị ở trên và danh sách chiêu thức trong lưới ở dưới.
    [DisallowMultipleComponent]
    public sealed class SkillCardSlotUI : MonoBehaviour
    {
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer frameSkillRenderer;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer frameLevelRenderer;
        [SerializeField] private SpriteRenderer fillRenderer;
        [SerializeField] private SpriteRenderer levelBadgeRenderer;
        [SerializeField] private SpriteRenderer selectionRenderer;

        [Header("Card Base Sprites")]
        [SerializeField] private Sprite cardBaseSprite;
        [SerializeField] private Sprite emptyPlaceholderSprite;

        [Header("Text (TextMeshPro)")]
        [SerializeField] private TextMeshPro levelText;
        [SerializeField] private TextMeshPro progressText;

        [Header("Collider")]
        [SerializeField] private BoxCollider2D clickCollider;

        private SkillData currentSkill;
        private BowData currentBow;
        private int slotIndex = -1;
        private bool isSelected;
        private TMP_FontAsset defaultFont;

        private const float DefaultFillWidth = 1.05f;
        private const float DefaultFillHeight = 1.4f;
        private const float FillLeftEdge = -0.525f;

        public SkillData Skill => currentSkill;
        public BowData Bow => currentBow;
        public int SlotIndex => slotIndex;
        public bool IsSelected => isSelected;
        public Sprite BaseSprite => baseRenderer != null ? baseRenderer.sprite : null;
        public Sprite CardBaseSprite { get => cardBaseSprite; set => cardBaseSprite = value; }
        public Sprite EmptyPlaceholderSprite { get => emptyPlaceholderSprite; set => emptyPlaceholderSprite = value; }

        public void SetSprites(Sprite cardBase, Sprite emptyPlaceholder)
        {
            if (cardBase != null) cardBaseSprite = cardBase;
            if (emptyPlaceholder != null) emptyPlaceholderSprite = emptyPlaceholder;
        }

        public event Action<SkillCardSlotUI> Clicked;

        private void Awake()
        {
            EnsureHierarchyAndComponents();
            if (cardBaseSprite == null && baseRenderer != null)
            {
                cardBaseSprite = baseRenderer.sprite;
            }
            if (emptyPlaceholderSprite == null && baseRenderer != null)
            {
                emptyPlaceholderSprite = baseRenderer.sprite;
            }
        }

        public void InitializeSlot(int index, TMP_FontAsset font)
        {
            slotIndex = index;
            if (font != null) defaultFont = font;
            EnsureHierarchyAndComponents();
        }

        public void Setup(SkillData skill, int level, int cards, int requiredCards, SkillFrames frames, TMP_FontAsset font)
        {
            currentSkill = skill;
            currentBow = null;
            if (font != null) defaultFont = font;
            EnsureHierarchyAndComponents();
            if (defaultFont != null)
            {
                if (levelText != null && levelText.font == null) levelText.font = defaultFont;
                if (progressText != null && progressText.font == null) progressText.font = defaultFont;
            }

            if (skill != null)
            {
                // 0. Base card sprite (Chuyển sang nền thẻ chiêu thức chuẩn)
                if (baseRenderer != null)
                {
                    if (cardBaseSprite != null) baseRenderer.sprite = cardBaseSprite;
                    baseRenderer.gameObject.SetActive(true);
                }

                // 1. Frame Skill (Khung theo độ hiếm)
                if (frameSkillRenderer != null)
                {
                    frameSkillRenderer.gameObject.SetActive(true);
                    if (frames != null)
                    {
                        Sprite frameSprite = frames.Get(skill.Rarity);
                        if (frameSprite != null) frameSkillRenderer.sprite = frameSprite;
                    }
                }

                // 2. Icon (Hình chiêu thức - nằm đè lên trên Frame skill như trong trận)
                if (iconRenderer != null)
                {
                    iconRenderer.gameObject.SetActive(true);
                    iconRenderer.sprite = skill.Icon;
                    FitIconToFrame(skill.Icon);
                }

                // 3. Level Badge & Text
                if (levelBadgeRenderer != null) levelBadgeRenderer.gameObject.SetActive(true);
                if (levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    levelText.text = level.ToString();
                }

                // 4. Fill Bar & Text tiến độ
                if (frameLevelRenderer != null) frameLevelRenderer.gameObject.SetActive(true);

                bool isMax = level >= PlayerSkillInventory.MaxLevel || requiredCards <= 0;
                if (isMax)
                {
                    if (fillRenderer != null)
                    {
                        fillRenderer.gameObject.SetActive(true);
                        SetFillAmount(1f);
                    }
                    if (progressText != null)
                    {
                        progressText.gameObject.SetActive(true);
                        progressText.text = "MAX";
                    }
                }
                else
                {
                    // Khi ban đầu chưa có thẻ (cards == 0) thì ẩn hẳn thanh fill
                    bool hasCards = cards > 0;
                    if (fillRenderer != null)
                    {
                        fillRenderer.gameObject.SetActive(hasCards);
                        if (hasCards)
                        {
                            float ratio = requiredCards > 0 ? Mathf.Clamp01((float)cards / requiredCards) : 0f;
                            SetFillAmount(ratio);
                        }
                    }

                    // Text số thẻ trên khung (ví dụ: "0/2" hoặc "2/3")
                    if (progressText != null)
                    {
                        progressText.gameObject.SetActive(true);
                        progressText.text = $"{cards}/{requiredCards}";
                    }
                }
            }
            else
            {
                Clear();
            }
        }

        public void SetupBow(BowData bow, int level, int cards, int requiredCards, BowFrames bowFrames, SkillFrames fallbackFrames, TMP_FontAsset font)
        {
            currentSkill = null;
            currentBow = bow;
            if (font != null) defaultFont = font;
            EnsureHierarchyAndComponents();
            if (defaultFont != null)
            {
                if (levelText != null && levelText.font == null) levelText.font = defaultFont;
                if (progressText != null && progressText.font == null) progressText.font = defaultFont;
            }

            if (bow != null)
            {
                // 0. Base card sprite
                if (baseRenderer != null)
                {
                    if (cardBaseSprite != null) baseRenderer.sprite = cardBaseSprite;
                    baseRenderer.gameObject.SetActive(true);
                }

                // 1. Frame Bow (Khung theo độ hiếm lấy từ BowFrames hoặc fallback SkillFrames)
                if (frameSkillRenderer != null)
                {
                    frameSkillRenderer.gameObject.SetActive(true);
                    Sprite frameSprite = null;
                    if (bowFrames != null) frameSprite = bowFrames.Get(bow.Rarity, fallbackFrames);
                    else if (fallbackFrames != null) frameSprite = fallbackFrames.Get((SkillRarity)bow.Rarity);

                    if (frameSprite != null) frameSkillRenderer.sprite = frameSprite;
                }

                // 2. Icon Cung
                if (iconRenderer != null)
                {
                    iconRenderer.gameObject.SetActive(true);
                    Sprite spr = bow.BowSprite != null ? bow.BowSprite : bow.Icon;
                    iconRenderer.sprite = spr;
                    FitIconToFrame(spr);
                }

                // 3. Level Badge & Text (y hệt ô kỹ năng)
                if (levelBadgeRenderer != null) levelBadgeRenderer.gameObject.SetActive(true);
                if (levelText != null)
                {
                    levelText.gameObject.SetActive(true);
                    levelText.text = level.ToString();
                }

                // 4. Fill Bar & Text tiến độ lượng thẻ hiện tại (y hệt ô kỹ năng)
                if (frameLevelRenderer != null) frameLevelRenderer.gameObject.SetActive(true);

                bool isMax = level >= PlayerSkillInventory.MaxLevel || requiredCards <= 0;
                if (isMax)
                {
                    if (fillRenderer != null)
                    {
                        fillRenderer.gameObject.SetActive(true);
                        SetFillAmount(1f);
                    }
                    if (progressText != null)
                    {
                        progressText.gameObject.SetActive(true);
                        progressText.text = "MAX";
                    }
                }
                else
                {
                    // Khi ban đầu chưa có thẻ (cards == 0) thì ẩn hẳn thanh fill
                    bool hasCards = cards > 0;
                    if (fillRenderer != null)
                    {
                        fillRenderer.gameObject.SetActive(hasCards);
                        if (hasCards)
                        {
                            float ratio = requiredCards > 0 ? Mathf.Clamp01((float)cards / requiredCards) : 0f;
                            SetFillAmount(ratio);
                        }
                    }

                    // Text số thẻ trên khung (ví dụ: "0/2" hoặc "2/3")
                    if (progressText != null)
                    {
                        progressText.gameObject.SetActive(true);
                        progressText.text = $"{cards}/{requiredCards}";
                    }
                }
            }
            else
            {
                Clear();
            }
        }

        public void SetupBow(BowData bow, BowFrames bowFrames, SkillFrames fallbackFrames, TMP_FontAsset font)
        {
            string id = (bow != null && !string.IsNullOrEmpty(bow.BowId)) ? bow.BowId : (bow != null ? bow.name : string.Empty);
            int level = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetLevel(id) : 1;
            int cards = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetCards(id) : 0;
            int required = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetRequiredCards(id) : 2;
            SetupBow(bow, level, cards, required, bowFrames, fallbackFrames, font);
        }

        // Xóa hiển thị kỹ năng (dùng khi slot trống hoặc SelectPlace chưa chọn chiêu nào)
        public void Clear()
        {
            currentSkill = null;
            currentBow = null;
            if (baseRenderer != null)
            {
                if (emptyPlaceholderSprite != null) baseRenderer.sprite = emptyPlaceholderSprite;
                baseRenderer.color = Color.white;
                baseRenderer.enabled = true;
            }
            if (iconRenderer != null)
            {
                iconRenderer.sprite = null;
                iconRenderer.gameObject.SetActive(false);
            }
            if (frameSkillRenderer != null)
            {
                frameSkillRenderer.gameObject.SetActive(false);
            }
            if (frameLevelRenderer != null)
            {
                frameLevelRenderer.gameObject.SetActive(false);
            }
            if (fillRenderer != null)
            {
                fillRenderer.gameObject.SetActive(false);
            }
            if (levelBadgeRenderer != null)
            {
                levelBadgeRenderer.gameObject.SetActive(false);
            }
            if (levelText != null)
            {
                levelText.text = string.Empty;
                levelText.gameObject.SetActive(false);
            }
            if (progressText != null)
            {
                progressText.text = string.Empty;
                progressText.gameObject.SetActive(false);
            }
            if (selectionRenderer != null)
            {
                selectionRenderer.gameObject.SetActive(false);
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectionRenderer != null)
            {
                selectionRenderer.gameObject.SetActive(selected);
            }
            else if (baseRenderer != null)
            {
                baseRenderer.color = selected ? new Color(1f, 1f, 0.65f, 1f) : Color.white;
            }
        }

        public void SetMaskInteraction(SpriteMaskInteraction interaction)
        {
            EnsureHierarchyAndComponents();
            if (baseRenderer != null) baseRenderer.maskInteraction = interaction;
            if (frameSkillRenderer != null) frameSkillRenderer.maskInteraction = interaction;
            if (iconRenderer != null) iconRenderer.maskInteraction = interaction;
            if (frameLevelRenderer != null) frameLevelRenderer.maskInteraction = interaction;
            if (fillRenderer != null) fillRenderer.maskInteraction = interaction;
            if (levelBadgeRenderer != null) levelBadgeRenderer.maskInteraction = interaction;
            if (selectionRenderer != null) selectionRenderer.maskInteraction = interaction;
        }

        public void SetTextVisibility(bool visible)
        {
            if (levelText != null && (currentSkill != null || currentBow != null)) levelText.enabled = visible;
            if (progressText != null && (currentSkill != null || currentBow != null)) progressText.enabled = visible;
        }

        public bool ContainsPoint(Vector2 worldPoint)
        {
            // 1. Kiểm tra theo khoảng cách 2D trực tiếp từ tâm slot (card size chuẩn ~1.1f x 1.8f theo lossyScale)
            Vector2 pos2D = transform.position;
            Vector2 diff = worldPoint - pos2D;
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            float scaleY = Mathf.Abs(transform.lossyScale.y);
            float halfW = Mathf.Max(0.4f, 0.65f * scaleX);
            float halfH = Mathf.Max(0.6f, 1.0f * scaleY);

            if (Mathf.Abs(diff.x) <= halfW && Mathf.Abs(diff.y) <= halfH)
            {
                return true;
            }

            // 2. Fallback collider và renderers
            if (clickCollider != null && clickCollider.OverlapPoint(worldPoint)) return true;

            Vector3 pt3 = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
            if (baseRenderer != null && baseRenderer.bounds.Contains(pt3)) return true;
            if (frameSkillRenderer != null && frameSkillRenderer.bounds.Contains(pt3)) return true;

            return false;
        }

        public void OnSlotClicked()
        {
            Clicked?.Invoke(this);
        }

        private void SetFillAmount(float ratio)
        {
            if (fillRenderer == null) return;

            ratio = Mathf.Clamp01(ratio);
            Transform fillT = fillRenderer.transform;
            float targetWidth = DefaultFillWidth * ratio;
            float posX = FillLeftEdge + targetWidth * 0.5f;

            fillT.localPosition = new Vector3(posX, 0f, 0f);
            fillT.localScale = new Vector3(targetWidth, DefaultFillHeight, 1f);
        }

        private void FitIconToFrame(Sprite icon)
        {
            if (iconRenderer == null || icon == null) return;

            float iconW = icon.bounds.size.x;
            float iconH = icon.bounds.size.y;
            float maxDim = Mathf.Max(iconW, iconH);
            if (maxDim > 0.001f)
            {
                float targetSize = 0.65f;
                float scale = targetSize / maxDim;
                iconRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
            iconRenderer.transform.localPosition = Vector3.zero;
        }

        public void EnsureHierarchyAndComponents()
        {
            if (baseRenderer == null)
            {
                baseRenderer = GetComponent<SpriteRenderer>();
                if (baseRenderer != null) baseRenderer.sortingOrder = 2;
            }
            if (cardBaseSprite == null && baseRenderer != null && baseRenderer.sprite != null)
            {
                cardBaseSprite = baseRenderer.sprite;
            }
            if (emptyPlaceholderSprite == null && baseRenderer != null && baseRenderer.sprite != null)
            {
                emptyPlaceholderSprite = baseRenderer.sprite;
            }

            // 1. Frame skill
            Transform frameSkillT = transform.Find("Frame skill");
            if (frameSkillT != null)
            {
                if (frameSkillRenderer == null) frameSkillRenderer = frameSkillT.GetComponent<SpriteRenderer>();
                if (frameSkillRenderer != null) frameSkillRenderer.sortingOrder = 3;

                // Icon under Frame skill (sortingOrder: 4, đè lên trên Frame skill)
                Transform iconT = frameSkillT.Find("Icon");
                if (iconT == null)
                {
                    GameObject iconGo = new GameObject("Icon");
                    iconT = iconGo.transform;
                    iconT.SetParent(frameSkillT, false);
                    iconT.localPosition = Vector3.zero;
                    iconT.localScale = new Vector3(0.65f, 0.65f, 1f);
                }
                if (iconRenderer == null)
                {
                    iconRenderer = iconT.GetComponent<SpriteRenderer>();
                    if (iconRenderer == null) iconRenderer = iconT.gameObject.AddComponent<SpriteRenderer>();
                }
                if (iconRenderer != null) iconRenderer.sortingOrder = 4;
            }

            // 2. Frame (Khung thanh cấp độ & tiến độ)
            Transform frameLevelT = transform.Find("Frame");
            if (frameLevelT != null)
            {
                if (frameLevelRenderer == null) frameLevelRenderer = frameLevelT.GetComponent<SpriteRenderer>();
                if (frameLevelRenderer != null) frameLevelRenderer.sortingOrder = 3;

                // Fill
                Transform fillT = frameLevelT.Find("Fill");
                if (fillT != null)
                {
                    if (fillRenderer == null) fillRenderer = fillT.GetComponent<SpriteRenderer>();
                    if (fillRenderer != null) fillRenderer.sortingOrder = 4;
                }

                // ProgressText (trực tiếp dưới Frame để không bị ẩn khi Fill tắt)
                Transform ptT = frameLevelT.Find("ProgressText");
                if (ptT == null && fillT != null) ptT = fillT.Find("ProgressText");
                if (ptT == null)
                {
                    GameObject ptGo = new GameObject("ProgressText");
                    ptT = ptGo.transform;
                    ptT.SetParent(frameLevelT, false);
                    ptT.localPosition = new Vector3(0.06f, 0f, 0f);
                }
                if (progressText == null) progressText = ptT.GetComponent<TextMeshPro>();
                if (progressText == null)
                {
                    progressText = ptT.gameObject.AddComponent<TextMeshPro>();
                }
                ConfigureProgressText(progressText);

                // Level Badge
                Transform levelT = frameLevelT.Find("Level");
                if (levelT != null)
                {
                    if (levelBadgeRenderer == null) levelBadgeRenderer = levelT.GetComponent<SpriteRenderer>();
                    if (levelBadgeRenderer != null) levelBadgeRenderer.sortingOrder = 5;

                    // LevelText trên Level Badge
                    Transform ltT = levelT.Find("LevelText");
                    if (ltT == null)
                    {
                        GameObject ltGo = new GameObject("LevelText");
                        ltT = ltGo.transform;
                        ltT.SetParent(levelT, false);
                        ltT.localPosition = Vector3.zero;
                    }
                    if (levelText == null) levelText = ltT.GetComponent<TextMeshPro>();
                    if (levelText == null)
                    {
                        levelText = ltT.gameObject.AddComponent<TextMeshPro>();
                    }
                    ConfigureLevelText(levelText);
                }
            }

            // Selection highlight
            Transform selT = transform.Find("SelectionHighlight");
            if (selT != null && selectionRenderer == null)
            {
                selectionRenderer = selT.GetComponent<SpriteRenderer>();
            }

            // Click Collider
            if (clickCollider == null)
            {
                clickCollider = GetComponent<BoxCollider2D>();
                if (clickCollider == null)
                {
                    clickCollider = gameObject.AddComponent<BoxCollider2D>();
                    clickCollider.size = new Vector2(0.9f, 1.62f);
                }
            }
        }

        private void ConfigureProgressText(TextMeshPro tmp)
        {
            if (tmp == null) return;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.fontSize = 1.0f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 6;
            tmp.rectTransform.sizeDelta = new Vector2(0.7f, 0.25f);
            tmp.rectTransform.localPosition = new Vector3(0.06f, 0f, 0f);
        }

        private void ConfigureLevelText(TextMeshPro tmp)
        {
            if (tmp == null) return;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.fontSize = 1.0f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 6;
            tmp.rectTransform.sizeDelta = new Vector2(0.25f, 0.25f);
            tmp.rectTransform.localPosition = Vector3.zero;
        }
    }
}
