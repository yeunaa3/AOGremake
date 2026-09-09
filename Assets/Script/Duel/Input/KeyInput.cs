using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AOG.Duel
{
    public sealed class KeyInput : InputSource
    {
#if ENABLE_INPUT_SYSTEM
        [Header("Movement")]
        [SerializeField] private Key moveLeft = Key.A;
        [SerializeField] private Key moveRight = Key.D;

        [Header("Actions")]
        [SerializeField] private Key dash = Key.Space;
        [SerializeField] private Key shield = Key.LeftShift;
        [SerializeField] private Key skill1 = Key.Digit1;
        [SerializeField] private Key skill2 = Key.Digit2;
        [SerializeField] private Key skill3 = Key.Digit3;
        [SerializeField] private Key skill4 = Key.Digit4;
#else
        [Header("Movement")]
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;

        [Header("Actions")]
        [SerializeField] private KeyCode dash = KeyCode.Space;
        [SerializeField] private KeyCode shield = KeyCode.LeftShift;
        [SerializeField] private KeyCode skill1 = KeyCode.Alpha1;
        [SerializeField] private KeyCode skill2 = KeyCode.Alpha2;
        [SerializeField] private KeyCode skill3 = KeyCode.Alpha3;
        [SerializeField] private KeyCode skill4 = KeyCode.Alpha4;
#endif

        public override InputCmd ReadCommand()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return default;

            float move = 0f;
            if (keyboard[moveLeft].isPressed) move -= 1f;
            if (keyboard[moveRight].isPressed) move += 1f;

            return new InputCmd
            {
                Move = Mathf.Clamp(move, -1f, 1f),
                DashPressed = keyboard[dash].wasPressedThisFrame,
                ShieldPressed = keyboard[shield].wasPressedThisFrame,
                Skill1Pressed = keyboard[skill1].wasPressedThisFrame,
                Skill2Pressed = keyboard[skill2].wasPressedThisFrame,
                Skill3Pressed = keyboard[skill3].wasPressedThisFrame,
                Skill4Pressed = keyboard[skill4].wasPressedThisFrame
            };
#else
            float move = 0f;
            if (Input.GetKey(moveLeft)) move -= 1f;
            if (Input.GetKey(moveRight)) move += 1f;

            return new InputCmd
            {
                Move = Mathf.Clamp(move, -1f, 1f),
                DashPressed = Input.GetKeyDown(dash),
                ShieldPressed = Input.GetKeyDown(shield),
                Skill1Pressed = Input.GetKeyDown(skill1),
                Skill2Pressed = Input.GetKeyDown(skill2),
                Skill3Pressed = Input.GetKeyDown(skill3),
                Skill4Pressed = Input.GetKeyDown(skill4)
            };
#endif
        }
    }
}
