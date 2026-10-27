using UnityEngine;

namespace AOG.Duel
{
    // Quay toàn bộ 9 bức ảnh họa tiết nền thành một khối đồng nhất quanh tâm trục (0, 0)
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class BackgroundScroller : MonoBehaviour
    {
        [Header("Tốc Độ Quay")]
        [Tooltip("Tốc độ quay của khối 9 ảnh họa tiết nền (độ mỗi giây). Dương = ngược chiều kim đồng hồ, Âm = cùng chiều kim đồng hồ.")]
        [SerializeField] private float rotationSpeed = 8.0f;

        [Header("Tùy Chọn Chạy Trong Editor")]
        [Tooltip("Bật nếu muốn xem hiệu ứng xoay ngay trong Scene view mà không cần ấn Play game")]
        [SerializeField] private bool previewRotateInEditor = false;

        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = value;
        }

        public bool PreviewRotateInEditor
        {
            get => previewRotateInEditor;
            set => previewRotateInEditor = value;
        }

        private void Update()
        {
            if (!Application.isPlaying && !previewRotateInEditor) return;
            if (Mathf.Approximately(rotationSpeed, 0f)) return;

            float deltaAngle = rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, 0f, deltaAngle, Space.Self);
        }
    }
}
