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
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text nameText;

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

            // 2. Độ hiếm (Rarity) và Tên cung (Name) - Cập nhật đồng bộ cả reference lẫn NameBow / Rare trong Stand
            var (foundName, foundRare) = FindStandLabels(transform);
            ApplyRarityAndName(foundName != null ? foundName : (nameText != null ? nameText.gameObject : null),
                               foundRare != null ? foundRare : (rarityText != null ? rarityText.gameObject : null),
                               bow);

            if (rarityText != null)
            {
                var visual = GetRarityVisual(bow.Rarity);
                rarityText.text = visual.label;
                rarityText.color = visual.color;
            }

            if (nameText != null)
            {
                nameText.text = bow.DisplayName;
                if (nameText.fontSize < 2f) nameText.fontSize = 2f;
                nameText.textWrappingMode = TextWrappingModes.NoWrap;
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

        public static (string label, Color color) GetRarityVisual(BowRarity rarity)
        {
            switch (rarity)
            {
                case BowRarity.Common:
                    // Màu sáng (Trắng sáng / bạc sáng)
                    return ("Common", new Color(0.95f, 0.95f, 0.95f, 1f));
                case BowRarity.Rare:
                    // Xanh nước biển (Cyan / Azure Blue)
                    return ("Rare", new Color(0.18f, 0.65f, 1f, 1f));
                case BowRarity.Heroic:
                    // Đỏ (Vibrant Red)
                    return ("Heroic", new Color(1f, 0.25f, 0.25f, 1f));
                case BowRarity.Legendary:
                    // Vàng (Gold / Bright Yellow)
                    return ("Legendary", new Color(1f, 0.82f, 0.1f, 1f));
                default:
                    return (rarity.ToString(), Color.white);
            }
        }

        public static Color GetRarityColor(BowRarity rarity) => GetRarityVisual(rarity).color;
        public static string GetRarityName(BowRarity rarity) => GetRarityVisual(rarity).label;

        public static void SetTextAndColor(GameObject targetObj, string text, Color color)
        {
            if (targetObj == null) return;

            // 1. TMP_Text (hỗ trợ cả TextMeshPro 3D lẫn TextMeshProUGUI UI)
            TMP_Text tmp = targetObj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = text;
                tmp.color = color;
                return;
            }

            // 2. UnityEngine.UI.Text
            UnityEngine.UI.Text uiText = targetObj.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (uiText != null)
            {
                uiText.text = text;
                uiText.color = color;
                return;
            }

            // 3. TextMesh (Legacy 3D Text)
            TextMesh tm = targetObj.GetComponentInChildren<TextMesh>(true);
            if (tm != null)
            {
                tm.text = text;
                tm.color = color;
                return;
            }

            // 4. SpriteRenderer nếu đối tượng có Sprite
            SpriteRenderer sr = targetObj.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                sr.color = color;
            }
        }

        public static (GameObject nameObj, GameObject rareObj) FindStandLabels(Transform standTransform)
        {
            if (standTransform == null) return (null, null);

            GameObject nameObj = null;
            GameObject rareObj = null;

            // 1. Tìm trực tiếp theo con cấp 1
            Transform nt = standTransform.Find("NameBow") ?? 
                           standTransform.Find("BowName") ?? 
                           standTransform.Find("BowNameText") ?? 
                           standTransform.Find("Name");
            if (nt != null) nameObj = nt.gameObject;

            Transform rt = standTransform.Find("Rare") ?? 
                           standTransform.Find("Rarity") ?? 
                           standTransform.Find("RarityText");
            if (rt != null) rareObj = rt.gameObject;

            // 2. Tìm đệ quy trong tất cả các con nếu chưa thấy
            if (nameObj == null || rareObj == null)
            {
                Transform[] allChildren = standTransform.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < allChildren.Length; i++)
                {
                    Transform c = allChildren[i];
                    if (c == standTransform) continue;
                    string n = c.name.ToLower();
                    if (nameObj == null && (n == "namebow" || n == "bowname" || n == "bownametext" || n == "name"))
                    {
                        nameObj = c.gameObject;
                    }
                    if (rareObj == null && (n == "rare" || n == "rarity" || n == "raritytext"))
                    {
                        rareObj = c.gameObject;
                    }
                }
            }

            return (nameObj, rareObj);
        }

        public static void ApplyRarityAndName(GameObject nameObj, GameObject rareObj, BowData bow)
        {
            if (bow == null) return;

            var visual = GetRarityVisual(bow.Rarity);

            if (nameObj != null)
            {
                SetTextAndColor(nameObj, bow.DisplayName, Color.white);
                TMP_Text tmp = nameObj.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                {
                    if (tmp.fontSize < 2f) tmp.fontSize = 2f;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            if (rareObj != null)
            {
                SetTextAndColor(rareObj, visual.label, visual.color);
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

            // 6. Rarity Text & Bow Name Text - Tự động tìm NameBow và Rare trong Stand
            if (rarityText == null || nameText == null)
            {
                var (foundName, foundRare) = FindStandLabels(transform);
                if (rarityText == null && foundRare != null)
                {
                    rarityText = foundRare.GetComponentInChildren<TMP_Text>(true);
                }
                if (nameText == null && foundName != null)
                {
                    nameText = foundName.GetComponentInChildren<TMP_Text>(true);
                }
            }

            if (rarityText == null)
            {
                Transform rt = transform.Find("RarityText");
                if (rt != null) rarityText = rt.GetComponent<TMP_Text>();
            }
            if (rarityText != null)
            {
                if (font != null && rarityText.font == null) rarityText.font = font;
                if (rarityText is TextMeshPro tmpR && tmpR.sortingOrder < 3) tmpR.sortingOrder = 5;
            }

            if (nameText == null)
            {
                Transform nt = transform.Find("BowNameText") ?? transform.Find("NameBow");
                if (nt != null) nameText = nt.GetComponent<TMP_Text>();
            }
            if (nameText != null)
            {
                if (font != null && nameText.font == null) nameText.font = font;
                if (nameText is TextMeshPro tmpN && tmpN.sortingOrder < 3) tmpN.sortingOrder = 5;
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
