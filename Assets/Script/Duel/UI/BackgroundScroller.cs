using UnityEngine;

namespace AOG.Duel
{
    // Hiệu ứng các hàng họa tiết nền trôi từ từ xuống dưới vô tận
    public class BackgroundScroller : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed = 0.4f;
        [SerializeField] private float rowSpacing = 3.0f;
        [SerializeField] private float wrapBottomY = -5.5f;

        private void Update()
        {
            if (scrollSpeed <= 0f) return;
            float dy = scrollSpeed * Time.deltaTime;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform t = transform.GetChild(i);
                Vector3 pos = t.localPosition;
                pos.y -= dy;
                if (pos.y < wrapBottomY) pos.y += rowSpacing * 3f;
                t.localPosition = pos;
            }
        }
    }
}
