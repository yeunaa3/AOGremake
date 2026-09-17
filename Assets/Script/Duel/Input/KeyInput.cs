using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    public sealed class KeyInput : InputSource
    {
        [Header("Di chuyển")]
        [SerializeField] private Key moveLeft = Key.A;
        [SerializeField] private Key moveRight = Key.D;

        [Header("Hành động")]
        [SerializeField] private Key dash = Key.Space;
        [SerializeField] private Key shield = Key.LeftShift;
        [SerializeField] private Key skill1 = Key.Digit1;
        [SerializeField] private Key skill2 = Key.Digit2;
        [SerializeField] private Key skill3 = Key.Digit3;
        [SerializeField] private Key skill4 = Key.Digit4;

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
                DashPressed = keyboard[dash].wasPressedThisFrame,
                ShieldPressed = keyboard[shield].wasPressedThisFrame,
                Skill1Pressed = keyboard[skill1].wasPressedThisFrame,
                Skill2Pressed = keyboard[skill2].wasPressedThisFrame,
                Skill3Pressed = keyboard[skill3].wasPressedThisFrame,
                Skill4Pressed = keyboard[skill4].wasPressedThisFrame
            };
        }
    }
}