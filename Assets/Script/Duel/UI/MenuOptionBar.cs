using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    // Quản lý thanh ô Option: Ẩn/Hiện viền và chữ phát sáng khi bấm chọn (hỗ trợ cả thanh 4 set và thanh 2 tab).
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

        [Header("Danh Sách Ô Lựa Chọn (Option Slots)")]
        public OptionSlot[] slots = new OptionSlot[4];
        public int selectedIndex = 0;

        [Header("Đồng Bộ Skill Set")]
        [Tooltip("Tự động đồng bộ với PlayerLoadoutManager (dành cho thanh 4 set kỹ năng)")]
        [SerializeField] private bool syncWithSkillSet = false;

        public bool SyncWithSkillSet
        {
            get => syncWithSkillSet;
            set => syncWithSkillSet = value;
        }

        public event Action<int> OptionSelected;

        private void Awake()
        {
            // Tự động nhận diện số slot thực tế nếu chưa được cấu hình
            if (slots == null || slots.Length == 0)
            {
                int detected = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (transform.Find($"slot{i + 1}") != null) detected++;
                    else break;
                }
                slots = new OptionSlot[Mathf.Max(2, detected > 0 ? detected : 4)];
            }

            for (int i = 0; i < slots.Length; i++)
            {
                Transform t = transform.Find($"slot{i + 1}");
                if (t == null) continue;

                var s = slots[i] ??= new OptionSlot();

                // Tự động nhận diện các thành phần con nếu chưa được kéo vào Inspector
                if (s.border == null && t.childCount > 0) s.border = t.GetChild(0).gameObject;
                if (s.glowingText == null && t.childCount > 1) s.glowingText = t.GetChild(1).gameObject;
                if (s.unlitText == null && t.childCount > 2) s.unlitText = t.GetChild(2).gameObject;

                EnsureCollider(t.gameObject);
                EnsureCollider(s.border);
                EnsureCollider(s.glowingText);
                EnsureCollider(s.unlitText);
            }

            // Tự động kích hoạt đồng bộ nếu là thanh 4 set kỹ năng (thanh Option của Skill Sets)
            if (!syncWithSkillSet && slots != null && slots.Length == 4)
            {
                syncWithSkillSet = true;
            }
        }

        private void Start()
        {
            if (syncWithSkillSet)
            {
                var mgr = PlayerLoadoutManager.Instance;
                if (mgr != null)
                {
                    selectedIndex = mgr.ActiveSkillSet;
                    mgr.ActiveSkillSetChanged -= OnActiveSkillSetChanged;
                    mgr.ActiveSkillSetChanged += OnActiveSkillSetChanged;
                }
            }

            Select(selectedIndex, notify: false);
        }

        private void OnDestroy()
        {
            if (syncWithSkillSet && PlayerLoadoutManager.HasInstance)
            {
                PlayerLoadoutManager.Instance.ActiveSkillSetChanged -= OnActiveSkillSetChanged;
            }
        }

        private void OnActiveSkillSetChanged(int newSetIndex)
        {
            if (newSetIndex >= 0 && slots != null && newSetIndex < slots.Length && selectedIndex != newSetIndex)
            {
                Select(newSetIndex, notify: false);
            }
        }

        private void Update()
        {
            if (TryGetPointerDown(out Vector2 screenPos))
            {
                Camera cam = Camera.main;
                if (cam == null) return;

                Vector2 worldPoint = cam.ScreenToWorldPoint(screenPos);
                TryHandlePointerClick(worldPoint);
            }
        }

        public bool TryHandlePointerClick(Vector2 worldPoint)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            foreach (Collider2D hit in hits)
            {
                if (hit != null && TrySelectByHit(hit))
                {
                    return true;
                }
            }

            // Fallback kiểm tra Bounds trực tiếp của SpriteRenderer nếu Collider chưa kích hoạt
            for (int i = 0; i < slots.Length; i++)
            {
                Transform t = transform.Find($"slot{i + 1}");
                if (t == null) continue;

                var s = slots[i];
                if (IsPointInTransformOrRenderers(t, s, worldPoint))
                {
                    Select(i, notify: true);
                    return true;
                }
            }

            return false;
        }

        private static bool IsPointInTransformOrRenderers(Transform t, OptionSlot s, Vector2 point)
        {
            if (t == null) return false;

            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null && sr.bounds.Contains(point)) return true;

            if (s != null)
            {
                if (s.border != null && s.border.activeSelf)
                {
                    var bsr = s.border.GetComponent<SpriteRenderer>();
                    if (bsr != null && bsr.bounds.Contains(point)) return true;
                }
                if (s.unlitText != null && s.unlitText.activeSelf)
                {
                    var usr = s.unlitText.GetComponent<SpriteRenderer>();
                    if (usr != null && usr.bounds.Contains(point)) return true;
                }
            }

            // Fallback bounds
            Bounds b = new Bounds(t.position, new Vector3(0.8f, 0.8f, 1f));
            return b.Contains(point);
        }

        public bool TrySelectByHit(Collider2D hit)
        {
            if (hit == null) return false;

            for (int i = 0; i < slots.Length; i++)
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
                    Select(i, notify: true);
                    return true;
                }
            }
            return false;
        }

        public void Select(int index, bool notify = true)
        {
            if (slots == null || index < 0 || index >= slots.Length) return;

            bool changed = (selectedIndex != index);
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

            if (changed && notify)
            {
                if (syncWithSkillSet && PlayerLoadoutManager.Instance != null)
                {
                    PlayerLoadoutManager.Instance.SetActiveSkillSet(selectedIndex);
                }

                OptionSelected?.Invoke(selectedIndex);
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
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                screenPos = UnityEngine.Input.mousePosition;
                return true;
            }
            return false;
        }
    }
}
