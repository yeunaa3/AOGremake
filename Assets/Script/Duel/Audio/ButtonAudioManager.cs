using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AOG.Duel
{
    /// <summary>
    /// Quản lý âm thanh nút bấm trong toàn bộ trò chơi.
    /// Tự động lắng nghe tất cả UI Button và cung cấp API phát âm thanh cho nút 2D.
    /// Có sẵn thanh trượt âm lượng riêng từng file âm thanh trong Inspector.
    /// </summary>
    [AddComponentMenu("AOG/Audio/Button Audio Manager")]
    public sealed class ButtonAudioManager : MonoBehaviour
    {
        public static ButtonAudioManager Instance { get; private set; }

        [Header("Button Audio Clips & Volume Sliders")]
        [Tooltip("Âm thanh bấm nút thông thường trong toàn game")]
        [SerializeField] private AudioClip normalButtonClickClip;
        [Tooltip("Âm lượng nút thông thường")]
        [SerializeField, Range(0f, 1f)] private float normalButtonVolume = 0.8f;

        [Tooltip("Âm thanh khi bấm nút Vào trận / Ghép trận (PVE, PVP, Match)")]
        [SerializeField] private AudioClip matchButtonClickClip;
        [Tooltip("Âm lượng nút Ghép trận")]
        [SerializeField, Range(0f, 1f)] private float matchButtonVolume = 0.8f;

        [Tooltip("Âm thanh khi bấm nút Kho đồ / Đổi trang bị / Bộ sưu tập")]
        [SerializeField] private AudioClip inventoryButtonClickClip;
        [Tooltip("Âm lượng nút Kho đồ")]
        [SerializeField, Range(0f, 1f)] private float inventoryButtonVolume = 0.8f;

        private AudioSource audioSource;
        private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
        private float lastClickTime;
        private const float ClickDebounceSeconds = 0.05f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("ButtonAudioManager");
                Instance = go.AddComponent<ButtonAudioManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D Sound

            LoadDefaultClipsIfMissing();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void Reset()
        {
            LoadDefaultClipsIfMissing();
        }

        private void LoadDefaultClipsIfMissing()
        {
#if UNITY_EDITOR
            if (normalButtonClickClip == null)
            {
                normalButtonClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Audio/Common_Button_Normal.ogg");
            }
            if (matchButtonClickClip == null)
            {
                matchButtonClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Audio/Button/Common_Button_Match.ogg");
            }
            if (inventoryButtonClickClip == null)
            {
                inventoryButtonClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Audio/Button/EFX_Inventory_Button.ogg");
            }
#endif
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            registeredButtons.Clear();
            ScanAndRegisterUIButtons();
        }

        private void Update()
        {
            // 1. Quét định kỳ kiểm tra các Button mới sinh ra trong UI (như modal, popup)
            if (Time.frameCount % 10 == 0)
            {
                ScanAndRegisterUIButtons();
            }

            // 2. Bắt ngay lập tức sự kiện click trên UI thông qua EventSystem Raycast
            CheckPointerClick();
        }

        private void ScanAndRegisterUIButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn != null && registeredButtons.Add(btn))
                {
                    btn.onClick.AddListener(() =>
                    {
                        string lowerName = btn.name.ToLowerInvariant();
                        if (lowerName.Contains("match") || lowerName.Contains("pve") || lowerName.Contains("pvp"))
                        {
                            PlayMatchClick();
                        }
                        else if (lowerName.Contains("deck") || lowerName.Contains("equip") || lowerName.Contains("inventory") || lowerName.Contains("bow") || lowerName.Contains("skill"))
                        {
                            PlayInventoryClick();
                        }
                        else
                        {
                            PlayClick();
                        }
                    });
                }
            }
        }

        private void CheckPointerClick()
        {
            Vector2 screenPos;

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                screenPos = UnityEngine.Input.mousePosition;
            }
            else
            {
                return;
            }

            if (EventSystem.current == null) return;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                Button btn = results[i].gameObject.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    string lowerName = btn.name.ToLowerInvariant();
                    if (lowerName.Contains("match") || lowerName.Contains("pve") || lowerName.Contains("pvp"))
                    {
                        PlayMatchClick();
                    }
                    else if (lowerName.Contains("deck") || lowerName.Contains("equip") || lowerName.Contains("inventory") || lowerName.Contains("bow") || lowerName.Contains("skill"))
                    {
                        PlayInventoryClick();
                    }
                    else
                    {
                        PlayClick();
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Phát âm thanh click nút chuẩn cho bất kỳ nút bấm nào trong game.
        /// </summary>
        public static void PlayClick(AudioClip customClip = null, float? customVolume = null)
        {
            if (Instance == null) return;
            if (Time.unscaledTime - Instance.lastClickTime < ClickDebounceSeconds) return;
            Instance.lastClickTime = Time.unscaledTime;

            AudioClip clip = customClip != null ? customClip : Instance.normalButtonClickClip;
            float vol = customVolume.HasValue ? customVolume.Value : Instance.normalButtonVolume;

            if (clip != null && Instance.audioSource != null)
            {
                Instance.audioSource.PlayOneShot(clip, vol);
            }
        }

        /// <summary>
        /// Phát âm thanh nút ghép trận / chiến đấu.
        /// </summary>
        public static void PlayMatchClick()
        {
            if (Instance == null) return;
            AudioClip clip = Instance.matchButtonClickClip != null ? Instance.matchButtonClickClip : Instance.normalButtonClickClip;
            float vol = Instance.matchButtonClickClip != null ? Instance.matchButtonVolume : Instance.normalButtonVolume;
            PlayClick(clip, vol);
        }

        /// <summary>
        /// Phát âm thanh nút trang bị / kho đồ / kỹ năng.
        /// </summary>
        public static void PlayInventoryClick()
        {
            if (Instance == null) return;
            AudioClip clip = Instance.inventoryButtonClickClip != null ? Instance.inventoryButtonClickClip : Instance.normalButtonClickClip;
            float vol = Instance.inventoryButtonClickClip != null ? Instance.inventoryButtonVolume : Instance.normalButtonVolume;
            PlayClick(clip, vol);
        }
    }
}
