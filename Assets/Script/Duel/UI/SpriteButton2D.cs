using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AOG.Duel
{
    // Nút bấm 2D hỗ trợ hiệu ứng nhún (bounce) và sự kiện click
    public class SpriteButton2D : MonoBehaviour
    {
        public UnityEvent onClick;
        private Vector3 baseScale;

        private void Awake() => baseScale = transform.localScale;

        public void PlayClick()
        {
            if (gameObject.activeInHierarchy) StartCoroutine(BounceRoutine());
            onClick?.Invoke();
        }

        private IEnumerator BounceRoutine()
        {
            transform.localScale = baseScale * 0.92f;
            yield return new WaitForSecondsRealtime(0.08f);
            transform.localScale = baseScale;
        }

        // Hỗ trợ cả khi project dùng legacy input hoặc EventSystem
        private void OnMouseDown() => PlayClick();
    }
}
