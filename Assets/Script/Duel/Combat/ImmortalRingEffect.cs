using UnityEngine;

namespace AOG.Duel
{
    // Hiệu ứng vòng tròn ánh sáng vàng nháy sáng lan tỏa khi kích hoạt chiêu Bất Tử (Immortal)
    [DisallowMultipleComponent]
    public sealed class ImmortalRingEffect : MonoBehaviour
    {
        [Header("Cấu Hình Hiệu Ứng")]
        [SerializeField] private float duration = 0.55f;
        [SerializeField] private float startScale = 0.2f;
        [SerializeField] private float endScale = 1.25f;
        [SerializeField] private Color flashColor = new Color(1f, 0.96f, 0.5f, 1f);
        [SerializeField] private Color fadeColor = new Color(1f, 0.75f, 0.1f, 0f);
        [SerializeField] private SpriteRenderer ringRenderer;

        private SpriteRenderer innerRenderer;
        private float elapsed;

        private void Awake()
        {
            // Tách khỏi parent để luôn ở world space, không bị lật scale hay ảnh hưởng bởi animation của nhân vật
            transform.SetParent(null, true);
            transform.position += Vector3.up * 0.45f;
            transform.localScale = Vector3.one * startScale;

            if (ringRenderer == null)
            {
                ringRenderer = GetComponent<SpriteRenderer>();
            }

            if (ringRenderer != null)
            {
                ringRenderer.sortingLayerName = "Default";
                ringRenderer.sortingOrder = 50; // Luôn hiển thị nổi bật trên toàn bộ nhân vật và nền
                ringRenderer.color = flashColor;

                // Tạo thêm 1 vòng sáng phụ bên trong (lõi sáng trắng vàng) để tạo độ dày và bừng sáng rực rỡ
                GameObject innerObj = new GameObject("InnerGlowRing");
                innerObj.transform.SetParent(transform, false);
                innerObj.transform.localPosition = Vector3.zero;
                innerObj.transform.localScale = Vector3.one * 0.75f;
                innerRenderer = innerObj.AddComponent<SpriteRenderer>();
                innerRenderer.sprite = ringRenderer.sprite;
                innerRenderer.sharedMaterial = ringRenderer.sharedMaterial;
                innerRenderer.sortingLayerName = "Default";
                innerRenderer.sortingOrder = 51;
                innerRenderer.color = Color.white;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Easing Out Quart cho độ bừng sáng dãn nở cực nhanh lúc đầu rồi nở rộng êm dịu
            float scaleT = 1f - Mathf.Pow(1f - t, 4f);
            float currentScale = Mathf.Lerp(startScale, endScale, scaleT);
            transform.localScale = new Vector3(currentScale, currentScale, 1f);

            // Nháy sáng cực đại lúc bắt đầu và mờ dần về cuối
            if (ringRenderer != null)
            {
                ringRenderer.color = Color.Lerp(flashColor, fadeColor, t);
            }
            if (innerRenderer != null)
            {
                Color innerFlash = Color.white;
                Color innerFade = new Color(1f, 0.9f, 0.4f, 0f);
                innerRenderer.color = Color.Lerp(innerFlash, innerFade, t);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
