using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    public sealed class KeyInput : InputSource
    {
        [Header("Di chuyển")]
        [SerializeField] private Key moveLeft = Key.A;
        [SerializeField] private Key moveRight = Key.D;

        [Header("5 Kỹ năng")]
        [SerializeField] private Key skill1 = Key.Digit1;
        [SerializeField] private Key skill2 = Key.Digit2;
        [SerializeField] private Key skill3 = Key.Digit3;
        [SerializeField] private Key skill4 = Key.Digit4;
        [SerializeField] private Key skill5 = Key.Digit5;

        // UI đọc trạng thái phím theo đúng cấu hình đang điều khiển nhân vật.
        public bool IsHeld(MobileAction action)
        {
            if (!isActiveAndEnabled || Keyboard.current == null) return false;
            Key key;
            switch (action)
            {
                case MobileAction.MoveLeft: key = moveLeft; break;
                case MobileAction.MoveRight: key = moveRight; break;
                case MobileAction.Skill1: key = skill1; break;
                case MobileAction.Skill2: key = skill2; break;
                case MobileAction.Skill3: key = skill3; break;
                case MobileAction.Skill4: key = skill4; break;
                case MobileAction.Skill5: key = skill5; break;
                default: return false;
            }
            return key != Key.None && Keyboard.current[key].isPressed;
        }

        public override InputCmd ReadCommand()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return default;

            float move = 0f;
            if (keyboard[moveLeft].isPressed) move = -1f;
            if (keyboard[moveRight].isPressed) move = 1f;

            return new InputCmd
            {
                Move = move,
                DashPressed = false,
                ShieldPressed = false,
                Skill1Pressed = keyboard[skill1].wasPressedThisFrame,
                Skill2Pressed = keyboard[skill2].wasPressedThisFrame,
                Skill3Pressed = keyboard[skill3].wasPressedThisFrame,
                Skill4Pressed = keyboard[skill4].wasPressedThisFrame,
                Skill5Pressed = keyboard[skill5].wasPressedThisFrame
            };
        }
    }
}
