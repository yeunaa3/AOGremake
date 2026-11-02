using UnityEngine;

namespace AOG.Duel
{
    public enum GameMode
    {
        PVE = 0,
        PVP = 1
    }

    public enum P2LoadoutMode
    {
        Random = 0,    // Ngẫu nhiên (Mỗi trận 1 bộ chiêu thức & cung khác nhau, đa dạng)
        UseSet2 = 1,   // Dùng Set kỹ năng II (Người chơi tự chọn ở màn Kỹ năng)
        Mirror = 2     // Giống hệt Player 1 (Solo mirror)
    }

    public static class GameModeManager
    {
        private const string PrefKeyMode = "AOG_GameMode";
        private const string PrefKeyP2LoadoutMode = "AOG_P2_LoadoutMode";

        public static GameMode CurrentMode
        {
            get => (GameMode)PlayerPrefs.GetInt(PrefKeyMode, (int)GameMode.PVE);
            set
            {
                PlayerPrefs.SetInt(PrefKeyMode, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static P2LoadoutMode P2Mode
        {
            get => (P2LoadoutMode)PlayerPrefs.GetInt(PrefKeyP2LoadoutMode, (int)P2LoadoutMode.Random);
            set
            {
                PlayerPrefs.SetInt(PrefKeyP2LoadoutMode, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static bool IsPVP => CurrentMode == GameMode.PVP;
    }
}
