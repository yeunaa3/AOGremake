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
        [SerializeField] private string loadoutSceneName = "Loadout";

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
            if (!TryGetPointerDown(out Vector2 screenPos)) return;

            if ((settingsUI != null && settingsUI.IsOpen) || (selectionUI != null && selectionUI.IsOpen)) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // Cùng cơ chế với MenuOptionBar: chỉ quét physics một lần đúng lúc click.
            Vector2 worldPoint = cam.ScreenToWorldPoint(screenPos);
            foreach (Collider2D hit in Physics2D.OverlapPointAll(worldPoint))
            {
                if (BelongsToButton(hit, settingButton)) { PlayButtonFeedback(settingButton); OpenSettings(); return; }
                if (BelongsToButton(hit, bowButton)) { PlayButtonFeedback(bowButton); OpenBowSelection(); return; }
                if (BelongsToButton(hit, skillButton)) { PlayButtonFeedback(skillButton); OpenSkillSelection(0); return; }
                if (BelongsToButton(hit, pveButton)) { PlayButtonFeedback(pveButton); LoadPVEScene(); return; }
                if (BelongsToButton(hit, pvpButton)) { PlayButtonFeedback(pvpButton); LoadPVPScene(); return; }
                if (BelongsToButton(hit, rankButton)) { PlayButtonFeedback(rankButton); return; }
            }
        }

        private static bool BelongsToButton(Collider2D hit, GameObject button)
        {
            return hit != null && button != null &&
                   (hit.transform == button.transform || hit.transform.IsChildOf(button.transform));
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

        public void OpenBowSelection() => LoadoutSceneController.Open(loadoutSceneName, LoadoutSceneController.LoadoutTab.Bow);
        public void OpenSkillSelection(int targetSlot = 0) => LoadoutSceneController.Open(loadoutSceneName, LoadoutSceneController.LoadoutTab.Skill);
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
            EnsureButtonColliders(bowButton, new Vector2(1.5f, 0.5f));
            EnsureButtonColliders(skillButton, new Vector2(1.5f, 0.5f));
            EnsureButtonColliders(pveButton, new Vector2(1.5f, 0.5f));
            EnsureButtonColliders(pvpButton, new Vector2(1.5f, 0.5f));
            EnsureButtonColliders(rankButton, new Vector2(1.5f, 0.5f));
            EnsureButtonColliders(settingButton, new Vector2(1.2f, 1.2f));
        }

        private static void EnsureButtonColliders(GameObject button, Vector2 fallbackSize)
        {
            if (button == null) return;
            AddBoxColliderIfMissing(button, fallbackSize);
            foreach (SpriteRenderer sprite in button.GetComponentsInChildren<SpriteRenderer>(true))
                AddBoxColliderIfMissing(sprite.gameObject, fallbackSize);
        }

        private static void AddBoxColliderIfMissing(GameObject obj, Vector2 fallbackSize)
        {
            if (obj == null) return;
            // Không dùng ?? với Unity Component: component bị xoá có thể là "fake null"
            // và khiến Awake văng MissingComponentException.
            var box = obj.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = obj.AddComponent<BoxCollider2D>();
            }
            if (box == null) return;
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

        private static bool TryGetPointerDown(out Vector2 screenPos)
        {
            screenPos = Vector2.zero;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            return false;
        }
    }
}
