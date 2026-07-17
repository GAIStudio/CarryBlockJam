using CarryBlockJam;
using UnityEngine;
using UnityEngine.Events;

namespace GAITemplate
{
    public class LevelManager : MonoBehaviour
    {
        [HideInInspector] public UnityEvent startEvent = new UnityEvent();
        [HideInInspector] public LevelData currentLevelData;
        int elephant;
        [HideInInspector] public EndGameEvent endGameEvent = new EndGameEvent();

        public LevelBase ActiveLevelBase { get; private set; }
        private bool _endGameReserved;

        #region Singleton
        public static LevelManager instance = null;
        private void Awake()
        {
            if (instance == null)
                instance = this;
        }
        #endregion

        private void Start() => ConstructLevel();

        private void ConstructLevel()
        {
            if (GameManager.instance == null || GameManager.instance.LevelConfig == null)
            {
                Debug.LogWarning("[LevelManager] GameManager or LevelConfig is missing. Skipping level setup.");
                startEvent.Invoke();
                return;
            }

            LevelConfig config = GameManager.instance.LevelConfig;
            currentLevelData = config.ResolveLevelData(
                GameManager.instance.level,
                GameManager.instance.FeatureProgressionConfig);

            if (config.autoSpawnLevel && currentLevelData != null)
                SpawnLevelBase(currentLevelData);
            else if (currentLevelData != null)
                CarryBlockJamSceneLevelRuntime.EnsureGameplayOnSceneBoards(currentLevelData);

            // Scene-board path does not go through LevelBase.BuildLevel, so start tutorial here.
            if (currentLevelData != null && TutorialManager.Instance != null)
                TutorialManager.Instance.StartTutorial(currentLevelData);

            if (!PlayerPrefs.HasKey("Elephant"))
            {
                PlayerPrefs.SetInt("Elephant", elephant);
            }

            startEvent.Invoke();
        }

        private static void SpawnLevelBase(LevelData levelData)
        {
            GameObject prefab = GameManager.instance != null ? GameManager.instance.LevelBasePrefab : null;

            if (prefab == null)
            {
                Debug.LogWarning(
                    "Level base prefab is not assigned. Set Game Data on GameManager and assign levelBasePrefab on the Piece Catalog asset.",
                    LevelManager.instance);
                return;
            }

            GameObject spawned = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            LevelBase levelBase = spawned.GetComponent<LevelBase>();
            if (levelBase == null)
            {
                Debug.LogError(
                    "Level base prefab is missing a LevelBase component.",
                    LevelManager.instance);
                return;
            }

            LevelManager.instance.ActiveLevelBase = levelBase;
            levelBase.BuildLevel(levelData);
        }

        public void Success()
        {
            if (_endGameReserved)
                return;

            _endGameReserved = true;
            PlayerPrefs.DeleteKey("Elephant");
            GameManager.instance.LevelUp();
            endGameEvent.Invoke(true);
        }

        public void Fail()
        {
            if (_endGameReserved)
                return;

            _endGameReserved = true;
            PlayerPrefs.DeleteKey("Elephant");
            Haptic.MediumTaptic();

            CarryBlockJamSwipeController swipeController =
                FindObjectOfType<CarryBlockJamSwipeController>();
            if (swipeController != null)
            {
                swipeController.PrepareForFailure(CompleteFailure);
                return;
            }

            CompleteFailure();
        }

        private void CompleteFailure()
        {
            endGameEvent.Invoke(false);
        }
    }

    public class EndGameEvent : UnityEvent<bool> { }
}
