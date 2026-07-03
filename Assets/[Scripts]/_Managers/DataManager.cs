using UnityEngine;

namespace GAITemplate
{
    public class DataManager : MonoBehaviour
    {
        private readonly string LEVEL_DATA = "level";
        private readonly string MONEY_DATA = "money";
        private readonly string SOUND_DATA = "sound";
        private readonly string VIBRATION_DATA = "vibration";

        public int level;
        public int money;
        public bool sound;
        public bool vibration;

        #region Singleton
        public static DataManager instance = null;
        private bool _initialized;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }
        #endregion

        public void Initialize(GameData gameData)
        {
            if (_initialized)
                return;

            _initialized = true;

            // Ses ve titreşim varsayılanı: gameData varsa onun ayarı, yoksa AÇIK.
            // (Eskiden gameData null olduğunda otomatik kapalıya dönüyordu.)
            bool soundDefault     = gameData != null ? gameData.soundEnabledByDefault     : true;
            bool vibrationDefault = gameData != null ? gameData.vibrationEnabledByDefault : true;

            level     = PlayerPrefs.GetInt(LEVEL_DATA, 1);
            money     = PlayerPrefs.GetInt(MONEY_DATA, gameData != null ? gameData.startingMoney : 0);
            sound     = PlayerPrefs.GetInt(SOUND_DATA,     soundDefault     ? 1 : 0) == 1;
            vibration = PlayerPrefs.GetInt(VIBRATION_DATA, vibrationDefault ? 1 : 0) == 1;
        }

        public void SetLevel(int _level)
        {
            level = _level;
            PlayerPrefs.SetInt(LEVEL_DATA, level);
            PlayerPrefs.Save();
        }

        public void SetMoney(int _money)
        {
            money = _money;
            PlayerPrefs.SetInt(MONEY_DATA, money);
            PlayerPrefs.Save();
        }

        public void SetSound(bool isOn)
        {
            PlayerPrefs.SetInt(SOUND_DATA, isOn ? 1 : 0);
            PlayerPrefs.Save();
            sound = isOn;
        }

        public void SetVibration(bool isOn)
        {
            PlayerPrefs.SetInt(VIBRATION_DATA, isOn ? 1 : 0);
            PlayerPrefs.Save();
            vibration = isOn;
        }
    }
}