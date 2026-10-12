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
        Skill4
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
        }

        private bool CanPress => button == null || button.IsInteractable();

        private void LateUpdate()
        {
            bool held = keyboardInput != null && keyboardInput.IsHeld(action);
            if (held && !keyWasHeld && CanPress) keyboardPressed = true;
            if (!held) keyboardPressed = false;
            keyWasHeld = held;
            Vector3 target = normalScale * (pointerHeld || keyboardPressed ? pressedScale : 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, target,
                1f - Mathf.Exp(-pressSpeed * Time.unscaledDeltaTime));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanPress || pointerHeld) return;
            pointerHeld = true;
            heldPointerId = eventData.pointerId;
            if (input == null) return;

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
            pointerHeld = keyboardPressed = keyWasHeld = false;
            ReleaseMovement();
            transform.localScale = normalScale;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            pointerHeld = keyboardPressed = keyWasHeld = false;
            ReleaseMovement();
            transform.localScale = normalScale;
        }
    }
}
