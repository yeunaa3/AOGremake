using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AOG.Duel
{
    // Điều khiển scene chọn trang bị: một scene, hai tab Bow / Skill.
    public sealed class LoadoutSceneController : MonoBehaviour
    {
        public enum LoadoutTab { Bow, Skill }

        private static LoadoutTab requestedTab = LoadoutTab.Bow;

        [Header("World Sprite Objects - không dùng Canvas")]
        [SerializeField] private GameObject bowPanel;
        [SerializeField] private GameObject skillPanel;
        [SerializeField] private GameObject bowTab;
        [SerializeField] private GameObject skillTab;
        [SerializeField] private GameObject backButton;
        [SerializeField] private string menuSceneName = "Menu";

        public static void Open(string sceneName, LoadoutTab tab)
        {
            requestedTab = tab;
            SceneManager.LoadScene(sceneName);
        }

        private void Awake()
        {
        }

        private void Start() => Show(requestedTab);

        public void ShowBow() => Show(LoadoutTab.Bow);
        public void ShowSkill() => Show(LoadoutTab.Skill);
        public void BackToMenu() => SceneManager.LoadScene(menuSceneName);

        private void Show(LoadoutTab tab)
        {
            requestedTab = tab;
            if (bowPanel != null) bowPanel.SetActive(tab == LoadoutTab.Bow);
            if (skillPanel != null) skillPanel.SetActive(tab == LoadoutTab.Skill);
        }

        private void Update()
        {
            if (!TryGetPointerDown(out Vector2 screenPosition)) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 worldPoint = cam.ScreenToWorldPoint(screenPosition);
            foreach (Collider2D hit in Physics2D.OverlapPointAll(worldPoint))
            {
                if (IsPartOf(hit, bowTab)) { ShowBow(); return; }
                if (IsPartOf(hit, skillTab)) { ShowSkill(); return; }
                if (IsPartOf(hit, backButton)) { BackToMenu(); return; }
            }
        }

        private static bool IsPartOf(Collider2D hit, GameObject root)
        {
            return hit != null && root != null &&
                   (hit.transform == root.transform || hit.transform.IsChildOf(root.transform));
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            return false;
        }
    }
}
