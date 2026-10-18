using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AOG.Duel
{
    // Quản lý các tương tác tại Menu chính: nút Bow, nút Skill, PVE (Map1), PVP (Map2).
    public sealed class MenuController : MonoBehaviour
    {
        [Header("Giao Diện & UI Selection")]
        [SerializeField] private LoadoutSelectionUI selectionUI;
        [SerializeField] private MenuSkillBar skillBar;

        [Header("Nhân Vật Trong Sảnh")]
        [SerializeField] private Gear characterGear;

        [Header("Tên Các Màn Đấu")]
        [SerializeField] private string pveSceneName = "Map1";
        [SerializeField] private string pvpSceneName = "Map2";

        [Header("Các Nút 2D Trong Menu (World Space)")]
        [SerializeField] private GameObject bowButton;
        [SerializeField] private GameObject skillButton;
        [SerializeField] private GameObject pveButton;
        [SerializeField] private GameObject pvpButton;
        [SerializeField] private GameObject rankButton;

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;

            if (selectionUI == null)
            {
                selectionUI = GetComponentInChildren<LoadoutSelectionUI>();
                if (selectionUI == null)
                {
                    GameObject uiObj = new GameObject("LoadoutSelectionUI");
                    uiObj.transform.SetParent(transform, false);
                    selectionUI = uiObj.AddComponent<LoadoutSelectionUI>();
                }
            }

            if (skillBar == null)
            {
                skillBar = FindFirstObjectByType<MenuSkillBar>();
            }

            if (characterGear == null)
            {
                characterGear = FindFirstObjectByType<Gear>();
            }

            AutoFindButtons();
            EnsureColliders();
        }

        private void Start()
        {
            // Đồng bộ cung đã lưu lên nhân vật đứng trên bục
            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.BowChanged += HandleBowChanged;
                if (characterGear != null && PlayerLoadoutManager.Instance.EquippedBow != null)
                {
                    characterGear.EquipBow(PlayerLoadoutManager.Instance.EquippedBow);
                }
            }

            if (skillBar != null)
            {
                skillBar.RefreshSlots();
            }
        }

        private void OnDestroy()
        {
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
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
            HandlePointerInput();
        }

        private void HandleKeyboardShortcuts()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (selectionUI != null && selectionUI.IsOpen)
                {
                    selectionUI.CloseAll();
                }
            }

            if (selectionUI != null && selectionUI.IsOpen) return;

            // Phím tắt tiện lợi
            if (keyboard.digit1Key.wasPressedThisFrame) LoadPVEScene();
            if (keyboard.digit2Key.wasPressedThisFrame) LoadPVPScene();
            if (keyboard.bKey.wasPressedThisFrame) OpenBowSelection();
            if (keyboard.kKey.wasPressedThisFrame) OpenSkillSelection(0);
        }

        private void HandlePointerInput()
        {
            // Kiểm tra click chuột hoặc chạm màn hình
            bool pointerDown = false;
            Vector2 screenPos = Vector2.zero;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                screenPos = mouse.position.ReadValue();
            }
            else
            {
                Touchscreen touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                {
                    pointerDown = true;
                    screenPos = touch.primaryTouch.position.ReadValue();
                }
            }

            if (!pointerDown) return;

            // Bỏ qua nếu con trỏ đang click trên UI Canvas (popup modal)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (selectionUI != null && selectionUI.IsOpen)
            {
                selectionUI.CloseAll();
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 worldPoint = mainCamera.ScreenToWorldPoint(screenPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPoint);
            if (hit == null) return;

            GameObject clickedObj = hit.gameObject;

            if (IsTarget(clickedObj, bowButton))
            {
                PlayButtonFeedback(bowButton);
                OpenBowSelection();
            }
            else if (IsTarget(clickedObj, skillButton))
            {
                PlayButtonFeedback(skillButton);
                OpenSkillSelection(0);
            }
            else if (IsTarget(clickedObj, pveButton))
            {
                PlayButtonFeedback(pveButton);
                LoadPVEScene();
            }
            else if (IsTarget(clickedObj, pvpButton))
            {
                PlayButtonFeedback(pvpButton);
                LoadPVPScene();
            }
            else if (IsTarget(clickedObj, rankButton))
            {
                PlayButtonFeedback(rankButton);
            }
            else
            {
                // Kiểm tra xem có click vào 5 ô skill trên bục đá không
                for (int i = 1; i <= 5; i++)
                {
                    if (clickedObj.name.Contains($"Slot_{i}") || clickedObj.name.Contains($"Slot{i}"))
                    {
                        OpenSkillSelection(i - 1);
                        break;
                    }
                }
            }
        }

        private static bool IsTarget(GameObject clicked, GameObject target)
        {
            if (clicked == null || target == null) return false;
            return clicked == target || clicked.transform.IsChildOf(target.transform);
        }

        public void OpenBowSelection()
        {
            if (selectionUI != null) selectionUI.OpenBowSelection();
        }

        public void OpenSkillSelection(int targetSlot = 0)
        {
            if (selectionUI != null) selectionUI.OpenSkillSelection(targetSlot);
        }

        public void LoadPVEScene()
        {
            SceneManager.LoadScene(pveSceneName);
        }

        public void LoadPVPScene()
        {
            SceneManager.LoadScene(pvpSceneName);
        }

        private void PlayButtonFeedback(GameObject buttonObj)
        {
            if (buttonObj == null) return;
            StartCoroutine(PunchScaleRoutine(buttonObj.transform));
        }

        private IEnumerator PunchScaleRoutine(Transform target)
        {
            if (target == null) yield break;
            Vector3 original = target.localScale;
            target.localScale = original * 0.9f;
            yield return new WaitForSecondsRealtime(0.08f);
            if (target != null) target.localScale = original * 1.05f;
            yield return new WaitForSecondsRealtime(0.06f);
            if (target != null) target.localScale = original;
        }

        private void AutoFindButtons()
        {
            if (bowButton == null) bowButton = GameObject.Find("Bow Button");
            if (skillButton == null) skillButton = GameObject.Find("Skill Button");
            if (pveButton == null) pveButton = GameObject.Find("PVE Button ") ?? GameObject.Find("PVE Button");
            if (pvpButton == null) pvpButton = GameObject.Find("PVP Button");
            if (rankButton == null) rankButton = GameObject.Find("Rank");
        }

        private void EnsureColliders()
        {
            AddBoxColliderIfMissing(bowButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(skillButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(pveButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(pvpButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(rankButton, new Vector2(1.5f, 0.5f));

            // Thêm collider cho 5 slot trên bục nếu có
            for (int i = 1; i <= 5; i++)
            {
                GameObject slot = GameObject.Find($"Slot_{i}");
                if (slot != null)
                {
                    AddBoxColliderIfMissing(slot, new Vector2(0.5f, 0.5f));
                }
            }
        }

        private static void AddBoxColliderIfMissing(GameObject obj, Vector2 fallbackSize)
        {
            if (obj == null) return;
            BoxCollider2D box = obj.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = obj.AddComponent<BoxCollider2D>();
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    box.size = sr.size != Vector2.zero ? sr.size : fallbackSize;
                }
                else
                {
                    box.size = fallbackSize;
                }
            }
        }
    }
}
