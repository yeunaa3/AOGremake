using UnityEngine;

namespace AOG.Duel
{
    /// <summary>
    /// Thanh máu mini gắn trực tiếp trong Prefab của từng vật thể triệu hồi (SkillObject).
    /// Chỉ gồm 1 khung nền BG và 1 thanh Fill.
    /// Người dùng có thể tự do căn chỉnh vị trí, kích thước, màu sắc trực tiếp trong Unity Inspector/Scene view.
    /// </summary>
    [ExecuteAlways]
    public class MiniHealthBar : MonoBehaviour
    {
        [Header("Liên Kết Renderers (Renderers Binding)")]
        [Tooltip("Renderer khung nền BG")]
        [SerializeField] private SpriteRenderer bgRenderer;
        [Tooltip("Renderer thanh máu Fill (co giãn theo % HP)")]
        [SerializeField] private SpriteRenderer fillRenderer;

        [Header("Kích Thước Thanh Máu")]
        [Tooltip("Chiều rộng của thanh máu Fill")]
        [SerializeField] private float barWidth = 0.85f;
        [Tooltip("Chiều cao của thanh máu Fill")]
        [SerializeField] private float barHeight = 0.11f;

        [Header("Màu Sắc Thanh Máu")]
        [Tooltip("Màu của thanh máu Fill")]
        [SerializeField] private Color fillColor = new Color(0.8784314f, 0.21385412f, 0.19999997f, 1f);

        [Header("Tùy Chọn Khóa Hướng")]
        [Tooltip("Luôn giữ thanh máu nằm ngang và không bị lật ngược khi cha xoay tròn hoặc lật hướng (scale.x âm)")]
        [SerializeField] private bool lockWorldOrientation = true;

        private float currentPercent = 1f;
        private float targetPercent = 1f;
        private Vector3 initialBaseScale = Vector3.one;

        public float BarWidth { get => barWidth; set => barWidth = value; }
        public float BarHeight { get => barHeight; set => barHeight = value; }
        public Color FillColor { get => fillColor; set => fillColor = value; }

        private void Awake()
        {
            initialBaseScale = transform.localScale;
            AutoFindRenderers();
        }

        private void Start()
        {
            AutoFindRenderers();
            ApplyFill();
        }

        public void AutoFindRenderers()
        {
            if (bgRenderer == null)
            {
                Transform bg = transform.Find("Bg");
                if (bg != null) bgRenderer = bg.GetComponent<SpriteRenderer>();
            }
            if (fillRenderer == null)
            {
                Transform fill = transform.Find("Fill");
                if (fill != null) fillRenderer = fill.GetComponent<SpriteRenderer>();
            }
        }

        public void SetHealth(float current, float max)
        {
            targetPercent = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        public void SetInstantHealth(float current, float max)
        {
            targetPercent = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            currentPercent = targetPercent;
            ApplyFill();
        }

        private void LateUpdate()
        {
            if (lockWorldOrientation && Application.isPlaying)
            {
                // Giữ góc quay luôn thẳng đứng độc lập với cha (không bị xoay tròn khi con lăn lăn)
                transform.rotation = Quaternion.identity;

                // Bù trừ nếu cha bị lật scale âm (ví dụ scale.x âm khi đổi hướng nhìn)
                if (transform.parent != null)
                {
                    float pSignX = Mathf.Sign(transform.parent.lossyScale.x);
                    float pSignY = Mathf.Sign(transform.parent.lossyScale.y);
                    Vector3 s = transform.localScale;
                    s.x = Mathf.Abs(initialBaseScale.x) * (pSignX < 0f ? -1f : 1f);
                    s.y = Mathf.Abs(initialBaseScale.y) * (pSignY < 0f ? -1f : 1f);
                    transform.localScale = s;
                }
            }

            // Co giãn thanh máu mượt mà trong chế độ chơi
            if (Application.isPlaying)
            {
                if (Mathf.Abs(currentPercent - targetPercent) > 0.001f)
                {
                    currentPercent = Mathf.MoveTowards(currentPercent, targetPercent, Time.deltaTime * 4f);
                    ApplyFill();
                }
            }
            else
            {
                ApplyFill();
            }
        }

        public void ApplyFill()
        {
            if (fillRenderer == null) AutoFindRenderers();
            if (fillRenderer == null) return;

            // Tính toán co giãn từ trái sang phải cho mọi loại pivot
            float width = barWidth * currentPercent;
            Transform fTrans = fillRenderer.transform;
            fTrans.localScale = new Vector3(width, barHeight, 1f);
            fTrans.localPosition = new Vector3(-barWidth * 0.5f + (width * 0.5f), 0f, 0f);

            // Giữ màu cố định không đổi màu mid/low theo yêu cầu
            fillRenderer.color = fillColor;
        }

        private void OnValidate()
        {
            AutoFindRenderers();
            ApplyFill();
        }
    }
}
