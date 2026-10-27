using System;
using TMPro;
using UnityEngine;

namespace AOG.Duel
{
    // Hiển thị thông tin chi tiết của cây cung đang chọn/cầm trên bệ Stand:
    // - Card Slot preview cây cung (grid_slot)
    // - Độ hiếm (Rarity) & Tên cung (DisplayName)
    // - 3 ô thông số: Damage, Draw Speed, Arrow Speed
    // - 1 ô Ability (Nội tại / Kỹ năng của cung)
    // LƯU Ý: Giữ nguyên 100% vị trí, kích thước và lề của các chữ theo cấu hình của người dùng trong Scene.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BowInfoDisplayUI : MonoBehaviour
    {
        [Header("Card Slot Preview")]
        [SerializeField] private SkillCardSlotUI equippedBowSlot;

        [Header("Tiêu Đề Cung")]
        [SerializeField] private TextMeshPro rarityText;
        [SerializeField] private TextMeshPro nameText;

        [Header("3 Ô Thông Số")]
        [SerializeField] private GameObject damageBox;
        [SerializeField] private TextMeshPro damageLabelText;
        [SerializeField] private TextMeshPro damageValueText;

        [SerializeField] private GameObject drawSpeedBox;
        [SerializeField] private TextMeshPro drawSpeedLabelText;
        [SerializeField] private TextMeshPro drawSpeedValueText;

        [SerializeField] private GameObject arrowSpeedBox;
        [SerializeField] private TextMeshPro arrowSpeedLabelText;
        [SerializeField] private TextMeshPro arrowSpeedValueText;

        [Header("Ô Ability / Nội Tại")]
        [SerializeField] private GameObject abilityBox;
        [SerializeField] private TextMeshPro abilityLabelText;
        [SerializeField] private TextMeshPro abilityNameText;

        [Header("Dữ Liệu Database")]
        [SerializeField] private LoadoutDatabase database;

        private BowData currentDisplayedBow;

        public BowData CurrentBow => currentDisplayedBow;

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            EnsureComponents();
            var mgr = PlayerLoadoutManager.Instance;
            BowData bowToDisplay = mgr != null ? mgr.EquippedBow : null;
            DisplayBow(bowToDisplay);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureComponents();
            }
        }
