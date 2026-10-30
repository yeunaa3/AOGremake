using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AOG.Duel
{
    /// <summary>
    /// Quản lý nhạc nền tiền trận đấu (chạy liên tục qua cả Menu và Loadout).
    /// Khi chưa vào trận (ở Menu hoặc Loadout), nhạc nền chạy liên tục không bị ngắt hay khởi động lại.
    /// Khi vào trận (Map1, Map2...) hoặc ở màn Start, nhạc nền sẽ dừng để nhường cho nhạc trận đấu.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class MenuMusicManager : MonoBehaviour
    {
        private static MenuMusicManager instance;
        public static MenuMusicManager Instance
        {
            get
            {
                if (instance == null)
                {
                    EnsureInstance();
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;

        [Header("Nhạc Nền Menu (Lobby)")]
        [Tooltip("Nhạc nền Lobby Angel (BGM_Lobby_Angel)")]
        [SerializeField] private AudioClip angelLobbyBgm;
        [Tooltip("Âm lượng nhạc nền Angel")]
        [SerializeField, Range(0f, 1f)] private float angelLobbyVolume = 0.5f;

        [Tooltip("Nhạc nền Lobby Devil (BGM_Lobby_Devil)")]
        [SerializeField] private AudioClip devilLobbyBgm;
        [Tooltip("Âm lượng nhạc nền Devil")]
        [SerializeField, Range(0f, 1f)] private float devilLobbyVolume = 0.5f;

        public static void EnsureInstance(AudioClip angelClip = null, AudioClip devilClip = null, float angelVol = 0.5f, float devilVol = 0.5f)
        {
            if (instance != null)
            {
                instance.Configure(angelClip, devilClip, angelVol, devilVol);
                return;
            }

            MenuMusicManager found = FindFirstObjectByType<MenuMusicManager>();
            if (found != null)
            {
                instance = found;
                if (Application.isPlaying) DontDestroyOnLoad(found.gameObject);
                instance.Configure(angelClip, devilClip, angelVol, devilVol);
                return;
            }

            GameObject go = new GameObject("MenuMusicManager");
            instance = go.AddComponent<MenuMusicManager>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
            instance.Configure(angelClip, devilClip, angelVol, devilVol);
        }

        public static void EnsurePlaying()
        {
            if (Instance != null)
            {
                Instance.PlayLobbyMusic();
            }
        }

        public static void StopMusic()
        {
            if (instance != null)
            {
                instance.StopLobbyMusic();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.loop = true;
            audioSource.playOnAwake = false;

            LoadDefaultAssets();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                instance = null;
            }
        }

        private void LoadDefaultAssets()
        {
#if UNITY_EDITOR
            if (angelLobbyBgm == null)
                angelLobbyBgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_Lobby_Angel.ogg");
            if (devilLobbyBgm == null)
                devilLobbyBgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_Lobby_Devil.ogg");
#endif
        }

        public void Configure(AudioClip angelClip, AudioClip devilClip, float angelVol, float devilVol)
        {
            if (angelClip != null) angelLobbyBgm = angelClip;
            if (devilClip != null) devilLobbyBgm = devilClip;
            angelLobbyVolume = angelVol;
            devilLobbyVolume = devilVol;

            LoadDefaultAssets();
            ApplyCurrentVolume();
        }

        public void UpdateVolumes(float angelVol, float devilVol)
        {
            angelLobbyVolume = angelVol;
            devilLobbyVolume = devilVol;
            ApplyCurrentVolume();
        }

        public void PlayLobbyMusic()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            LoadDefaultAssets();

            // Nếu đang phát một trong hai bài nhạc lobby, giữ nguyên không ngắt quãng
            if (audioSource.isPlaying && (audioSource.clip == angelLobbyBgm || audioSource.clip == devilLobbyBgm))
            {
                ApplyCurrentVolume();
                return;
            }

            // Ngẫu nhiên chọn 1 trong 2 bài nhạc Lobby (Angel hoặc Devil)
            bool chooseAngel = UnityEngine.Random.value < 0.5f;
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
                audioSource.Play();
            }
        }

        public void StopLobbyMusic()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private void ApplyCurrentVolume()
        {
            if (audioSource == null || audioSource.clip == null) return;
            if (audioSource.clip == angelLobbyBgm)
            {
                audioSource.volume = angelLobbyVolume;
            }
            else if (audioSource.clip == devilLobbyBgm)
            {
                audioSource.volume = devilLobbyVolume;
            }
        }

        private void OnValidate()
        {
            ApplyCurrentVolume();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPreMatchScene(scene.name))
            {
                // Màn tiền trận đấu: tiếp tục phát nhạc nếu đang phát, hoặc phát nếu chưa phát
                PlayLobbyMusic();
            }
            else
            {
                // Màn trận đấu (Map1, Map2...) hoặc màn Start: dừng nhạc lobby
                StopLobbyMusic();
            }
        }

        public static bool IsPreMatchScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return string.Equals(sceneName, "Menu", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sceneName, "Loadout", StringComparison.OrdinalIgnoreCase);
        }
    }
}
