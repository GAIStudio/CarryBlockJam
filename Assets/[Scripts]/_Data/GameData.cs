using UnityEngine;
using UnityEngine.Serialization;

namespace GAITemplate
{
    [CreateAssetMenu(fileName = "GameData", menuName = "GAITemplate/Game Data")]
    public class GameData : ScriptableObject
    {
        [Header("Economy")]
        public int startingMoney;
        public int levelCompleteReward = 10;

        [Header("Player Defaults")]
        public bool soundEnabledByDefault = true;
        public bool vibrationEnabledByDefault = true;

        [Header("UI")]
        public string levelTextFormat = "LEVEL {0}";

        [Header("Grid")]
        [Min(0.01f)]
        [FormerlySerializedAs("gridSpacing")]
        [Tooltip("Distance between columns (local X).")]
        public float gridSpacingX = 1f;

        [Min(0.01f)]
        [Tooltip("Distance between rows (local Z).")]
        public float gridSpacingZ = 1f;

        [Header("Pieces")]
        [Tooltip("Piece catalog and level base prefab for grid levels.")]
        public GamePieceCatalog pieceCatalog;

        [Header("Feature Progression")]
        [Tooltip("Win-screen milestone levels and copy. Optional.")]
        public FeatureProgressionConfig featureProgression;

        public string FormatLevelText(int level) => string.Format(levelTextFormat, level);
    }
}
