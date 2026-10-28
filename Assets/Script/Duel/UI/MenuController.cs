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
        [SerializeField] private MenuOptionBar setOptionBar;
        [Header("Stand Bow Info Display")]
        [SerializeField] private GameObject nameBowObject;
        [SerializeField] private GameObject rareObject;

        [Header("Loadout Display (Chỉ hiển thị 5 icon kỹ năng trên bệ đứng)")]
        [SerializeField] private SpriteRenderer[] pedestalSkillIcons;

        [Header("Tên Màn Đấu")]
        [SerializeField] private string[] pveSceneNames = new string[] { "Map1", "Map2" };
        [SerializeField] private string pveSceneName = "Map1";
        [SerializeField] private string pvpSceneName = "Map2";
        [SerializeField] private string loadoutSceneName = "Loadout";

        [Header("Nhạc Nền Menu (Lobby)")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Nhạc nền Lobby Angel (BGM_Lobby_Angel)")]
        [SerializeField] private AudioClip angelLobbyBgm;
        [Tooltip("Âm lượng nhạc nền Angel")]
        [SerializeField, Range(0f, 1f)] private float angelLobbyVolume = 0.5f;
        [Tooltip("Nhạc nền Lobby Devil (BGM_Lobby_Devil)")]
        [SerializeField] private AudioClip devilLobbyBgm;
        [Tooltip("Âm lượng nhạc nền Devil")]
        [SerializeField, Range(0f, 1f)] private float devilLobbyVolume = 0.5f;

        private void Awake()
        {
            if (characterGear == null) characterGear = FindFirstObjectByType<Gear>();
            if (selectionUI == null) selectionUI = GetComponentInChildren<LoadoutSelectionUI>();
            if (settingsUI == null) settingsUI = FindFirstObjectByType<SettingsPopupUI>();
            if (setOptionBar == null) setOptionBar = FindFirstObjectByType<MenuOptionBar>();
            AutoFindPedestalIcons();

            AutoFindButtons();
            EnsureColliders();
        }

        private void Start()
        {
            if (setOptionBar == null) setOptionBar = FindFirstObjectByType<MenuOptionBar>();

            PlayerLoadoutManager loadoutMgr = PlayerLoadoutManager.Instance;
            if (loadoutMgr != null)
            {
                loadoutMgr.BowChanged -= HandleBowChanged;
                loadoutMgr.BowChanged += HandleBowChanged;
                loadoutMgr.ActiveSkillSetChanged -= HandleActiveSkillSetChanged;
                loadoutMgr.ActiveSkillSetChanged += HandleActiveSkillSetChanged;
                loadoutMgr.SkillChanged -= HandleSkillChanged;
                loadoutMgr.SkillChanged += HandleSkillChanged;
                loadoutMgr.LoadoutChanged -= RefreshPedestalSkillIcons;
                loadoutMgr.LoadoutChanged += RefreshPedestalSkillIcons;

                if (characterGear != null && loadoutMgr.EquippedBow != null)
                {
                    characterGear.EquipBow(loadoutMgr.EquippedBow);
                }
                if (setOptionBar != null)
                {
                    setOptionBar.Select(loadoutMgr.ActiveSkillSet, notify: false);
                }
            }

            RefreshPedestalSkillIcons();
            UpdateStandBowLabels(PlayerLoadoutManager.HasInstance ? PlayerLoadoutManager.Instance.EquippedBow : null);
            SetupMenuMusic();
        }

        private void SetupMenuMusic()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

#if UNITY_EDITOR
            if (angelLobbyBgm == null)
                angelLobbyBgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_Lobby_Angel.ogg");
            if (devilLobbyBgm == null)
                devilLobbyBgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_Lobby_Devil.ogg");
#endif

            bool chooseAngel = Random.value < 0.5f;
            AudioClip chosen = chooseAngel ? angelLobbyBgm : devilLobbyBgm;
            float vol = chooseAngel ? angelLobbyVolume : devilLobbyVolume;
            if (chosen == null)
            {
                chosen = chooseAngel ? devilLobbyBgm : angelLobbyBgm;
                vol = chooseAngel ? devilLobbyVolume : angelLobbyVolume;
            }

            if (chosen != null)
            {
                audioSource.clip = chosen;
                audioSource.volume = vol;
                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.BowChanged -= HandleBowChanged;
                PlayerLoadoutManager.Instance.ActiveSkillSetChanged -= HandleActiveSkillSetChanged;
                PlayerLoadoutManager.Instance.SkillChanged -= HandleSkillChanged;
                PlayerLoadoutManager.Instance.LoadoutChanged -= RefreshPedestalSkillIcons;
            }
        }

        private void HandleActiveSkillSetChanged(int newSet)
        {
            RefreshPedestalSkillIcons();
            UpdateStandBowLabels(PlayerLoadoutManager.HasInstance ? PlayerLoadoutManager.Instance.EquippedBow : null);
        }

        private void HandleSkillChanged(int slot, SkillData skill)
        {
            RefreshPedestalSkillIcons();
            UpdateStandBowLabels(PlayerLoadoutManager.HasInstance ? PlayerLoadoutManager.Instance.EquippedBow : null);
        }

        private const float PedestalIconTargetSize = 0.72f;

        public void RefreshPedestalSkillIcons()
        {
            if (pedestalSkillIcons == null || pedestalSkillIcons.Length == 0)
            {
                AutoFindPedestalIcons();
            }

            if (pedestalSkillIcons == null) return;

            PlayerLoadoutManager loadoutMgr = PlayerLoadoutManager.Instance;
            for (int i = 0; i < pedestalSkillIcons.Length; i++)
            {
                SpriteRenderer sr = pedestalSkillIcons[i];
                if (sr == null) continue;

                SkillData skill = loadoutMgr != null ? loadoutMgr.GetEquippedSkill(i) : null;
                if (skill != null && skill.Icon != null)
                {
                    sr.sprite = skill.Icon;
                    sr.gameObject.SetActive(true);

                    // Phóng to và chuẩn hoá kích thước icon sao cho vừa vặn, đầy đặn trong vùng fill màu đen
                    float iconW = skill.Icon.bounds.size.x;
                    float iconH = skill.Icon.bounds.size.y;
                    float maxDim = Mathf.Max(iconW, iconH);
                    if (maxDim > 0.001f)
                    {
                        float scale = PedestalIconTargetSize / maxDim;
                        sr.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                }
                else
                {
                    sr.sprite = null;
                    sr.gameObject.SetActive(false);
                }
            }
        }

        private void AutoFindPedestalIcons()
        {
            GameObject pedestal = GameObject.Find("PedestalBase");
            if (pedestal == null) return;
            Transform container = pedestal.transform.Find("SkillIcons") ?? pedestal.transform;
            var list = new System.Collections.Generic.List<SpriteRenderer>();
            for (int i = 1; i <= 5; i++)
            {
                Transform t = container.Find($"Icon{i}") ?? container.Find($"slot{i}");
                if (t != null)
                {
                    SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null) list.Add(sr);
                }
            }
            if (list.Count > 0) pedestalSkillIcons = list.ToArray();
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
            if (pedestalSkillIcons != null)
            {
                foreach (Collider2D hit in Physics2D.OverlapPointAll(worldPoint))
                {
                    for (int i = 0; i < pedestalSkillIcons.Length; i++)
                    {
                        if (pedestalSkillIcons[i] != null && pedestalSkillIcons[i].gameObject.activeSelf && BelongsToButton(hit, pedestalSkillIcons[i].gameObject))
                        {
                            PlayButtonFeedback(pedestalSkillIcons[i].gameObject);
                            OpenSkillSelection(i);
                            return;
                        }
                    }
                }
            }
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
        public void LoadPVEScene()
        {
            if (pveSceneNames != null && pveSceneNames.Length > 0)
            {
                int index = Random.Range(0, pveSceneNames.Length);
                SceneManager.LoadScene(pveSceneNames[index]);
            }
            else
            {
                SceneManager.LoadScene(pveSceneName);
            }
        }
        public void LoadPVPScene() => SceneManager.LoadScene(pvpSceneName);

        private void HandleBowChanged(BowData newBow)
        {
            if (characterGear != null && newBow != null)
            {
                characterGear.EquipBow(newBow);
            }
            UpdateStandBowLabels(newBow);
        }

        private void AutoFindStandBowLabels()
        {
            if (nameBowObject == null || rareObject == null)
            {
                GameObject stand = GameObject.Find("Stand");
                if (stand != null)
                {
                    var found = BowInfoDisplayUI.FindStandLabels(stand.transform);
                    if (nameBowObject == null) nameBowObject = found.nameObj;
                    if (rareObject == null) rareObject = found.rareObj;
                }
            }
        }

        public void UpdateStandBowLabels(BowData bow = null)
        {
            AutoFindStandBowLabels();
            if (bow == null)
            {
                var mgr = PlayerLoadoutManager.Instance;
                bow = mgr != null ? mgr.EquippedBow : null;
                if (bow == null)
                {
                    var db = Resources.Load<LoadoutDatabase>("LoadoutDatabase");
                    if (db != null && db.Bows != null && db.Bows.Count > 0)
                        bow = db.Bows[0];
                }
            }
            if (bow == null) return;

            BowInfoDisplayUI.ApplyRarityAndName(nameBowObject, rareObject, bow);
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
