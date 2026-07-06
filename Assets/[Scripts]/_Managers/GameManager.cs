using UnityEngine;
using UnityEngine.SceneManagement;

namespace GAITemplate
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameData gameData;
        [SerializeField] private LevelConfig levelConfig;

        [Header("Runtime State")]
        public int level = -1;
        public int money;

        #region Singleton
        public static GameManager instance = null;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
                Application.targetFrameRate = 120;
                GetDependencies();
            }
            else
            {
                DestroyImmediate(this);
            }
        }
        #endregion

        private void GetDependencies()
        {
            if (gameData == null)
                Debug.LogError("GameData is not assigned on GameManager.", this);

            if (levelConfig == null)
                Debug.LogError("LevelConfig is not assigned on GameManager.", this);

            DataManager dataManager = DataManager.instance != null
                ? DataManager.instance
                : GetComponent<DataManager>();

            if (dataManager != null && gameData != null)
            {
                dataManager.Initialize(gameData);
                level = dataManager.level;
                money = dataManager.money;
            }
        }

        public GameData Data => gameData;

        public LevelConfig LevelConfig => levelConfig;

        public FeatureProgressionConfig FeatureProgressionConfig =>
            gameData != null ? gameData.featureProgression : null;

        public GamePieceCatalog PieceCatalog =>
            gameData != null ? gameData.pieceCatalog : null;

        public GameObject LevelBasePrefab =>
            PieceCatalog != null ? PieceCatalog.levelBasePrefab : null;

        public GameObject DefaultPiecePrefab =>
            PieceCatalog != null ? PieceCatalog.defaultPiecePrefab : null;

        public float GridSpacingX =>
            Data != null ? Mathf.Max(0.01f, Data.gridSpacingX) : 1f;

        public float GridSpacingZ =>
            Data != null ? Mathf.Max(0.01f, Data.gridSpacingZ) : 1f;

        #region DataOperations
        public void AddMoney(int amount)
        {
            money += amount;
            DataManager.instance.SetMoney(money);
        }

        public void LevelUp()
        {
            DataManager.instance.SetLevel(++level);
        }
        #endregion

        #region SceneOperations
        // LogoTransitionScene üzerinden logo'lu geçiş yapılır.

        public void RestartScene() => LogoTransitionScene.ReloadCurrent();

        public void OpenScene(int sceneIndex) => LogoTransitionScene.LoadScene(sceneIndex);

        public void OpenScene(string sceneName) => LogoTransitionScene.LoadScene(sceneName);
        #endregion
    }
}