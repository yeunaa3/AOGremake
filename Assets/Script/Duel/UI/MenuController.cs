using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AOG.Duel
{
    // Quản lý tương tác Menu: nút Cung, Kỹ năng, Setting, PVE và PVP
    public sealed class MenuController : MonoBehaviour
    {
        [Header("Giao Diện & Dữ Liệu")]
        [SerializeField] private LoadoutSelectionUI selectionUI;
        [SerializeField] private SettingsPopupUI settingsUI;
        [SerializeField] private Gear characterGear;

        [Header("Các Nút Menu")]
        [SerializeField] private GameObject bowButton;
        [SerializeField] private GameObject skillButton;
        [SerializeField] private GameObject pveButton;
        [SerializeField] private GameObject pvpButton;
        [SerializeField] private GameObject rankButton;
        [SerializeField] private GameObject settingButton;

        [Header("Tên Màn Đấu")]
        [SerializeField] private string pveSceneName = "Map1";
        [SerializeField] private string pvpSceneName = "Map2";

        private void Awake()
        {
            if (characterGear == null) characterGear = FindFirstObjectByType<Gear>();
            if (selectionUI == null) selectionUI = GetComponentInChildren<LoadoutSelectionUI>();
            if (settingsUI == null) settingsUI = FindFirstObjectByType<SettingsPopupUI>();

            AutoFindButtons();
            EnsureColliders();
        }

        private void Start()
        {
            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.BowChanged += HandleBowChanged;
                if (characterGear != null && PlayerLoadoutManager.Instance.EquippedBow != null)
                {
                    characterGear.EquipBow(PlayerLoadoutManager.Instance.EquippedBow);
                }
            }
        }

        private void OnDestroy()
        {
            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.BowChanged -= HandleBowChanged;
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
                if (settingsUI != null && settingsUI.IsOpen)
                {
                    settingsUI.Close();
                    return;
                }
                if (selectionUI != null && selectionUI.IsOpen)
                {
                    selectionUI.CloseAll();
                    return;
                }
            }

            if ((selectionUI != null && selectionUI.IsOpen) || (settingsUI != null && settingsUI.IsOpen)) return;

            if (keyboard.digit1Key.wasPressedThisFrame) LoadPVEScene();
            if (keyboard.digit2Key.wasPressedThisFrame) LoadPVPScene();
            if (keyboard.bKey.wasPressedThisFrame) OpenBowSelection();
            if (keyboard.kKey.wasPressedThisFrame) OpenSkillSelection(0);
        }

        private void HandlePointerInput()
        {
            Vector2 screenPos = Vector2.zero;
            bool pointerDown = false;

            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame))
            {
                pointerDown = true;
                screenPos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.wasPressedThisFrame || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                pointerDown = true;
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Pointer.current != null && (Pointer.current.press.wasPressedThisFrame || Pointer.current.press.wasReleasedThisFrame))
            {
                pointerDown = true;
                screenPos = Pointer.current.position.ReadValue();
            }

            if (!pointerDown) return;

            if ((settingsUI != null && settingsUI.IsOpen) || (selectionUI != null && selectionUI.IsOpen)) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float zDist = Mathf.Abs(cam.transform.position.z);
            Vector3 worldPos3D = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
            Vector2 worldPoint = new Vector2(worldPos3D.x, worldPos3D.y);

            // Kiểm tra click từng nút (kết hợp cả Collider lẫn Bounds hình học để đảm bảo 100% ăn click)
            if (CheckButtonHit(settingButton, worldPoint, new Vector2(1.2f, 1.2f)))
            {
                PlayButtonFeedback(settingButton);
                OpenSettings();
                return;
            }
            if (CheckButtonHit(bowButton, worldPoint, new Vector2(3.0f, 1.0f)))
            {
                PlayButtonFeedback(bowButton);
                OpenBowSelection();
                return;
            }
            if (CheckButtonHit(skillButton, worldPoint, new Vector2(3.0f, 1.0f)))
            {
                PlayButtonFeedback(skillButton);
                OpenSkillSelection(0);
                return;
            }
            if (CheckButtonHit(pveButton, worldPoint, new Vector2(3.0f, 1.0f)))
            {
                PlayButtonFeedback(pveButton);
                LoadPVEScene();
                return;
            }
            if (CheckButtonHit(pvpButton, worldPoint, new Vector2(3.0f, 1.0f)))
            {
                PlayButtonFeedback(pvpButton);
                LoadPVPScene();
                return;
            }
            if (CheckButtonHit(rankButton, worldPoint, new Vector2(3.0f, 1.0f)))
            {
                PlayButtonFeedback(rankButton);
                return;
            }
        }

        private static bool CheckButtonHit(GameObject btn, Vector2 point, Vector2 fallbackSize)
        {
            if (btn == null || !btn.activeInHierarchy) return false;

            // 1. Kiểm tra Collider2D
            var col = btn.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(point)) return true;

            // 2. Kiểm tra SpriteRenderer Bounds trực tiếp
            var sr = btn.GetComponent<SpriteRenderer>();
            if (sr != null && sr.bounds.Contains(point)) return true;

            // 3. Kiểm tra vùng Rect thực tế
            Vector2 pos = btn.transform.position;
            Vector2 size = (sr != null && sr.sprite != null) ? (Vector2)sr.bounds.size : fallbackSize;
            size = new Vector2(Mathf.Max(size.x, fallbackSize.x), Mathf.Max(size.y, fallbackSize.y));
            Rect rect = new Rect(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, size.x, size.y);
            if (rect.Contains(point)) return true;

            // 4. Kiểm tra các object con
            for (int i = 0; i < btn.transform.childCount; i++)
            {
                Transform child = btn.transform.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;

                var childCol = child.GetComponent<Collider2D>();
                if (childCol != null && childCol.OverlapPoint(point)) return true;

                var childSr = child.GetComponent<SpriteRenderer>();
                if (childSr != null && childSr.bounds.Contains(point)) return true;
            }

            return false;
        }

        public void OpenSettings()
        {
            Debug.Log("[MenuController] OpenSettings triggered!");
            if (settingsUI == null)
            {
                settingsUI = FindFirstObjectByType<SettingsPopupUI>();
                if (settingsUI == null)
                {
                    var sObj = new GameObject("SettingsPopupUI");
                    settingsUI = sObj.AddComponent<SettingsPopupUI>();
                }
            }
            settingsUI.Open();
        }

        public void OpenBowSelection() => selectionUI?.OpenBowSelection();
        public void OpenSkillSelection(int targetSlot = 0) => selectionUI?.OpenSkillSelection(targetSlot);
        public void LoadPVEScene() => SceneManager.LoadScene(pveSceneName);
        public void LoadPVPScene() => SceneManager.LoadScene(pvpSceneName);

        private void HandleBowChanged(BowData newBow)
        {
            if (characterGear != null && newBow != null)
            {
                characterGear.EquipBow(newBow);
            }
        }

        private void PlayButtonFeedback(GameObject buttonObj)
        {
            if (buttonObj == null || !gameObject.activeInHierarchy) return;
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
            if (settingButton == null) settingButton = GameObject.Find("Setting");
        }

        private void EnsureColliders()
        {
            AddBoxColliderIfMissing(bowButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(skillButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(pveButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(pvpButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(rankButton, new Vector2(1.5f, 0.5f));
            AddBoxColliderIfMissing(settingButton, new Vector2(1.2f, 1.2f));

            if (settingButton != null && settingButton.transform.childCount > 0)
            {
                AddBoxColliderIfMissing(settingButton.transform.GetChild(0).gameObject, new Vector2(1.2f, 1.2f));
            }
        }

        private static void AddBoxColliderIfMissing(GameObject obj, Vector2 fallbackSize)
        {
            if (obj == null) return;
            var box = obj.GetComponent<BoxCollider2D>() ?? obj.AddComponent<BoxCollider2D>();
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float w = sr.drawMode != SpriteDrawMode.Simple ? sr.size.x : sr.sprite.bounds.size.x;
                float h = sr.drawMode != SpriteDrawMode.Simple ? sr.size.y : sr.sprite.bounds.size.y;
                box.size = new Vector2(Mathf.Max(fallbackSize.x, w), Mathf.Max(fallbackSize.y, h));
                box.offset = (Vector2)sr.localBounds.center;
            }
            else
            {
                box.size = fallbackSize;
            }
        }
    }
}
