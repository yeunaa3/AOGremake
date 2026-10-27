using System;
using System.Collections.Generic;
using UnityEngine;

namespace AOG.Duel
{
    // Quản lý 5 slot kỹ năng đã trang bị trong LoadedSkillPanel.
    // Tận dụng trực tiếp các GameObject slot có sẵn trong Scene, chỉ thay đổi sprite và text.
    public sealed class LoadedSkillPanelUI : MonoBehaviour
    {
        [Header("Tham Chiếu Dữ Liệu")]
        [SerializeField] private LoadoutDatabase database;
        [SerializeField] private SkillCollectionGridUI skillCollectionGridUI;
        [SerializeField] private MenuOptionBar setOptionBar;

        [Header("5 Slots Kỹ Năng Đã Chọn (Có sẵn trong Scene)")]
        [SerializeField] private List<SkillCardSlotUI> slots = new List<SkillCardSlotUI>();

        private int selectedSlotIndex = 0;
        public int SelectedSlotIndex => selectedSlotIndex;

        public event Action<int> SlotSelected;

        private void Awake()
        {
            if (database == null)
            {
                database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            }

            if (skillCollectionGridUI == null)
            {
                skillCollectionGridUI = FindFirstObjectByType<SkillCollectionGridUI>();
            }

            FindExistingSlots();
        }

        private void Start()
        {
            if (database == null)
            {
                database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            }

            if (setOptionBar == null)
            {
                Transform optT = transform.Find("Option");
                if (optT == null && transform.parent != null)
                {
                    optT = transform.parent.Find("Option");
                }
                if (optT != null)
                {
                    setOptionBar = optT.GetComponent<MenuOptionBar>();
                    if (setOptionBar == null) setOptionBar = optT.gameObject.AddComponent<MenuOptionBar>();
                }
                if (setOptionBar == null)
                {
                    setOptionBar = FindFirstObjectByType<MenuOptionBar>();
                }
            }

            PlayerLoadoutManager loadoutMgr = PlayerLoadoutManager.Instance;

            if (setOptionBar != null && loadoutMgr != null)
            {
                setOptionBar.Select(loadoutMgr.ActiveSkillSet, notify: false);
                setOptionBar.OptionSelected -= OnSetOptionSelected;
                setOptionBar.OptionSelected += OnSetOptionSelected;
            }

            if (loadoutMgr != null)
            {
                loadoutMgr.SkillChanged -= OnSkillChanged;
                loadoutMgr.SkillChanged += OnSkillChanged;
                loadoutMgr.LoadoutChanged -= RefreshAllSlots;
                loadoutMgr.LoadoutChanged += RefreshAllSlots;
                loadoutMgr.ActiveSkillSetChanged -= OnActiveSkillSetChanged;
                loadoutMgr.ActiveSkillSetChanged += OnActiveSkillSetChanged;
            }

            if (PlayerSkillInventory.HasInstance)
            {
                PlayerSkillInventory.Instance.InventoryChanged -= OnInventoryChanged;
                PlayerSkillInventory.Instance.InventoryChanged += OnInventoryChanged;
            }

            RefreshAllSlots();
            SelectSlot(0);
        }

        private void OnDestroy()
        {
            if (setOptionBar != null)
            {
                setOptionBar.OptionSelected -= OnSetOptionSelected;
            }

            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.SkillChanged -= OnSkillChanged;
                PlayerLoadoutManager.Instance.LoadoutChanged -= RefreshAllSlots;
                PlayerLoadoutManager.Instance.ActiveSkillSetChanged -= OnActiveSkillSetChanged;
            }

            if (PlayerSkillInventory.HasInstance)
            {
                PlayerSkillInventory.Instance.InventoryChanged -= OnInventoryChanged;
            }
        }

        private void OnSetOptionSelected(int setIndex)
        {
            PlayerLoadoutManager.Instance.SetActiveSkillSet(setIndex);
        }

        private void OnActiveSkillSetChanged(int setIndex)
        {
            if (setOptionBar != null && setOptionBar.selectedIndex != setIndex)
            {
                setOptionBar.Select(setIndex, notify: false);
            }
            RefreshAllSlots();
        }

