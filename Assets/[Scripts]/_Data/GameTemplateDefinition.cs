using UnityEngine;

namespace GAITemplate
{
    public enum PuzzleMechanicType
    {
        Grid,
        SlideLane
    }

    [CreateAssetMenu(fileName = "GameTemplate", menuName = "GAITemplate/Game Template")]
    public class GameTemplateDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string templateId;
        public string displayName;

        [TextArea(2, 5)]
        public string description;

        public Texture2D icon;

        [Header("Mechanic")]
        public PuzzleMechanicType mechanicType;

        [Header("Configuration")]
        public GameData gameData;
        public LevelConfig levelConfig;

        [Tooltip("Level entries copied into LevelConfig when applying this template.")]
        public LevelData[] templateLevels;

        [Header("Prefabs")]
        public GameObject sampleLevelPrefab;
        public GameObject boardRootPrefab;
    }
}
