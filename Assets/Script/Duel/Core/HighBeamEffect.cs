using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Hiệu ứng cột ánh sáng vút thẳng lên trời (bắt đầu từ aimtarget).
    /// Bắn vút lên trời, giữ sáng rồi mờ dần biến mất từ từ.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HighBeamEffect : MonoBehaviour
    {
        [Tooltip("Thời gian bắn vút lên cao (giây)")]
        [SerializeField] private float shootDuration = 0.18f;

        [Tooltip("Thời gian giữ sáng tối đa (giây)")]
        [SerializeField] private float holdDuration = 0.10f;

        [Tooltip("Thời gian mờ dần biến mất từ từ (giây)")]
        [SerializeField] private float fadeDuration = 0.75f;

        [Tooltip("Độ cao tối đa (Scale Y) khi vút lên trời")]
        [SerializeField] private float targetScaleY = 2.2f;

        [Tooltip("Độ rộng cột sáng (Scale X)")]
        [SerializeField] private float targetScaleX = 0.9f;

        [Tooltip("Màu ánh sáng (mặc định trắng sáng rực rỡ)")]
        [SerializeField] private Color beamColor = new Color(1f, 1f, 1f, 1f);

        private SpriteRenderer sr;
        private float elapsed;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = beamColor;
            }
            transform.localScale = new Vector3(targetScaleX * 0.4f, 0.05f, 1f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (elapsed < shootDuration)
            {
                // 1. Vút nhanh lên trời
                float progress = Mathf.Clamp01(elapsed / shootDuration);
                float easeOut = Mathf.Sin(progress * Mathf.PI * 0.5f);
                float currentScaleY = Mathf.Lerp(0.05f, targetScaleY, easeOut);
                float currentScaleX = targetScaleX * Mathf.Lerp(0.4f, 1.1f, easeOut);
                transform.localScale = new Vector3(currentScaleX, currentScaleY, 1f);

                if (sr != null)
                {
                    sr.color = beamColor;
                }
            }
            else if (elapsed < shootDuration + holdDuration)
            {
                // 2. Giữ nguyên độ cao rực rỡ
                transform.localScale = new Vector3(targetScaleX, targetScaleY, 1f);
                if (sr != null)
                {
                    sr.color = beamColor;
                }
            }
            else
            {
                // 3. Biến mất từ từ (Fade out dần dần)
                float fadeElapsed = elapsed - (shootDuration + holdDuration);
                float fadeT = Mathf.Clamp01(fadeElapsed / fadeDuration);
                float alpha = Mathf.Lerp(beamColor.a, 0f, Mathf.SmoothStep(0f, 1f, fadeT));
                float widthScale = Mathf.Lerp(targetScaleX, targetScaleX * 0.7f, fadeT);
                transform.localScale = new Vector3(widthScale, targetScaleY, 1f);

                if (sr != null)
                {
                    sr.color = new Color(beamColor.r, beamColor.g, beamColor.b, alpha);
                }

                if (fadeElapsed >= fadeDuration)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
