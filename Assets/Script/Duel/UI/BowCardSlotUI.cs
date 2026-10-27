using System;
using TMPro;
using UnityEngine;

namespace AOG.Duel
{
    // Quản lý hiển thị cho 1 ô thẻ Cung (Bow Card Slot).
    // Tương tự SkillCardSlotUI: hiển thị nền thẻ, khung viền theo độ hiếm (BowFrames), ảnh cung và thông số.
    [DisallowMultipleComponent]
    public sealed class BowCardSlotUI : MonoBehaviour
    {
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer frameRenderer;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer selectionRenderer;

        [Header("Card Base Sprites")]
        [SerializeField] private Sprite cardBaseSprite;
        [SerializeField] private Sprite emptyPlaceholderSprite;

        [Header("Text (TextMeshPro)")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private TextMeshPro statsText;
        [SerializeField] private TextMeshPro passiveText;

        [Header("Collider")]
        [SerializeField] private BoxCollider2D clickCollider;

        private BowData currentBow;
        private int slotIndex = -1;
        private bool isSelected;
        private TMP_FontAsset defaultFont;

        public BowData Bow => currentBow;
        public int SlotIndex => slotIndex;
        public bool IsSelected => isSelected;
        public Sprite BaseSprite => baseRenderer != null ? baseRenderer.sprite : null;
        public Sprite CardBaseSprite { get => cardBaseSprite; set => cardBaseSprite = value; }
        public Sprite EmptyPlaceholderSprite { get => emptyPlaceholderSprite; set => emptyPlaceholderSprite = value; }

        public event Action<BowCardSlotUI> Clicked;

        public void OnSlotClicked()
        {
            Clicked?.Invoke(this);
        }

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

        public void Setup(BowData bow, BowFrames frames, TMP_FontAsset font)
        {
            currentBow = bow;
            if (font != null) defaultFont = font;
            EnsureHierarchyAndComponents();

            if (bow != null)
            {
                // 0. Base card sprite
                if (baseRenderer != null)
                {
                    if (cardBaseSprite != null) baseRenderer.sprite = cardBaseSprite;
                    baseRenderer.gameObject.SetActive(true);
                }

                // 1. Frame Bow (Khung theo độ hiếm lấy từ BowFrames tương tự tab Skill)
                if (frameRenderer != null)
                {
                    frameRenderer.gameObject.SetActive(true);
                    if (frames != null)
                    {
                        Sprite frameSprite = frames.Get(bow.Rarity);
                        if (frameSprite != null) frameRenderer.sprite = frameSprite;
                    }
                }

                // 2. Icon Cung
                if (iconRenderer != null)
                {
                    iconRenderer.gameObject.SetActive(true);
                    Sprite spr = bow.BowSprite != null ? bow.BowSprite : bow.Icon;
                    iconRenderer.sprite = spr;
                    FitIconToFrame(spr);
                }

                // 3. Name Text
                if (nameText != null)
                {
                    nameText.gameObject.SetActive(true);
                    nameText.text = bow.DisplayName;
                }

                // 4. Stats Text
                if (statsText != null)
                {
                    statsText.gameObject.SetActive(true);
                    statsText.text = $"ATK: {bow.Damage} | SPD: {bow.AttackSpeedMultiplier:0.##}x";
                }

                // 5. Passive Text
                if (passiveText != null)
                {
                    passiveText.gameObject.SetActive(true);
                    passiveText.text = bow.Passive != null ? bow.Passive.Description : string.Empty;
                }
            }
            else
            {
                Clear();
            }
        }

        public void Clear()
        {
            currentBow = null;
            if (baseRenderer != null && emptyPlaceholderSprite != null)
            {
                baseRenderer.sprite = emptyPlaceholderSprite;
                baseRenderer.gameObject.SetActive(true);
            }
            if (iconRenderer != null)
            {
                iconRenderer.sprite = null;
                iconRenderer.gameObject.SetActive(false);
            }
            if (frameRenderer != null)
            {
                frameRenderer.gameObject.SetActive(false);
            }
            if (nameText != null)
            {
                nameText.text = string.Empty;
                nameText.gameObject.SetActive(false);
            }
            if (statsText != null)
            {
                statsText.text = string.Empty;
                statsText.gameObject.SetActive(false);
            }
            if (passiveText != null)
            {
                passiveText.text = string.Empty;
                passiveText.gameObject.SetActive(false);
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
        }

        public bool ContainsPoint(Vector2 worldPoint)
        {
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

            if (clickCollider != null)
            {
                return clickCollider.OverlapPoint(worldPoint);
            }

            return false;
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

            if (frameRenderer == null)
            {
                Transform t = transform.Find("Frame") ?? transform.Find("Frame bow") ?? transform.Find("Frame skill");
                if (t != null) frameRenderer = t.GetComponent<SpriteRenderer>();
            }

            if (iconRenderer == null)
            {
                Transform t = transform.Find("Icon");
                if (t != null) iconRenderer = t.GetComponent<SpriteRenderer>();
            }

            if (selectionRenderer == null)
            {
                Transform t = transform.Find("Selection");
                if (t != null) selectionRenderer = t.GetComponent<SpriteRenderer>();
            }

            if (clickCollider == null)
            {
                clickCollider = GetComponent<BoxCollider2D>();
                if (clickCollider == null)
                {
                    clickCollider = gameObject.AddComponent<BoxCollider2D>();
                    clickCollider.size = new Vector2(1.2f, 1.8f);
                    clickCollider.isTrigger = true;
                }
            }
        }
    }
}
