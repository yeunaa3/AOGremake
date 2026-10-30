using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    // Quản lý hiển thị các slot chiêu thức và cung tên trong vùng SwitchTab/BG.
    // Sử dụng các GameObject slot đã được tạo sẵn trong Scene, chỉ việc gán ảnh và text.
    // Hỗ trợ cuộn chuột và vuốt kéo màn hình, giới hạn hiển thị trong vùng BG.
    [ExecuteAlways]
    public sealed class SkillCollectionGridUI : MonoBehaviour
    {
        public enum GridMode { Skill, Bow }

        [Header("Dữ Liệu & Tham Chiếu")]
        [SerializeField] private LoadoutDatabase database;
        [SerializeField] private SpriteRenderer bgRenderer;
        [SerializeField] private LoadedSkillPanelUI loadedSkillPanelUI;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private SkillCardSlotUI selectPlaceSlot;

        [Header("Sap Xep Do Hiem (Rarity Sort)")]
        [SerializeField] private GameObject ascDecButton;
        [SerializeField] private TextMeshPro ascDecText;
        [SerializeField] private bool isAscending = true;

        public bool IsAscending => isAscending;

        [Header("Danh Sách Slot Trong Grid (Có sẵn trong Scene)")]
        [SerializeField] private List<SkillCardSlotUI> gridSlots = new List<SkillCardSlotUI>();

        [Header("Cấu Hình Cuộn")]
        [SerializeField] private float scrollSensitivity = 0.003f;
        [SerializeField] private float dragSensitivity = 1f;
        [SerializeField] private float maxScrollY = 0f;

        private GridMode currentMode = GridMode.Skill;
        public GridMode CurrentMode => currentMode;

        private SkillData stagedSkill;
        public SkillData StagedSkill => stagedSkill;
        public SkillCardSlotUI SelectPlaceSlot => selectPlaceSlot;

        private float currentScrollY = 0f;
        private float targetScrollY = 0f;

        private bool isDragging;
        private Vector2 lastPointerWorldPos;
        private Vector2 dragStartPointerPos;
        private bool hasDraggedPointer;

        private void Awake()
        {
            if (database == null) database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            if (bgRenderer == null) bgRenderer = GetComponent<SpriteRenderer>();
            if (loadedSkillPanelUI == null) loadedSkillPanelUI = FindFirstObjectByType<LoadedSkillPanelUI>();

            if (selectPlaceSlot == null)
            {
                GameObject spGo = GameObject.Find("SelectPlace");
                if (spGo != null)
                {
                    selectPlaceSlot = spGo.GetComponent<SkillCardSlotUI>();
                    if (selectPlaceSlot == null) selectPlaceSlot = spGo.AddComponent<SkillCardSlotUI>();
                }
            }

            FindExistingSlots();
            EnsureAscDecButton();
        }

        private void Start()
        {
            if (selectPlaceSlot != null)
            {
                if (selectPlaceSlot.CardBaseSprite == null && gridSlots.Count > 0 && gridSlots[0] != null)
                {
                    selectPlaceSlot.CardBaseSprite = gridSlots[0].BaseSprite;
                }
                selectPlaceSlot.Clear();
                if (currentMode == GridMode.Bow)
                {
                    selectPlaceSlot.gameObject.SetActive(false);
                }
            }

            EnsureAscDecButton();
            RefreshAllGridSlots();

            var inv = PlayerSkillInventory.Instance;
            if (inv != null)
            {
                inv.InventoryChanged -= OnInventoryChanged;
                inv.InventoryChanged += OnInventoryChanged;
            }

            var mgr = PlayerLoadoutManager.Instance;
            if (mgr != null)
            {
                mgr.BowChanged -= OnBowChanged;
                mgr.BowChanged += OnBowChanged;
            }
        }

        private void OnDestroy()
        {
            if (PlayerSkillInventory.HasInstance)
            {
                PlayerSkillInventory.Instance.InventoryChanged -= OnInventoryChanged;
            }

            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.BowChanged -= OnBowChanged;
            }
        }

        public void SetDatabase(LoadoutDatabase db)
        {
            if (db != null) database = db;
        }

        public void ShowSkills()
        {
            currentMode = GridMode.Skill;
            if (selectPlaceSlot != null)
            {
                selectPlaceSlot.gameObject.SetActive(true);
            }
            currentScrollY = 0f;
            targetScrollY = 0f;
            if (contentContainer != null)
            {
                contentContainer.localPosition = baseContentPos;
            }
            RefreshAllGridSlots();
        }

        public void ShowBows()
        {
            currentMode = GridMode.Bow;
            ClearStaging();
            if (selectPlaceSlot != null)
            {
                selectPlaceSlot.gameObject.SetActive(false);
            }
            currentScrollY = 0f;
            targetScrollY = 0f;
            if (contentContainer != null)
            {
                contentContainer.localPosition = baseContentPos;
            }
            RefreshAllGridSlots();
        }

        public void SetMode(GridMode mode)
        {
            currentMode = mode;
            if (mode == GridMode.Bow) ClearStaging();
            RefreshAllGridSlots();
        }

        public void FindExistingSlots()
        {
            if (contentContainer == null)
            {
                Transform found = transform.Find("GridContent");
                if (found == null && transform.parent != null)
                {
                    found = transform.parent.Find("SkillGrid/GridContent");
                }
                if (found == null)
                {
                    GameObject skillGrid = GameObject.Find("SkillGrid");
                    if (skillGrid != null) found = skillGrid.transform.Find("GridContent");
                }
                contentContainer = found;
            }

            if (contentContainer != null)
            {
                if (gridSlots.Count == 0 || gridSlots.Any(s => s == null) || gridSlots.Count < contentContainer.childCount)
                {
                    gridSlots.Clear();
                    for (int i = 0; i < contentContainer.childCount; i++)
                    {
                        Transform child = contentContainer.GetChild(i);
                        SkillCardSlotUI slotUI = child.GetComponent<SkillCardSlotUI>();
                        if (slotUI == null) slotUI = child.gameObject.AddComponent<SkillCardSlotUI>();
                        gridSlots.Add(slotUI);
                    }
                }

                for (int i = 0; i < gridSlots.Count; i++)
                {
                    SkillCardSlotUI slotUI = gridSlots[i];
                    if (slotUI == null) continue;

                    slotUI.InitializeSlot(i, database != null ? database.FontAsset : null);
                    slotUI.SetMaskInteraction(SpriteMaskInteraction.VisibleInsideMask);

                    slotUI.Clicked -= HandleGridSlotClicked;
                    slotUI.Clicked += HandleGridSlotClicked;
                }
            }

            int actualCount = (currentMode == GridMode.Bow)
                ? (database != null && database.Bows != null ? database.Bows.Count : 0)
                : (database != null && database.Skills != null ? database.Skills.Count : 0);
            UpdateScrollBounds(actualCount);
        }

        private void UpdateScrollBounds(int itemCount)
        {
            int rowCount = Mathf.CeilToInt((float)itemCount / 4);
            if (rowCount > 3)
            {
                maxScrollY = (rowCount - 3) * 1.80f;
            }
            else
            {
                maxScrollY = 0f;
            }
            targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxScrollY);
        }

        public void RefreshAllGridSlots()
        {
            FindExistingSlots();

            if (database == null) return;

            if (currentMode == GridMode.Bow)
            {
                RefreshBowGrid();
            }
            else
            {
                RefreshSkillGrid();
            }
        }

        private void RefreshSkillGrid()
        {
            if (database == null) database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            if (database == null || database.Skills == null || database.Skills.Count == 0) return;

            List<SkillData> sortedSkills = isAscending
                ? database.Skills.Where(s => s != null).OrderBy(s => (int)s.Rarity).ThenBy(s => s.DisplayName).ToList()
                : database.Skills.Where(s => s != null).OrderByDescending(s => (int)s.Rarity).ThenBy(s => s.DisplayName).ToList();

            if (sortedSkills.Count == 0) return;
            Debug.Log($"[SkillGrid] Refreshed with {sortedSkills.Count} skills (Database count: {database.Skills.Count})");

            for (int i = 0; i < gridSlots.Count; i++)
            {
                SkillCardSlotUI slotUI = gridSlots[i];
                if (slotUI == null) continue;

                slotUI.Clicked -= HandleGridSlotClicked;
                slotUI.Clicked += HandleGridSlotClicked;

                if (i < sortedSkills.Count)
                {
                    SkillData skill = sortedSkills[i];
                    slotUI.gameObject.SetActive(true);

                    string id = !string.IsNullOrEmpty(skill.SkillId) ? skill.SkillId : skill.name;
                    int level = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetLevel(id) : 1;
                    int cards = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetCards(id) : 0;
                    int required = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetRequiredCards(id) : 5;

                    slotUI.Setup(skill, level, cards, required,
                        database != null ? database.Frames : null,
                        database != null ? database.FontAsset : null);
                    slotUI.SetSelected(stagedSkill != null && skill == stagedSkill);
                }
                else
                {
                    slotUI.gameObject.SetActive(false);
                }
            }

            UpdateScrollBounds(sortedSkills.Count);
            UpdateCulling();
        }

        private void RefreshBowGrid()
        {
            if (database == null) database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            if (database == null || database.Bows == null || database.Bows.Count == 0) return;

            List<BowData> sortedBows = isAscending
                ? database.Bows.Where(b => b != null).OrderBy(b => (int)b.Rarity).ThenBy(b => b.DisplayName).ToList()
                : database.Bows.Where(b => b != null).OrderByDescending(b => (int)b.Rarity).ThenBy(b => b.DisplayName).ToList();

            if (sortedBows.Count == 0) return;

            BowData equippedBow = PlayerLoadoutManager.HasInstance ? PlayerLoadoutManager.Instance.EquippedBow : null;
            if (equippedBow == null && sortedBows.Count > 0)
            {
                equippedBow = sortedBows[0];
            }

            for (int i = 0; i < gridSlots.Count; i++)
            {
                SkillCardSlotUI slotUI = gridSlots[i];
                if (slotUI == null) continue;

                slotUI.Clicked -= HandleGridSlotClicked;
                slotUI.Clicked += HandleGridSlotClicked;

                if (i < sortedBows.Count)
                {
                    BowData bow = sortedBows[i];
                    slotUI.gameObject.SetActive(true);

                    string id = !string.IsNullOrEmpty(bow.BowId) ? bow.BowId : bow.name;
                    int level = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetLevel(id) : 1;
                    int cards = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetCards(id) : 0;
                    int required = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetRequiredCards(id) : 2;

                    slotUI.SetupBow(bow, level, cards, required, database.BowFrames, database.Frames, database.FontAsset);
                    slotUI.SetSelected(equippedBow != null && bow.BowId == equippedBow.BowId);
                }
                else
                {
                    slotUI.gameObject.SetActive(false);
                }
            }

            UpdateScrollBounds(sortedBows.Count);
            UpdateCulling();
        }

        private void OnInventoryChanged(string id)
        {
            RefreshAllGridSlots();
        }

        private void OnBowChanged(BowData bow)
        {
            if (currentMode == GridMode.Bow)
            {
                for (int i = 0; i < gridSlots.Count; i++)
                {
                    if (gridSlots[i] != null)
                    {
                        gridSlots[i].SetSelected(gridSlots[i].Bow != null && bow != null && gridSlots[i].Bow.BowId == bow.BowId);
                    }
                }
            }
        }

        private void HandleGridSlotClicked(SkillCardSlotUI clickedSlot)
        {
            if (clickedSlot == null) return;

            if (currentMode == GridMode.Bow)
            {
                if (clickedSlot.Bow != null)
                {
                    PlayerLoadoutManager.Instance.EquipBow(clickedSlot.Bow);
                    for (int i = 0; i < gridSlots.Count; i++)
                    {
                        if (gridSlots[i] != null)
                        {
                            gridSlots[i].SetSelected(gridSlots[i].Bow == clickedSlot.Bow);
                        }
                    }
                }
            }
            else
            {
                if (clickedSlot.Skill != null)
                {
                    StageSkill(clickedSlot.Skill);
                }
            }
        }

        public void StageSkill(SkillData skill)
        {
            if (skill == null) return;
            stagedSkill = skill;

            if (selectPlaceSlot != null)
            {
                string id = !string.IsNullOrEmpty(skill.SkillId) ? skill.SkillId : skill.name;
                int level = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetLevel(id) : 1;
                int cards = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetCards(id) : 0;
                int required = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetRequiredCards(id) : 5;

                selectPlaceSlot.Setup(skill, level, cards, required,
                    database != null ? database.Frames : null,
                    database != null ? database.FontAsset : null);
            }

            for (int i = 0; i < gridSlots.Count; i++)
            {
                if (gridSlots[i] != null)
                {
                    gridSlots[i].SetSelected(gridSlots[i].Skill == skill);
                }
            }
        }

        public void ClearStaging()
        {
            stagedSkill = null;
            if (selectPlaceSlot != null)
            {
                selectPlaceSlot.Clear();
                if (currentMode == GridMode.Bow)
                {
                    selectPlaceSlot.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < gridSlots.Count; i++)
            {
                if (gridSlots[i] != null && currentMode == GridMode.Skill)
                {
                    gridSlots[i].SetSelected(false);
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            HandleScrollInput();
            SmoothScroll();
            UpdateCulling();
        }

        private void HandleScrollInput()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            if (Mouse.current != null && maxScrollY > 0.01f)
            {
                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

                if (IsInsideBG(mouseWorld))
                {
                    float wheelY = Mouse.current.scroll.ReadValue().y;
                    if (Mathf.Abs(wheelY) > 0.01f)
                    {
                        targetScrollY -= wheelY * scrollSensitivity;
                        targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxScrollY);
                    }
                }
            }

            bool isDown = false;
            Vector2 pointerScreen = Vector2.zero;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                isDown = true;
                pointerScreen = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                isDown = true;
                pointerScreen = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                isDown = true;
                pointerScreen = UnityEngine.Input.mousePosition;
            }

            Vector2 pointerWorld = cam.ScreenToWorldPoint(pointerScreen);

            if (isDown)
            {
                if (!isDragging)
                {
                    if (IsInsideBG(pointerWorld))
                    {
                        isDragging = true;
                        lastPointerWorldPos = pointerWorld;
                        dragStartPointerPos = pointerWorld;
                        hasDraggedPointer = false;
                    }
                }
                else
                {
                    float deltaY = pointerWorld.y - lastPointerWorldPos.y;
                    if (Mathf.Abs(pointerWorld.y - dragStartPointerPos.y) > 0.12f || Mathf.Abs(pointerWorld.x - dragStartPointerPos.x) > 0.12f)
                    {
                        hasDraggedPointer = true;
                    }

                    if (maxScrollY > 0.01f)
                    {
                        targetScrollY -= deltaY * dragSensitivity;
                        targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxScrollY);
                    }
                    lastPointerWorldPos = pointerWorld;
                }
            }
            else
            {
                if (isDragging)
                {
                    if (!hasDraggedPointer)
                    {
                        TryHandlePointerClick(lastPointerWorldPos);
                    }
                }
                isDragging = false;
            }
        }

        private readonly Vector3 baseContentPos = Vector3.zero;

        private void SmoothScroll()
        {
            currentScrollY = Mathf.Lerp(currentScrollY, targetScrollY, Time.deltaTime * 18f);
            if (contentContainer != null)
            {
                float scaleFactor = contentContainer.localScale.y;
                contentContainer.localPosition = new Vector3(baseContentPos.x, baseContentPos.y + currentScrollY * scaleFactor, 0f);
            }
        }

        private void UpdateCulling()
        {
            if (bgRenderer == null) return;

            Bounds b = bgRenderer.bounds;
            float topBound = b.max.y + 0.1f;
            float bottomBound = b.min.y - 0.1f;

            for (int i = 0; i < gridSlots.Count; i++)
            {
                SkillCardSlotUI slot = gridSlots[i];
                if (slot == null || !slot.gameObject.activeSelf) continue;

                float slotWorldY = slot.transform.position.y;
                bool isVisible = slotWorldY <= topBound && slotWorldY >= bottomBound;
                slot.SetTextVisibility(isVisible);
            }
        }

        private bool IsInsideBG(Vector2 worldPoint)
        {
            if (bgRenderer == null) return true;
            Bounds b = bgRenderer.bounds;
            b.Expand(0.5f);
            return b.Contains(worldPoint);
        }

        public void ToggleSortOrder()
        {
            isAscending = !isAscending;
            UpdateAscDecButtonText();
            RefreshAllGridSlots();
        }

        public void SetSortOrder(bool ascending)
        {
            isAscending = ascending;
            UpdateAscDecButtonText();
            RefreshAllGridSlots();
        }

        public bool TryHandleAscDecClick(Vector2 worldPoint)
        {
            if (ascDecButton == null) EnsureAscDecButton();
            if (ascDecButton != null)
            {
                Collider2D col = ascDecButton.GetComponent<Collider2D>();
                SpriteRenderer sr = ascDecButton.GetComponent<SpriteRenderer>();
                if ((col != null && col.OverlapPoint(worldPoint)) || (sr != null && sr.bounds.Contains(worldPoint)))
                {
                    ButtonAudioManager.PlayClick();
                    ToggleSortOrder();
                    return true;
                }
            }
            return false;
        }

        public void EnsureAscDecButton()
        {
            if (ascDecButton == null)
            {
                if (transform.parent != null)
                {
                    Transform found = transform.parent.Find("ACSDEC") ?? transform.parent.Find("ASCDEC");
                    if (found != null) ascDecButton = found.gameObject;
                }
                if (ascDecButton == null)
                {
                    GameObject foundGo = GameObject.Find("ACSDEC") ?? GameObject.Find("ASCDEC");
                    if (foundGo != null) ascDecButton = foundGo;
                }
            }

            if (ascDecButton != null)
            {
                Collider2D col = ascDecButton.GetComponent<Collider2D>();
                if (col == null)
                {
                    BoxCollider2D box = ascDecButton.AddComponent<BoxCollider2D>();
                    SpriteRenderer bsr = ascDecButton.GetComponent<SpriteRenderer>();
                    if (bsr != null) box.size = bsr.size;
                    else box.size = new Vector2(1.55f, 0.62f);
                }

                if (ascDecText == null)
                {
                    ascDecText = ascDecButton.GetComponentInChildren<TextMeshPro>(true);
                    if (ascDecText == null)
                    {
                        GameObject textGo = new GameObject("Text");
                        textGo.transform.SetParent(ascDecButton.transform, false);
                        textGo.transform.localPosition = Vector3.zero;
                        ascDecText = textGo.AddComponent<TextMeshPro>();
                    }
                }

                if (ascDecText != null)
                {
                    if (database != null && database.FontAsset != null && ascDecText.font == null)
                    {
                        ascDecText.font = database.FontAsset;
                    }
                    if (ascDecText.sortingOrder < 3) ascDecText.sortingOrder = 5;
                }
            }

            UpdateAscDecButtonText();
        }

        private void UpdateAscDecButtonText()
        {
            if (ascDecText != null)
            {
                ascDecText.text = isAscending ? "Ascending" : "Decending";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && contentContainer != null && gridSlots != null && gridSlots.Count > 0)
            {
                UnityEditor.EditorApplication.delayCall -= DelayRefresh;
                UnityEditor.EditorApplication.delayCall += DelayRefresh;
            }
        }

        private void DelayRefresh()
        {
            if (this == null) return;
            RefreshAllGridSlots();
        }
#endif

        public bool TryHandlePointerClick(Vector2 worldPoint)
        {
            if (TryHandleAscDecClick(worldPoint)) return true;
            if (!IsInsideBG(worldPoint)) return false;

            for (int i = 0; i < gridSlots.Count; i++)
            {
                SkillCardSlotUI slot = gridSlots[i];
                if (slot != null && slot.gameObject.activeSelf && slot.ContainsPoint(worldPoint))
                {
                    slot.OnSlotClicked();
                    if (currentMode == GridMode.Bow)
                    {
                        if (slot.Bow != null)
                        {
                            PlayerLoadoutManager.Instance.EquipBow(slot.Bow);
                            for (int j = 0; j < gridSlots.Count; j++)
                            {
                                if (gridSlots[j] != null)
                                {
                                    gridSlots[j].SetSelected(gridSlots[j].Bow == slot.Bow);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (slot.Skill != null)
                        {
                            StageSkill(slot.Skill);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