        public void FindExistingSlots()
        {
            if (slots.Count >= 5 && slots[0] != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null)
                    {
                        slots[i].InitializeSlot(i, database != null ? database.FontAsset : null);
                        slots[i].Clicked -= HandleSlotClicked;
                        slots[i].Clicked += HandleSlotClicked;
                    }
                }
                return;
            }

            slots.Clear();

            // Tìm tất cả các slot có sẵn là con của LoadedSkillPanel
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("slot", StringComparison.OrdinalIgnoreCase))
                {
                    SkillCardSlotUI slotUI = child.GetComponent<SkillCardSlotUI>();
                    if (slotUI == null) slotUI = child.gameObject.AddComponent<SkillCardSlotUI>();
                    slotUI.InitializeSlot(slots.Count, database != null ? database.FontAsset : null);

                    slotUI.Clicked -= HandleSlotClicked;
                    slotUI.Clicked += HandleSlotClicked;

                    slots.Add(slotUI);
                    if (slots.Count == 5) break;
                }
            }
        }

        public void RefreshAllSlots()
        {
            FindExistingSlots();

            for (int i = 0; i < slots.Count; i++)
            {
                UpdateSlotUI(i);
            }
        }

        private void OnSkillChanged(int slot, SkillData skill)
        {
            if (slot >= 0 && slot < slots.Count)
            {
                UpdateSlotUI(slot);
            }
        }

        private void OnInventoryChanged(string skillId)
        {
            RefreshAllSlots();
        }

        private void UpdateSlotUI(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;

            SkillCardSlotUI slot = slots[slotIndex];
            if (slot == null) return;

            if (database == null) database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            SkillData equipped = PlayerLoadoutManager.Instance != null ? PlayerLoadoutManager.Instance.GetEquippedSkill(slotIndex) : null;
            if (equipped != null)
            {
                string id = !string.IsNullOrEmpty(equipped.SkillId) ? equipped.SkillId : equipped.name;
                int level = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetLevel(id) : 1;
                int cards = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetCards(id) : 0;
                int required = PlayerSkillInventory.HasInstance ? PlayerSkillInventory.Instance.GetRequiredCards(id) : 5;

                slot.Setup(equipped, level, cards, required,
                    database != null ? database.Frames : null,
                    database != null ? database.FontAsset : null);
            }
            else
            {
                slot.Setup(null, 1, 0, 5,
                    database != null ? database.Frames : null,
                    database != null ? database.FontAsset : null);
            }

            slot.SetSelected(slotIndex == selectedSlotIndex);
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= slots.Count) return;
            selectedSlotIndex = index;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null) slots[i].SetSelected(i == selectedSlotIndex);
            }

            SlotSelected?.Invoke(selectedSlotIndex);
        }

        public void OnSlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;

            if (skillCollectionGridUI == null)
            {
                skillCollectionGridUI = FindFirstObjectByType<SkillCollectionGridUI>();
            }

            // Nếu SelectPlace đang chứa kỹ năng được chọn từ tủ đồ, gán kỹ năng đó vào slot được bấm!
            if (skillCollectionGridUI != null && skillCollectionGridUI.StagedSkill != null)
            {
                SkillData skillToEquip = skillCollectionGridUI.StagedSkill;
                PlayerLoadoutManager.Instance.EquipSkillToSlot(slotIndex, skillToEquip);
                // Xóa trạng thái staging sau khi đã đưa vào slot mong muốn
                skillCollectionGridUI.ClearStaging();
            }

            SelectSlot(slotIndex);
        }

        private void HandleSlotClicked(SkillCardSlotUI clickedSlot)
        {
            int idx = slots.IndexOf(clickedSlot);
            if (idx >= 0)
            {
                OnSlotClicked(idx);
            }
        }

        public bool TryHandlePointerClick(Vector2 worldPoint)
        {
            // Kiểm tra click vào thanh chọn Set (Option 1, 2, 3, 4)
            if (setOptionBar != null && setOptionBar.TryHandlePointerClick(worldPoint))
            {
                return true;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].ContainsPoint(worldPoint))
                {
                    OnSlotClicked(i);
                    return true;
                }
            }
            return false;
        }
    }
}
