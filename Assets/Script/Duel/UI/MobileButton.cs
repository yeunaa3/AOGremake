using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AOG.Duel
{
    public enum MobileAction
    {
        MoveLeft,
        MoveRight,
        Dash,
        Shield,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Skill5
    }

    public sealed class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TouchInput input;
        [SerializeField] private MobileAction action;
        [SerializeField] private KeyInput keyboardInput;
        [Header("Hiệu ứng nhấn")]
        [SerializeField, Range(.5f, 1f)] private float pressedScale = .9f;
        [SerializeField, Min(1f)] private float pressSpeed = 25f;
        private Button button;
        private Vector3 normalScale;
        private bool pointerHeld;
        private int heldPointerId;
        private bool keyWasHeld;
        private bool keyboardPressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            normalScale = transform.localScale;
            if (keyboardInput == null && input != null) keyboardInput = input.GetComponent<KeyInput>();
            if (keyboardInput == null) keyboardInput = FindAnyObjectByType<KeyInput>();
            if (input == null) input = FindAnyObjectByType<TouchInput>();
        }

        private bool CanPress => button == null || button.IsInteractable();

        private void LateUpdate()
        {
            bool keyboardHeld = keyboardInput != null && keyboardInput.IsHeld(action);
            bool isPressed = pointerHeld || keyboardHeld;
            Vector3 target = normalScale * (isPressed ? pressedScale : 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, target,
                1f - Mathf.Exp(-pressSpeed * Time.unscaledDeltaTime));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || pointerHeld) return;
            pointerHeld = true;
            heldPointerId = eventData.pointerId;

            if (!CanPress || input == null) return;

            switch (action)
            {
                case MobileAction.MoveLeft: input.SetLeftHeld(true); break;
                case MobileAction.MoveRight: input.SetRightHeld(true); break;
                case MobileAction.Dash: input.PressDash(); break;
                case MobileAction.Shield: input.PressShield(); break;
                case MobileAction.Skill1: input.PressSkill(0); break;
                case MobileAction.Skill2: input.PressSkill(1); break;
                case MobileAction.Skill3: input.PressSkill(2); break;
                case MobileAction.Skill4: input.PressSkill(3); break;
                case MobileAction.Skill5: input.PressSkill(4); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pointerHeld || eventData.pointerId != heldPointerId) return;
            pointerHeld = false;
            ReleaseMovement();
        }

        private void ReleaseMovement()
        {
            if (input == null) return;
            if (action == MobileAction.MoveLeft) input.SetLeftHeld(false);
            if (action == MobileAction.MoveRight) input.SetRightHeld(false);
        }

        private void OnDisable()
        {
            pointerHeld = false;
            ReleaseMovement();
            transform.localScale = normalScale;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            pointerHeld = false;
            ReleaseMovement();
            transform.localScale = normalScale;
        }
    }
}
