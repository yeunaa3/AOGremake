using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    public sealed class StartSceneController : MonoBehaviour
    {
        [Header("Video Clips")]
        [Tooltip("Video 1: Đoạn intro phát đầu tiên khi vào Start scene.")]
        [SerializeField] private VideoClip video1;

        [Tooltip("Video 2: Đoạn lặp lại kèm chữ 'Press to start'.")]
        [SerializeField] private VideoClip video2;

        [Tooltip("Video 3: Đoạn kết thúc chạy khi người chơi bấm nút, sau đó chuyển sang Menu scene.")]
        [SerializeField] private VideoClip video3;

        [Header("Video Players")]
        [Tooltip("Player A: Phát Video 1 (Intro).")]
        [SerializeField] private VideoPlayer playerA;

        [Tooltip("Player B: Phát Video 2 (Vòng lặp Press to start).")]
        [SerializeField] private VideoPlayer playerB;

        [Tooltip("Player C: Phát Video 3 (Outro chuyển sang Menu).")]
        [SerializeField] private VideoPlayer playerC;

        [Header("UI Video Displays")]
        [SerializeField] private RawImage rawImageA;
        [SerializeField] private RawImage rawImageB;
        [SerializeField] private RawImage rawImageC;
        [SerializeField] private CanvasGroup canvasGroupA;
        [SerializeField] private CanvasGroup canvasGroupB;
        [SerializeField] private CanvasGroup canvasGroupC;

        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;
        [Tooltip("Nhạc nền của Start scene (mặc định BGM_Lobby_Angel).")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("UI - Press To Start")]
        [Tooltip("GameObject UI chứa chữ Press to start (sẽ nhấp nháy ở Video 2).")]
        [SerializeField] private GameObject pressToStartUI;
        [Tooltip("CanvasGroup để làm hiệu ứng nhấp nháy mượt mà cho chữ Press to start.")]
        [SerializeField] private CanvasGroup pressToStartCanvasGroup;
        [SerializeField] private float blinkSpeed = 2.5f;

        [Header("Flow Settings")]
        [Tooltip("Tên scene Menu để chuyển sang sau khi xem xong Video 3.")]
        [SerializeField] private string menuSceneName = "Menu";
        [SerializeField] private bool allowSkipVideo1 = false;

        private enum Phase
        {
            None,
            Video1,
            Video2,
            Video3,
            LoadingMenu
        }

        private Phase currentPhase = Phase.None;
        private bool isTransitioning;
        private float phase3StartTime;

        private RenderTexture renderTextureA;
        private RenderTexture renderTextureB;
        private RenderTexture renderTextureC;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            SetupAudio();
            AutoDiscoverReferences();
            SetupRenderTextures();
            SetupVideoPlayers();
        }

        private void Start()
        {
            // Để pressToStartUI luôn active để TextMeshPro render font và cache vào VRAM ngay từ đầu,
            // tránh giật lag khi chuyển sang Video 2 do SetActive(true) bắt buộc Canvas rebuild.
            if (pressToStartUI != null)
            {
                pressToStartUI.SetActive(true);
            }
            if (pressToStartCanvasGroup != null)
            {
                pressToStartCanvasGroup.alpha = 0f;
            }

            PlayPhase1();
        }

        private void AutoDiscoverReferences()
        {
            if (rawImageA == null || rawImageB == null)
            {
                var foundRawImages = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var ri in foundRawImages)
                {
                    string n = ri.name.ToUpper();
                    if (n.EndsWith("_A") || n.Contains("IMAGE_A") || (n.Contains("A") && !n.Contains("B") && !n.Contains("C")))
                    {
                        if (rawImageA == null) rawImageA = ri;
                    }
                    else if (n.EndsWith("_B") || n.Contains("IMAGE_B") || (n.Contains("B") && !n.Contains("C")))
                    {
                        if (rawImageB == null) rawImageB = ri;
                    }
                    else if (n.EndsWith("_C") || n.Contains("IMAGE_C") || n.Contains("C"))
                    {
                        if (rawImageC == null) rawImageC = ri;
                    }
                }
            }

            if (rawImageA != null && canvasGroupA == null) canvasGroupA = rawImageA.GetComponent<CanvasGroup>();
            if (rawImageA != null && canvasGroupA == null) canvasGroupA = rawImageA.gameObject.AddComponent<CanvasGroup>();

            if (rawImageB != null && canvasGroupB == null) canvasGroupB = rawImageB.GetComponent<CanvasGroup>();
            if (rawImageB != null && canvasGroupB == null) canvasGroupB = rawImageB.gameObject.AddComponent<CanvasGroup>();

            if (pressToStartUI == null)
            {
                var tmps = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var tmp in tmps)
                {
                    if (tmp.name.Contains("Press") || tmp.text.Contains("PRESS"))
                    {
                        pressToStartUI = tmp.gameObject;
                        pressToStartCanvasGroup = tmp.GetComponent<CanvasGroup>();
                        break;
                    }
                }
            }

            if (pressToStartUI != null && pressToStartCanvasGroup == null)
            {
                pressToStartCanvasGroup = pressToStartUI.GetComponent<CanvasGroup>();
                if (pressToStartCanvasGroup == null)
                    pressToStartCanvasGroup = pressToStartUI.AddComponent<CanvasGroup>();
            }
        }

        private void SetupAudio()
        {
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            }

            if (backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.playOnAwake = false;
                if (!musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }
        }

        private void SetupRenderTextures()
        {
            int width = 1920;
            int height = 1008;

            if (video1 != null && video1.width > 0 && video1.height > 0)
            {
                width = (int)video1.width;
                height = (int)video1.height;
            }

            renderTextureA = CreateRT("RT_VideoA", width, height);
            renderTextureB = CreateRT("RT_VideoB", width, height);
            renderTextureC = CreateRT("RT_VideoC", width, height);

            if (rawImageA != null)
            {
                rawImageA.texture = renderTextureA;
                rawImageA.raycastTarget = false;
                rawImageA.enabled = true;
            }
            if (rawImageB != null)
            {
                rawImageB.texture = renderTextureB;
                rawImageB.raycastTarget = false;
                rawImageB.enabled = true;
            }

            // Tạo RawImage C chuyên trách cho Video 3 ngay từ Awake để không phải swap texture lúc đang chạy
            if (rawImageC == null && rawImageA != null)
            {
                GameObject objC = Instantiate(rawImageA.gameObject, rawImageA.transform.parent);
                objC.name = "RawImage_C";
                int siblingIndex = rawImageB != null ? rawImageB.transform.GetSiblingIndex() + 1 : rawImageA.transform.GetSiblingIndex() + 1;
                objC.transform.SetSiblingIndex(siblingIndex);
                rawImageC = objC.GetComponent<RawImage>();
                canvasGroupC = objC.GetComponent<CanvasGroup>();
                if (canvasGroupC == null) canvasGroupC = objC.AddComponent<CanvasGroup>();

                rawImageC.texture = renderTextureC;
                rawImageC.raycastTarget = false;
                rawImageC.enabled = true;
                canvasGroupC.alpha = 0f;
                canvasGroupC.blocksRaycasts = false;
            }
            else if (rawImageC != null)
            {
                rawImageC.texture = renderTextureC;
                rawImageC.raycastTarget = false;
                rawImageC.enabled = true;
                if (canvasGroupC == null) canvasGroupC = rawImageC.GetComponent<CanvasGroup>();
                if (canvasGroupC != null) canvasGroupC.alpha = 0f;
            }
        }

        private RenderTexture CreateRT(string rtName, int w, int h)
        {
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                name = rtName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();
            return rt;
        }

        private void SetupVideoPlayers()
        {
            if (playerA == null)
            {
                var players = GetComponentsInChildren<VideoPlayer>(true);
                if (players.Length > 0) playerA = players[0];
                if (players.Length > 1) playerB = players[1];
                if (players.Length > 2) playerC = players[2];
            }

            if (playerA == null) playerA = gameObject.AddComponent<VideoPlayer>();

            if (playerB == null)
            {
                GameObject objB = new GameObject("VideoPlayer_B");
                objB.transform.SetParent(transform);
                playerB = objB.AddComponent<VideoPlayer>();
            }

            if (playerC == null)
            {
                GameObject objC = new GameObject("VideoPlayer_C");
                objC.transform.SetParent(transform);
                playerC = objC.AddComponent<VideoPlayer>();
            }

            ConfigurePlayer(playerA, renderTextureA);
            ConfigurePlayer(playerB, renderTextureB);
            ConfigurePlayer(playerC, renderTextureC);

            playerA.loopPointReached += OnPlayerALoopPoint;
            playerC.loopPointReached += OnPlayerCLoopPoint;
        }

        private void ConfigurePlayer(VideoPlayer vp, RenderTexture target)
        {
            if (vp == null) return;
            vp.playOnAwake = false;
            vp.waitForFirstFrame = false;
            vp.skipOnDrop = false;
            vp.timeReference = VideoTimeReference.InternalTime;
            vp.playbackSpeed = 1f;

            if (target != null && (rawImageA != null || rawImageB != null))
            {
                vp.renderMode = VideoRenderMode.RenderTexture;
                vp.targetTexture = target;
            }
            else
            {
                vp.renderMode = VideoRenderMode.CameraNearPlane;
                vp.targetCamera = Camera.main;
                vp.aspectRatio = VideoAspectRatio.FitInside;
            }

            vp.audioOutputMode = VideoAudioOutputMode.None;
        }

        private void PlayPhase1()
        {
            currentPhase = Phase.Video1;
            isTransitioning = false;

            SetDisplayVisibility(showA: true, showB: false, showC: false);

            if (pressToStartCanvasGroup != null)
                pressToStartCanvasGroup.alpha = 0f;

            // 1. Cài đặt VideoClips
            if (playerA != null && video1 != null)
            {
                playerA.clip = video1;
                playerA.isLooping = false;
            }

            if (playerB != null && video2 != null)
            {
                playerB.clip = video2;
                playerB.isLooping = true;
            }

            if (playerC != null && video3 != null)
            {
                playerC.clip = video3;
                playerC.isLooping = false;
            }

            StartCoroutine(PrepareAndPlayRoutine());
        }

        private IEnumerator PrepareAndPlayRoutine()
        {
            // Nạp Video 1 trước để phát ngay từ frame 0 mà không bao giờ bị khựng lúc mở đầu
            if (playerA != null && video1 != null)
            {
                if (!playerA.isPrepared)
                {
                    playerA.Prepare();
                    while (!playerA.isPrepared)
                    {
                        yield return null;
                    }
                }
                playerA.Play();
            }
            else
            {
                SwitchToPhase2();
                yield break;
            }

            // Chờ 0.5s cho Video 1 chạy mượt mà ổn định rồi mới nạp Video 2
            yield return new WaitForSeconds(0.5f);

            if (playerB != null && video2 != null && !playerB.isPrepared)
            {
                playerB.Prepare();
                while (!playerB.isPrepared)
                {
                    yield return null;
                }
            }

            // Nạp tiếp Video 3
            if (playerC != null && video3 != null && !playerC.isPrepared)
            {
                playerC.Prepare();
            }
        }

        private void SwitchToPhase2()
        {
            if (currentPhase != Phase.Video1) return;
            currentPhase = Phase.Video2;
            isTransitioning = false;

            // 1. Phát ngay Player B (Video 2 đã nạp sẵn khung hình 0)
            if (playerB != null)
            {
                playerB.Play();
            }

            // 2. Chuyển quyền hiển thị sang B ngay lập tức (không delay, không rebuild canvas)
            SetDisplayVisibility(showA: false, showB: true, showC: false);

            // 3. Tạm dừng Player A thay vì Stop để không bị stall decoder và không drop frame Video 2
            if (playerA != null)
            {
                playerA.Pause();
            }

            // 4. Bật hiển thị chữ PRESS TO START qua CanvasGroup alpha (tuyệt đối không dùng SetActive để không lag canvas)
            if (pressToStartCanvasGroup != null)
            {
                pressToStartCanvasGroup.alpha = 0.3f;
            }
        }

        private void SwitchToPhase3()
        {
            if (isTransitioning || currentPhase != Phase.Video2) return;
            isTransitioning = true;
            currentPhase = Phase.Video3;
            phase3StartTime = Time.time;

            // Ẩn chữ PRESS TO START
            if (pressToStartCanvasGroup != null)
                pressToStartCanvasGroup.alpha = 0f;

            // 1. Phát ngay Player C (Video 3 đã nạp sẵn từ Phase 1 trên RawImage C)
            if (playerC != null)
            {
                playerC.Play();
            }

            // 2. Chuyển quyền hiển thị sang C ngay lập tức bằng CanvasGroup
            SetDisplayVisibility(showA: false, showB: false, showC: true);

            // 3. Tạm dừng Player B
            if (playerB != null)
            {
                playerB.Pause();
            }
        }

        private void SetDisplayVisibility(bool showA, bool showB, bool showC)
        {
            if (canvasGroupA != null)
            {
                canvasGroupA.alpha = showA ? 1f : 0f;
                canvasGroupA.blocksRaycasts = false;
            }

            if (canvasGroupB != null)
            {
                canvasGroupB.alpha = showB ? 1f : 0f;
                canvasGroupB.blocksRaycasts = false;
            }

            if (canvasGroupC != null)
            {
                canvasGroupC.alpha = showC ? 1f : 0f;
                canvasGroupC.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (currentPhase == Phase.Video1)
            {
                if (allowSkipVideo1 && CheckAnyInput())
                {
                    SwitchToPhase2();
                }
            }
            else if (currentPhase == Phase.Video2)
            {
                if (pressToStartCanvasGroup != null)
                {
                    float alpha = Mathf.PingPong(Time.time * blinkSpeed, 0.7f) + 0.3f;
                    pressToStartCanvasGroup.alpha = alpha;
                }

                if (CheckAnyInput())
                {
                    SwitchToPhase3();
                }
            }
            else if (currentPhase == Phase.Video3)
            {
                // Cho phép bấm phím để bỏ qua Video 3 CHỈ KHI đã phát được ít nhất 0.8 giây
                // (Ngăn chặn triệt để cú click từ Video 2 ăn lẹm sang làm mất Video 3 ngay lập tức)
                if (Time.time - phase3StartTime > 0.8f && CheckAnyInput())
                {
                    LoadMenuScene();
                }
            }
        }

        public void OnScreenClicked()
        {
            if (currentPhase == Phase.Video1)
            {
                if (allowSkipVideo1) SwitchToPhase2();
            }
            else if (currentPhase == Phase.Video2)
            {
                SwitchToPhase3();
            }
            else if (currentPhase == Phase.Video3)
            {
                // Chỉ nhận click bỏ qua nếu đã xem ít nhất 0.8 giây
                if (Time.time - phase3StartTime > 0.8f)
                {
                    LoadMenuScene();
                }
            }
        }

        private void OnPlayerALoopPoint(VideoPlayer source)
        {
            if (currentPhase == Phase.Video1)
            {
                SwitchToPhase2();
            }
        }

        private void OnPlayerCLoopPoint(VideoPlayer source)
        {
            if (currentPhase == Phase.Video3)
            {
                LoadMenuScene();
            }
        }

        private void LoadMenuScene()
        {
            if (currentPhase == Phase.LoadingMenu) return;
            currentPhase = Phase.LoadingMenu;

            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            if (!string.IsNullOrEmpty(menuSceneName))
            {
                SceneManager.LoadScene(menuSceneName);
            }
            else
            {
                Debug.LogWarning("[StartSceneController] menuSceneName is empty!", this);
            }
        }

        private bool CheckAnyInput()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;

            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                return true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
                return true;

            return false;
        }

        private void OnDestroy()
        {
            if (playerA != null)
            {
                playerA.loopPointReached -= OnPlayerALoopPoint;
            }
            if (playerC != null)
            {
                playerC.loopPointReached -= OnPlayerCLoopPoint;
            }

            DestroyRT(ref renderTextureA);
            DestroyRT(ref renderTextureB);
            DestroyRT(ref renderTextureC);
        }

        private void DestroyRT(ref RenderTexture rt)
        {
            if (rt != null)
            {
                if (rt.IsCreated()) rt.Release();
                Destroy(rt);
                rt = null;
            }
        }
    }
}
