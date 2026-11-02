using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AOG.Duel
{
    // Quản lý gán phím bàn phím cho Người chơi 1 và Người chơi 2 (Local PVP)
    public static class KeybindingManager
    {
        private const string PrefPrefix = "AOG_Key_";

        public static event Action KeybindingsChanged;

        // Phím mặc định Người chơi 1 (Bên trái bàn phím)
        public static readonly Key DefaultP1MoveLeft = Key.A;
        public static readonly Key DefaultP1MoveRight = Key.D;
        public static readonly Key[] DefaultP1Skills = new Key[]
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5
        };

        // Phím mặc định Người chơi 2 (Bên phải bàn phím - tiện cả bàn phím thường lẫn laptop không có Numpad)
        public static readonly Key DefaultP2MoveLeft = Key.LeftArrow;
        public static readonly Key DefaultP2MoveRight = Key.RightArrow;
        public static readonly Key[] DefaultP2Skills = new Key[]
        {
            Key.J, Key.K, Key.L, Key.U, Key.I
        };

        // Numpad phụ mặc định cho P2 (nếu máy có bàn phím số Numpad)
        public static readonly Key[] AltP2Skills = new Key[]
        {
            Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5
        };

        public static Key GetMoveLeft(int playerIndex)
        {
            Key defaultKey = playerIndex == 0 ? DefaultP1MoveLeft : DefaultP2MoveLeft;
            return (Key)PlayerPrefs.GetInt($"{PrefPrefix}P{playerIndex}_MoveLeft", (int)defaultKey);
        }

        public static Key GetMoveRight(int playerIndex)
        {
            Key defaultKey = playerIndex == 0 ? DefaultP1MoveRight : DefaultP2MoveRight;
            return (Key)PlayerPrefs.GetInt($"{PrefPrefix}P{playerIndex}_MoveRight", (int)defaultKey);
        }

        public static Key GetSkill(int playerIndex, int slot)
        {
            slot = Mathf.Clamp(slot, 0, 4);
            Key defaultKey = playerIndex == 0 ? DefaultP1Skills[slot] : DefaultP2Skills[slot];
            return (Key)PlayerPrefs.GetInt($"{PrefPrefix}P{playerIndex}_Skill_{slot}", (int)defaultKey);
        }

        public static void SetMoveLeft(int playerIndex, Key key)
        {
            PlayerPrefs.SetInt($"{PrefPrefix}P{playerIndex}_MoveLeft", (int)key);
            PlayerPrefs.Save();
            KeybindingsChanged?.Invoke();
        }

        public static void SetMoveRight(int playerIndex, Key key)
        {
            PlayerPrefs.SetInt($"{PrefPrefix}P{playerIndex}_MoveRight", (int)key);
            PlayerPrefs.Save();
            KeybindingsChanged?.Invoke();
        }

        public static void SetSkill(int playerIndex, int slot, Key key)
        {
            slot = Mathf.Clamp(slot, 0, 4);
            PlayerPrefs.SetInt($"{PrefPrefix}P{playerIndex}_Skill_{slot}", (int)key);
            PlayerPrefs.Save();
            KeybindingsChanged?.Invoke();
        }

        public static void ResetToDefaults()
        {
            for (int p = 0; p < 2; p++)
            {
                PlayerPrefs.DeleteKey($"{PrefPrefix}P{p}_MoveLeft");
                PlayerPrefs.DeleteKey($"{PrefPrefix}P{p}_MoveRight");
                for (int s = 0; s < 5; s++)
                {
                    PlayerPrefs.DeleteKey($"{PrefPrefix}P{p}_Skill_{s}");
                }
            }
            PlayerPrefs.Save();
            KeybindingsChanged?.Invoke();
        }

        public static string GetReadableKeyName(Key key)
        {
            switch (key)
            {
                case Key.None: return "Chưa gán";
                case Key.Digit0: return "0";
                case Key.Digit1: return "1";
                case Key.Digit2: return "2";
                case Key.Digit3: return "3";
                case Key.Digit4: return "4";
                case Key.Digit5: return "5";
                case Key.Digit6: return "6";
                case Key.Digit7: return "7";
                case Key.Digit8: return "8";
                case Key.Digit9: return "9";
                case Key.Numpad0: return "Num 0";
                case Key.Numpad1: return "Num 1";
                case Key.Numpad2: return "Num 2";
                case Key.Numpad3: return "Num 3";
                case Key.Numpad4: return "Num 4";
                case Key.Numpad5: return "Num 5";
                case Key.Numpad6: return "Num 6";
                case Key.Numpad7: return "Num 7";
                case Key.Numpad8: return "Num 8";
                case Key.Numpad9: return "Num 9";
                case Key.LeftArrow: return "← Trái";
                case Key.RightArrow: return "→ Phải";
                case Key.UpArrow: return "↑ Lên";
                case Key.DownArrow: return "↓ Xuống";
                case Key.LeftShift: return "L-Shift";
                case Key.RightShift: return "R-Shift";
                case Key.LeftCtrl: return "L-Ctrl";
                case Key.RightCtrl: return "R-Ctrl";
                case Key.Space: return "Space";
                case Key.Enter: return "Enter";
                case Key.NumpadEnter: return "Num Enter";
                case Key.Comma: return ",";
                case Key.Period: return ".";
                case Key.Slash: return "/";
                case Key.Semicolon: return ";";
                case Key.Quote: return "'";
                case Key.Minus: return "-";
                case Key.Equals: return "=";
                case Key.LeftBracket: return "[";
                case Key.RightBracket: return "]";
                default: return key.ToString();
            }
        }
    }
}
