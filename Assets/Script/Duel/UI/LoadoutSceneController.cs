using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AOG.Duel
{
    // Điều khiển scene chọn trang bị: một scene, hai tab Bow / Skill.
    public sealed class LoadoutSceneController : MonoBehaviour
    {
        public enum LoadoutTab { Skill, Bow }

        private static LoadoutTab requestedTab = LoadoutTab.Skill;
        private static bool hasExplicitTab = false;

        [Header("World Sprite Objects - không dùng Canvas")]
        [SerializeField] private GameObject bowPanel;
        [SerializeField] private GameObject stand;
        [SerializeField] private Gear characterGear;
        [SerializeField] private GameObject backButton;
        [SerializeField] private GameObject selectPlace;
        [SerializeField] private string menuSceneName = "Menu";

        [Header("UI Controllers")]
        [SerializeField] private LoadedSkillPanelUI loadedSkillPanelUI;
        [SerializeField] private SkillCollectionGridUI skillCollectionGridUI;
        [SerializeField] private SkillCardSlotUI selectPlaceSlot;
        [SerializeField] private MenuOptionBar switchTabOptionBar;
        [SerializeField] private BowInfoDisplayUI bowInfoDisplayUI;
        [SerializeField] private LoadoutDatabase database;

        [Header("Editor Preview - Chuyển xem Tab khi đang căn chỉnh giao diện")]
        [SerializeField] private LoadoutTab previewInEditor = LoadoutTab.Skill;

        private bool isUpdatingTab;

        public static void Open(string sceneName, LoadoutTab tab)
        {
            requestedTab = tab;
            hasExplicitTab = true;
            SceneManager.LoadScene(sceneName);
        }

        private void Awake()
        {
            EnsureLoadoutComponents();
        }

        private void Start()
        {
            EnsureLoadoutComponents();

            if (switchTabOptionBar != null)
            {
                switchTabOptionBar.OptionSelected -= OnSwitchTabOptionSelected;
                switchTabOptionBar.OptionSelected += OnSwitchTabOptionSelected;
            }

            var mgr = PlayerLoadoutManager.Instance;
            if (mgr != null)
            {
                mgr.BowChanged -= HandleBowChanged;
                mgr.BowChanged += HandleBowChanged;
                if (characterGear != null && mgr.EquippedBow != null)
                {
                    characterGear.EquipBow(mgr.EquippedBow);
                }
            }

            LoadoutTab tabToShow = hasExplicitTab ? requestedTab : LoadoutTab.Skill;
            hasExplicitTab = false;

            Show(tabToShow);
        }

        private void OnDestroy()
        {
            if (switchTabOptionBar != null)
            {
                switchTabOptionBar.OptionSelected -= OnSwitchTabOptionSelected;
            }

            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.BowChanged -= HandleBowChanged;
            }
        }

        private void HandleBowChanged(BowData newBow)
        {
            if (characterGear != null && newBow != null)
            {
                characterGear.EquipBow(newBow);
            }
            if (bowInfoDisplayUI != null && newBow != null)
            {
                bowInfoDisplayUI.DisplayBow(newBow);
            }
        }

        public void ShowBow() => Show(LoadoutTab.Bow);
        public void ShowSkill() => Show(LoadoutTab.Skill);
        public void BackToMenu() => SceneManager.LoadScene(menuSceneName);

        private void OnSwitchTabOptionSelected(int index)
        {
            if (index == 0) ShowSkill();
            else if (index == 1) ShowBow();
        }

        private void Show(LoadoutTab tab)
        {
            if (isUpdatingTab) return;
            isUpdatingTab = true;

            try
            {
                requestedTab = tab;
                bool isSkill = (tab == LoadoutTab.Skill);

                // 1. Phía bên trái:
                // - Tab Skill: hiển thị LoadedSkillPanel (5 slot đã nạp) và SelectPlace
                // - Tab Bow: hiển thị Stand và Character (để xem trước cung trên tay)
                if (loadedSkillPanelUI != null) loadedSkillPanelUI.gameObject.SetActive(isSkill);
                if (selectPlace != null) selectPlace.SetActive(isSkill);
                if (selectPlaceSlot != null) selectPlaceSlot.gameObject.SetActive(isSkill);
                if (stand != null) stand.SetActive(!isSkill);
                if (characterGear != null) characterGear.gameObject.SetActive(!isSkill);

                var loadoutMgr = PlayerLoadoutManager.Instance;
                BowData currentBow = loadoutMgr != null ? loadoutMgr.EquippedBow : null;
                if (currentBow == null && database != null && database.Bows != null && database.Bows.Count > 0)
                {
                    currentBow = database.Bows[0];
                }

                if (!isSkill)
                {
                    if (characterGear != null && currentBow != null)
                    {
                        characterGear.EquipBow(currentBow);
                    }
                    if (bowInfoDisplayUI != null)
                    {
                        bowInfoDisplayUI.DisplayBow(currentBow);
                    }
                }

                // 2. Phía bên phải (Lưới danh sách trong SwitchTab):
                // - Luôn giữ BG và SwitchTab hiển thị
                if (skillCollectionGridUI != null)
                {
                    skillCollectionGridUI.gameObject.SetActive(true);
                    if (isSkill)
                    {
                        skillCollectionGridUI.ShowSkills();
                    }
                    else
                    {
                        skillCollectionGridUI.ShowBows();
                    }
                }

                // 3. Hiển thị trang Bow (nếu có panel riêng)
                if (bowPanel != null) bowPanel.SetActive(!isSkill);

                // 4. Cập nhật nút được chọn trên thanh SwitchTab/Option (Nút 1: Skill; Nút 2: Cung)
                if (switchTabOptionBar != null)
                {
                    switchTabOptionBar.Select(isSkill ? 0 : 1, notify: false);
                }

                // 5. Luôn giữ nút Back, thanh SwitchTab và nền đằng sau
                if (backButton != null) backButton.SetActive(true);
            }
            finally
            {
                isUpdatingTab = false;
            }
        }

        private void EnsureLoadoutComponents()
        {
            if (backButton == null)
            {
                GameObject found = GameObject.Find("BackButton");
                if (found != null) backButton = found;
            }

            if (stand == null)
            {
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                for (int i = 0; i < allTransforms.Length; i++)
                {
                    Transform t = allTransforms[i];
                    if (t != null && t.gameObject.scene == gameObject.scene && t.name == "Stand")
                    {
                        stand = t.gameObject;
                        break;
                    }
                }
            }

            if (characterGear == null)
            {
                characterGear = FindFirstObjectByType<Gear>(FindObjectsInactive.Include);
                if (characterGear == null)
                {
                    Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                    for (int i = 0; i < allTransforms.Length; i++)
                    {
                        Transform t = allTransforms[i];
                        if (t != null && t.gameObject.scene == gameObject.scene && t.name == "Character")
                        {
                            characterGear = t.GetComponent<Gear>();
                            if (characterGear != null) break;
                        }
                    }
                }
            }

            if (loadedSkillPanelUI == null)
            {
                loadedSkillPanelUI = FindFirstObjectByType<LoadedSkillPanelUI>();
                if (loadedSkillPanelUI == null)
                {
                    GameObject panel = GameObject.Find("LoadedSkillPanel");
                    if (panel != null) loadedSkillPanelUI = panel.AddComponent<LoadedSkillPanelUI>();
                }
            }

            if (skillCollectionGridUI == null)
            {
                skillCollectionGridUI = FindFirstObjectByType<SkillCollectionGridUI>();
                if (skillCollectionGridUI == null)
                {
                    GameObject switchTab = GameObject.Find("SwitchTab");
                    if (switchTab != null)
                    {
                        Transform bg = switchTab.transform.Find("BG");
                        if (bg != null) skillCollectionGridUI = bg.gameObject.AddComponent<SkillCollectionGridUI>();
                    }
                }
            }

            if (selectPlace == null)
            {
                if (selectPlaceSlot != null)
                {
                    selectPlace = selectPlaceSlot.gameObject;
                }
                else
                {
                    Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                    for (int i = 0; i < allTransforms.Length; i++)
                    {
                        Transform t = allTransforms[i];
                        if (t != null && t.gameObject.scene == gameObject.scene && t.name == "SelectPlace")
                        {
                            selectPlace = t.gameObject;
                            selectPlaceSlot = t.GetComponent<SkillCardSlotUI>();
                            break;
                        }
                    }
                }
            }
            if (selectPlaceSlot == null && selectPlace != null)
            {
                selectPlaceSlot = selectPlace.GetComponent<SkillCardSlotUI>();
                if (selectPlaceSlot == null) selectPlaceSlot = selectPlace.AddComponent<SkillCardSlotUI>();
            }

            if (switchTabOptionBar == null)
            {
                GameObject switchTab = GameObject.Find("SwitchTab");
                if (switchTab != null)
                {
                    Transform opt = switchTab.transform.Find("Option");
                    if (opt != null)
                    {
                        switchTabOptionBar = opt.GetComponent<MenuOptionBar>();
                        if (switchTabOptionBar == null) switchTabOptionBar = opt.gameObject.AddComponent<MenuOptionBar>();
                    }
                }
            }

            if (bowInfoDisplayUI == null)
            {
                if (stand != null) bowInfoDisplayUI = stand.GetComponentInChildren<BowInfoDisplayUI>(true);
                if (bowInfoDisplayUI == null) bowInfoDisplayUI = FindFirstObjectByType<BowInfoDisplayUI>(FindObjectsInactive.Include);
            }

            if (database == null)
            {
                database = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
            }
        }

        private void Update()
        {
            if (!TryGetPointerDown(out Vector2 screenPosition)) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 worldPoint = cam.ScreenToWorldPoint(screenPosition);

            // 1. Kiểm tra nút BackButton
            if (backButton != null)
            {
                SpriteRenderer sr = backButton.GetComponent<SpriteRenderer>();
                Collider2D col = backButton.GetComponent<Collider2D>();
                if ((sr != null && sr.bounds.Contains(worldPoint)) || (col != null && col.OverlapPoint(worldPoint)))
                {
                    BackToMenu();
                    return;
                }
            }

            // 2. Kiểm tra thanh SwitchTab/Option (chuyển giữa tab Skill và tab Bow)
            if (switchTabOptionBar != null && switchTabOptionBar.TryHandlePointerClick(worldPoint))
            {
                return;
            }

            // 3. Kiểm tra nút ASCDEC (hoạt động cho cả 2 tab)
            if (skillCollectionGridUI != null && skillCollectionGridUI.TryHandleAscDecClick(worldPoint))
            {
                return;
            }

            // 3. Nếu đang ở trang Skill:
            if (requestedTab == LoadoutTab.Skill)
            {
                // Click vào LoadedSkillPanel (5 slot kỹ năng hoặc thanh Option 4 set)
                if (loadedSkillPanelUI != null && loadedSkillPanelUI.TryHandlePointerClick(worldPoint))
                {
                    return;
                }

                // Click chọn chiêu trong danh sách toàn bộ kỹ năng bên dưới để đưa vào SelectPlace
                if (skillCollectionGridUI != null && skillCollectionGridUI.TryHandlePointerClick(worldPoint))
                {
                    return;
                }

                // Click vào SelectPlace để hủy chọn nếu không muốn trang bị nữa
                if (selectPlaceSlot != null && selectPlaceSlot.ContainsPoint(worldPoint))
                {
                    if (skillCollectionGridUI != null) skillCollectionGridUI.ClearStaging();
                    return;
                }
            }
            else if (requestedTab == LoadoutTab.Bow)
            {
                // Click chọn cung trong danh sách toàn bộ cung
                if (skillCollectionGridUI != null && skillCollectionGridUI.TryHandlePointerClick(worldPoint))
                {
                    return;
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
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                screenPosition = UnityEngine.Input.mousePosition;
                return true;
            }
            return false;
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall -= DelayApplyEditorPreview;
                UnityEditor.EditorApplication.delayCall += DelayApplyEditorPreview;
            }
        }

        private void DelayApplyEditorPreview()
        {
            if (this == null) return;
            ApplyEditorPreview();
        }

        [ContextMenu("Chuyển xem Tab Kỹ Năng (Skill)")]
        public void EditorPreviewSkillTab()
        {
            previewInEditor = LoadoutTab.Skill;
            ApplyEditorPreview();
        }

        [ContextMenu("Chuyển xem Tab Cung (Bow)")]
        public void EditorPreviewBowTab()
        {
            previewInEditor = LoadoutTab.Bow;
            ApplyEditorPreview();
        }

        public void ApplyEditorPreview()
        {
            EnsureLoadoutComponents();
            bool isSkill = (previewInEditor == LoadoutTab.Skill);

            if (loadedSkillPanelUI != null) loadedSkillPanelUI.gameObject.SetActive(isSkill);
            if (selectPlace != null) selectPlace.SetActive(isSkill);
            if (selectPlaceSlot != null) selectPlaceSlot.gameObject.SetActive(isSkill);

            if (stand != null) stand.SetActive(!isSkill);
            if (characterGear != null) characterGear.gameObject.SetActive(!isSkill);

            BowData previewBow = null;
            if (PlayerLoadoutManager.HasInstance) previewBow = PlayerLoadoutManager.Instance.EquippedBow;
            if (previewBow == null && database != null && database.Bows != null && database.Bows.Count > 0)
            {
                previewBow = database.Bows[0];
            }

            if (!isSkill)
            {
                if (characterGear != null && previewBow != null)
                {
                    characterGear.EquipBow(previewBow);
                }
                if (bowInfoDisplayUI != null)
                {
                    bowInfoDisplayUI.DisplayBow(previewBow);
                }
            }

            if (skillCollectionGridUI != null)
            {
                skillCollectionGridUI.gameObject.SetActive(true);
                if (database != null) skillCollectionGridUI.SetDatabase(database);
                if (isSkill)
                {
                    skillCollectionGridUI.ShowSkills();
                }
                else
                {
                    skillCollectionGridUI.ShowBows();
                }
            }

            if (switchTabOptionBar != null)
            {
                switchTabOptionBar.Select(isSkill ? 0 : 1, notify: false);
            }
        }
#endif
    }
}
