using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    public sealed class KeyInput : InputSource
    {
        [Header("Chỉ số người chơi (0 = P1, 1 = P2)")]
        [SerializeField] private int playerIndex = 0;
        public int PlayerIndex
        {
            get => playerIndex;
            set
            {
                playerIndex = value;
                ReloadKeybindings();
            }
        }

        [Header("Di chuyển")]
        [SerializeField] private Key moveLeft = Key.A;
        [SerializeField] private Key moveRight = Key.D;

        [Header("5 Kỹ năng")]
        [SerializeField] private Key skill1 = Key.Digit1;
        [SerializeField] private Key skill2 = Key.Digit2;
        [SerializeField] private Key skill3 = Key.Digit3;
        [SerializeField] private Key skill4 = Key.Digit4;
        [SerializeField] private Key skill5 = Key.Digit5;

        private void Awake()
        {
            ReloadKeybindings();
        }

        private void OnEnable()
        {
            ReloadKeybindings();
            KeybindingManager.KeybindingsChanged += ReloadKeybindings;
        }

        private void OnDisable()
        {
            KeybindingManager.KeybindingsChanged -= ReloadKeybindings;
        }

        public void ReloadKeybindings()
        {
            moveLeft = KeybindingManager.GetMoveLeft(playerIndex);
            moveRight = KeybindingManager.GetMoveRight(playerIndex);
            skill1 = KeybindingManager.GetSkill(playerIndex, 0);
            skill2 = KeybindingManager.GetSkill(playerIndex, 1);
            skill3 = KeybindingManager.GetSkill(playerIndex, 2);
            skill4 = KeybindingManager.GetSkill(playerIndex, 3);
            skill5 = KeybindingManager.GetSkill(playerIndex, 4);
        }

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
            if (moveLeft != Key.None && keyboard[moveLeft].isPressed) move = -1f;
            if (moveRight != Key.None && keyboard[moveRight].isPressed) move = 1f;

            bool s1 = (skill1 != Key.None && keyboard[skill1].wasPressedThisFrame);
            bool s2 = (skill2 != Key.None && keyboard[skill2].wasPressedThisFrame);
            bool s3 = (skill3 != Key.None && keyboard[skill3].wasPressedThisFrame);
            bool s4 = (skill4 != Key.None && keyboard[skill4].wasPressedThisFrame);
            bool s5 = (skill5 != Key.None && keyboard[skill5].wasPressedThisFrame);

            // Hỗ trợ thêm Numpad phụ cho P2 nếu chưa được gán vào chỗ khác
            if (playerIndex == 1)
            {
                s1 |= keyboard[Key.Numpad1].wasPressedThisFrame;
                s2 |= keyboard[Key.Numpad2].wasPressedThisFrame;
                s3 |= keyboard[Key.Numpad3].wasPressedThisFrame;
                s4 |= keyboard[Key.Numpad4].wasPressedThisFrame;
                s5 |= keyboard[Key.Numpad5].wasPressedThisFrame;
            }

            return new InputCmd
            {
                Move = move,
                DashPressed = false,
                ShieldPressed = false,
                Skill1Pressed = s1,
                Skill2Pressed = s2,
                Skill3Pressed = s3,
                Skill4Pressed = s4,
                Skill5Pressed = s5
            };
        }
    }
}
