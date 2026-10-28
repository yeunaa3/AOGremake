using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AOG.Duel
{
    /// <summary>
    /// Hiển thị đếm ngược 4 giây lúc vào trận:
    /// 4s -> 3s: Số 3
    /// 3s -> 2s: Số 2
    /// 2s -> 1s: Số 1
    /// 1s -> 0s: LET'S ROCK
    /// 0s: Bắt đầu trận đấu.
    /// </summary>
    public sealed class CountdownDisplayUI : MonoBehaviour
    {
        [Header("Manager")]
        [SerializeField] private GameManager matchManager;

        [Header("Sprites Countdown (3, 2, 1, Let's Rock)")]
        [SerializeField] private Sprite count3Sprite;
        [SerializeField] private Sprite count2Sprite;
        [SerializeField] private Sprite count1Sprite;
        [SerializeField] private Sprite letRockSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip countdownAudio;
        [Tooltip("Âm lượng âm thanh đếm 3 2 1")]
        [SerializeField, Range(0f, 1f)] private float countdownVolume = 1f;
        [SerializeField] private AudioClip startAudio;
        [Tooltip("Âm lượng âm thanh Let Rock")]
        [SerializeField, Range(0f, 1f)] private float startVolume = 1f;

        [Header("Scale Settings")]
        [SerializeField] private float numberScale = 1.8f;
        [SerializeField] private float letRockScale = 1.5f;

        [Header("UI Element")]
        [SerializeField] private Image targetImage;

        private AudioSource audioSource;
        private int lastStep = -1;
        private Coroutine punchRoutine;

        private void Awake()
        {
            if (matchManager == null)
            {
                matchManager = GetComponent<GameManager>() ?? FindFirstObjectByType<GameManager>();
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            EnsureTargetImage();
            LoadDefaultResources();
        }

        private void EnsureTargetImage()
        {
            if (targetImage != null)
            {
                targetImage.gameObject.SetActive(false);
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("CountdownDisplayImage");
            if (existing != null)
            {
                targetImage = existing.GetComponent<Image>();
            }
            else
            {
                GameObject go = new GameObject("CountdownDisplayImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(canvas.transform, false);
                go.transform.SetAsLastSibling();

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 60f);

                targetImage = go.GetComponent<Image>();
                targetImage.raycastTarget = false;
            }

            if (targetImage != null)
            {
                targetImage.gameObject.SetActive(false);
            }
        }

        private void LoadDefaultResources()
        {
#if UNITY_EDITOR
            if (count3Sprite == null || count2Sprite == null || count1Sprite == null || letRockSprite == null)
            {
                Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/All_BG_Element/sactx-1024x1024-Uncompressed-Atlas_InGame-f971c844.png");
                if (allAssets != null)
                {
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite s)
                        {
                            if (s.name.EndsWith("_18") && count3Sprite == null) count3Sprite = s;
                            else if (s.name.EndsWith("_19") && count2Sprite == null) count2Sprite = s;
                            else if (s.name.EndsWith("_29") && count1Sprite == null) count1Sprite = s;
                            else if (s.name.EndsWith("_20") && letRockSprite == null) letRockSprite = s;
                        }
                    }
                }
            }

            if (countdownAudio == null)
            {
                countdownAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Match/Ingame_Ready_CountDown.ogg");
            }

            if (startAudio == null)
            {
                startAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Match/Ingame_Ready_Start.ogg");
            }
#endif
        }

        private void OnEnable()
        {
            if (matchManager == null)
            {
                matchManager = GetComponent<GameManager>() ?? FindFirstObjectByType<GameManager>();
            }

            if (matchManager != null)
            {
                matchManager.StateChanged += HandleStateChanged;
                matchManager.CountdownChanged += HandleCountdownChanged;
            }
            lastStep = -1;
        }

        private void OnDisable()
        {
            if (matchManager != null)
            {
                matchManager.StateChanged -= HandleStateChanged;
                matchManager.CountdownChanged -= HandleCountdownChanged;
            }
            if (targetImage != null)
            {
                targetImage.gameObject.SetActive(false);
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Countdown)
            {
                lastStep = -1;
                EnsureTargetImage();
            }
            else
            {
                if (targetImage != null)
                {
                    targetImage.gameObject.SetActive(false);
                }
            }
        }

        private void HandleCountdownChanged(float remaining)
        {
            if (matchManager == null || matchManager.State != GameState.Countdown) return;

            if (targetImage == null) EnsureTargetImage();
            if (targetImage == null) return;

            // remaining chạy từ 4.0 về 0.0:
            // > 3.0s: Step 3 (ảnh "3")
            // 2.0s - 3.0s: Step 2 (ảnh "2")
            // 1.0s - 2.0s: Step 1 (ảnh "1")
            // 0.0s - 1.0s: Step 0 (ảnh "LET'S ROCK")
            int step;
            if (remaining > 3.0f) step = 3;
            else if (remaining > 2.0f) step = 2;
            else if (remaining > 1.0f) step = 1;
            else if (remaining > 0.0f) step = 0;
            else step = -1;

            if (step != lastStep)
            {
                lastStep = step;
                if (step >= 0)
                {
                    ShowStep(step);
                }
                else
                {
                    targetImage.gameObject.SetActive(false);
                }
            }
        }

        private void ShowStep(int step)
        {
            Sprite chosenSprite = null;
            AudioClip clipToPlay = null;
            float baseScale = numberScale;

            switch (step)
            {
                case 3:
                    chosenSprite = count3Sprite;
                    clipToPlay = countdownAudio;
                    baseScale = numberScale;
                    break;
                case 2:
                    chosenSprite = count2Sprite;
                    clipToPlay = countdownAudio;
                    baseScale = numberScale;
                    break;
                case 1:
                    chosenSprite = count1Sprite;
                    clipToPlay = countdownAudio;
                    baseScale = numberScale;
                    break;
                case 0:
                    chosenSprite = letRockSprite;
                    clipToPlay = startAudio;
                    baseScale = letRockScale;
                    break;
            }

            if (chosenSprite != null)
            {
                targetImage.sprite = chosenSprite;
                targetImage.SetNativeSize();
                targetImage.transform.SetAsLastSibling();
                targetImage.gameObject.SetActive(true);

                if (punchRoutine != null) StopCoroutine(punchRoutine);
                punchRoutine = StartCoroutine(PunchScaleRoutine(baseScale));
            }

            if (clipToPlay != null && audioSource != null)
            {
                float clipVol = (clipToPlay == startAudio) ? startVolume : countdownVolume;
                audioSource.PlayOneShot(clipToPlay, clipVol);
            }
        }

        private IEnumerator PunchScaleRoutine(float targetBaseScale)
        {
            if (targetImage == null) yield break;

            Transform tr = targetImage.transform;
            float elapsed = 0f;
            float duration = 0.22f;

            Vector3 startScale = Vector3.one * (targetBaseScale * 1.35f);
            Vector3 endScale = Vector3.one * targetBaseScale;

            tr.localScale = startScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                tr.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
                yield return null;
            }

            tr.localScale = endScale;
            punchRoutine = null;
        }
    }
}
