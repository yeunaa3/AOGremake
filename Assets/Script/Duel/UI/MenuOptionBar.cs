using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    // Quản lý thanh 4 ô Option: Ẩn/Hiện các GameObject được kéo vào khi bấm chọn
    public class MenuOptionBar : MonoBehaviour
    {
        [System.Serializable]
        public class OptionSlot
        {
            [Header("Hiện khi CHỌN - Ẩn khi BỎ CHỌN")]
            [Tooltip("Object viền sáng")]
            public GameObject border;

            [Tooltip("Object chữ/ảnh phát sáng")]
            public GameObject glowingText;

            [Header("Ẩn khi CHỌN - Hiện khi BỎ CHỌN")]
            [Tooltip("Object chữ/ảnh thường chưa sáng")]
            public GameObject unlitText;
        }

        [Header("4 Ô Lựa Chọn (Option Slots)")]
        public OptionSlot[] slots = new OptionSlot[4];
        public int selectedIndex = 0;

        private void Awake()
        {
            if (slots == null || slots.Length != 4) slots = new OptionSlot[4];

            for (int i = 0; i < 4; i++)
            {
                Transform t = transform.Find($"slot{i + 1}");
                if (t == null) continue;

                var s = slots[i] ??= new OptionSlot();

                EnsureCollider(t.gameObject);
                EnsureCollider(s.border);
                EnsureCollider(s.glowingText);
                EnsureCollider(s.unlitText);
            }
        }

        private void Start() => Select(selectedIndex);

        private void Update()
        {
            // Bắt sự kiện click chuột / chạm bằng New Input System
            if (TryGetPointerDown(out Vector2 screenPos))
            {
                Camera cam = Camera.main;
                if (cam == null) return;

                Vector2 worldPoint = cam.ScreenToWorldPoint(screenPos);
                Collider2D hit = Physics2D.OverlapPoint(worldPoint);
                if (hit != null)
                {
                    TrySelectByHit(hit);
                }
            }
        }

        public bool TrySelectByHit(Collider2D hit)
        {
            if (hit == null) return false;

            for (int i = 0; i < 4; i++)
            {
                Transform t = transform.Find($"slot{i + 1}");
                var s = (slots != null && i < slots.Length) ? slots[i] : null;

                bool isHit = (t != null && (hit.transform == t || hit.transform.IsChildOf(t)));
                if (!isHit && s != null)
                {
                    if (s.border != null && (hit.gameObject == s.border || hit.transform.IsChildOf(s.border.transform))) isHit = true;
                    if (s.glowingText != null && (hit.gameObject == s.glowingText || hit.transform.IsChildOf(s.glowingText.transform))) isHit = true;
                    if (s.unlitText != null && (hit.gameObject == s.unlitText || hit.transform.IsChildOf(s.unlitText.transform))) isHit = true;
                }

                if (isHit)
                {
                    Select(i);
                    return true;
                }
            }
            return false;
        }

        public void Select(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return;
            selectedIndex = index;

            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null) continue;

                bool isSelected = (i == index);

                if (s.border != null) s.border.SetActive(isSelected);
                if (s.glowingText != null) s.glowingText.SetActive(isSelected);
                if (s.unlitText != null) s.unlitText.SetActive(!isSelected);
            }
        }

        private static void EnsureCollider(GameObject go)
        {
            if (go == null) return;
            if (go.GetComponent<Collider2D>() == null)
            {
                var box = go.AddComponent<BoxCollider2D>();
                var sr = go.GetComponent<SpriteRenderer>();
                box.size = (sr != null && sr.sprite != null) ? (Vector2)sr.sprite.bounds.size : new Vector2(0.6f, 0.6f);
            }
        }

        private static bool TryGetPointerDown(out Vector2 screenPos)
        {
            screenPos = Vector2.zero;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            return false;
        }
    }
}
