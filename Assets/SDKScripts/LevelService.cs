using UnityEngine;

namespace DEVELOPER_SYSTEM.Main.Other_System
{
    public static class LevelService
    {
        public static int LevelIndex
        {
            get => PlayerPrefs.GetInt("LevelIndex", 1);
            set
            {
                PlayerPrefs.SetInt("LevelIndex", value);
                PlayerPrefs.Save();
            }
        }
    }
}