#endif

        public void DisplayBow(BowData bow)
        {
            EnsureComponents();

            if (bow == null)
            {
                if (database != null && database.Bows != null && database.Bows.Count > 0)
                {
                    bow = database.Bows[0];
                }
            }

            if (bow == null) return;
            currentDisplayedBow = bow;

            // 1. Hiển thị ô thẻ Cung Preview (grid_slot)
            if (equippedBowSlot != null)
            {
                equippedBowSlot.gameObject.SetActive(true);
                equippedBowSlot.SetMaskInteraction(SpriteMaskInteraction.None);

                string id = !string.IsNullOrEmpty(bow.BowId) ? bow.BowId : bow.name;
                var inv = PlayerSkillInventory.Instance;
                int level = inv != null ? inv.GetLevel(id) : 1;
                int cards = inv != null ? inv.GetCards(id) : 0;
                int required = inv != null ? inv.GetRequiredCards(id) : 2;

                equippedBowSlot.SetupBow(
                    bow, level, cards, required,
                    database != null ? database.BowFrames : null,
                    database != null ? database.Frames : null,
                    database != null ? database.FontAsset : null
                );
                equippedBowSlot.SetMaskInteraction(SpriteMaskInteraction.None);
            }

            // 2. Độ hiếm (Rarity) và Tên cung (Name) - Giữ nguyên vị trí người dùng đã chỉnh trong Scene
            if (rarityText != null)
            {
                rarityText.text = bow.Rarity.ToString().ToUpper();
                rarityText.color = GetRarityColor(bow.Rarity);
            }

            if (nameText != null)
            {
                nameText.text = bow.DisplayName;
            }

            // 3. Ô 1: Sát thương (Damage) - Chỉ cập nhật nội dung số, giữ nguyên vị trí của người dùng
            if (damageValueText != null)
            {
                damageValueText.text = bow.Damage.ToString();
            }

            // 4. Ô 2: Tốc độ đánh / kéo cung (Draw Speed)
            if (drawSpeedValueText != null)
            {
                drawSpeedValueText.text = bow.AttackSpeedMultiplier.ToString("0.#") + "x";
            }

            // 5. Ô 3: Tốc độ bay của tên (Arrow Speed)
            if (arrowSpeedValueText != null)
            {
                arrowSpeedValueText.text = bow.ArrowSpeed.ToString("0.#");
            }

            // 6. Ô 4: Ability / Nội tại của cung
            if (abilityNameText != null)
            {
                if (bow.Passive != null && !string.IsNullOrEmpty(bow.Passive.PassiveName))
                {
                    abilityNameText.text = bow.Passive.PassiveName;
                }
                else if (bow.Passive != null)
                {
                    string pName = bow.Passive.GetType().Name.Replace("Passive", "");
                    abilityNameText.text = pName;
                }
                else
                {
                    abilityNameText.text = "None";
                }
            }
        }

        public static Color GetRarityColor(BowRarity rarity)
        {
            switch (rarity)
            {
                case BowRarity.Common:
                    return new Color(0.85f, 0.85f, 0.85f, 1f); // Xám bạc
                case BowRarity.Rare:
                    return new Color(0.3f, 0.65f, 1f, 1f); // Xanh dương
                case BowRarity.Heroic:
                    return new Color(0.75f, 0.4f, 1f, 1f); // Tím
                case BowRarity.Legendary:
                    return new Color(1f, 0.8f, 0.2f, 1f); // Vàng kim (Gold)
                default:
                    return Color.white;
            }
        }

        public void EnsureComponents()
        {
            if (database == null)
            {
                database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            }

            TMP_FontAsset font = database != null ? database.FontAsset : null;

            // 1. Card slot (grid_slot)
            if (equippedBowSlot == null)
            {
                Transform slotT = transform.Find("grid_slot");
                if (slotT != null)
                {
                    equippedBowSlot = slotT.GetComponent<SkillCardSlotUI>();
                    if (equippedBowSlot == null) equippedBowSlot = slotT.gameObject.AddComponent<SkillCardSlotUI>();
                }
            }
            if (equippedBowSlot != null)
            {
                equippedBowSlot.SetMaskInteraction(SpriteMaskInteraction.None);
            }

            // 2. Damage Box (DameText)
            if (damageBox == null)
            {
                Transform dt = transform.Find("DameText");
                if (dt != null) damageBox = dt.gameObject;
            }
            if (damageBox != null)
            {
                EnsureBoxTexts(damageBox.transform, ref damageLabelText, ref damageValueText, font);
            }

            // 3. Draw Speed Box
            if (drawSpeedBox == null)
            {
                Transform dsb = transform.Find("DrawSpeedBox");
                if (dsb != null) drawSpeedBox = dsb.gameObject;
            }
            if (drawSpeedBox != null)
            {
                EnsureBoxTexts(drawSpeedBox.transform, ref drawSpeedLabelText, ref drawSpeedValueText, font);
            }

            // 4. Arrow Speed Box
            if (arrowSpeedBox == null)
            {
                Transform asb = transform.Find("ArrowSpeedBox");
                if (asb != null) arrowSpeedBox = asb.gameObject;
            }
            if (arrowSpeedBox != null)
            {
                EnsureBoxTexts(arrowSpeedBox.transform, ref arrowSpeedLabelText, ref arrowSpeedValueText, font);
            }

            // 5. Ability Box
            if (abilityBox == null)
            {
                Transform ab = transform.Find("AbilityBox");
                if (ab != null) abilityBox = ab.gameObject;
            }
            if (abilityBox != null)
            {
                EnsureBoxTexts(abilityBox.transform, ref abilityLabelText, ref abilityNameText, font);
            }

            // 6. Rarity Text - Giữ nguyên 100% Transform do người dùng căn chỉnh
            if (rarityText == null)
            {
                Transform rt = transform.Find("RarityText");
                if (rt != null) rarityText = rt.GetComponent<TextMeshPro>();
            }
            if (rarityText != null)
            {
                if (font != null && rarityText.font == null) rarityText.font = font;
                if (rarityText.sortingOrder < 3) rarityText.sortingOrder = 5;
            }

            // 7. Bow Name Text - Giữ nguyên 100% Transform do người dùng căn chỉnh
            if (nameText == null)
            {
                Transform nt = transform.Find("BowNameText");
                if (nt != null) nameText = nt.GetComponent<TextMeshPro>();
            }
            if (nameText != null)
            {
                if (font != null && nameText.font == null) nameText.font = font;
                if (nameText.sortingOrder < 3) nameText.sortingOrder = 5;
            }
        }

        private void EnsureBoxTexts(Transform boxT, ref TextMeshPro labelTmp, ref TextMeshPro valueTmp, TMP_FontAsset font)
        {
            if (labelTmp == null)
            {
                Transform lT = boxT.Find("Label");
                if (lT != null) labelTmp = lT.GetComponent<TextMeshPro>();
            }
            if (labelTmp != null)
            {
                if (font != null && labelTmp.font == null) labelTmp.font = font;
                if (labelTmp.sortingOrder < 3) labelTmp.sortingOrder = 5;
            }

            if (valueTmp == null)
            {
                Transform vT = boxT.Find("Value");
                if (vT != null) valueTmp = vT.GetComponent<TextMeshPro>();
            }
            if (valueTmp != null)
            {
                if (font != null && valueTmp.font == null) valueTmp.font = font;
                if (valueTmp.sortingOrder < 3) valueTmp.sortingOrder = 5;
            }
        }
    }
}
