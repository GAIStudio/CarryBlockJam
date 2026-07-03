using UnityEngine;

namespace GAITemplate
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "GAITemplate/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Flow")]
        public bool autoSpawnLevel = true;

        [Tooltip("When the player level exceeds the config count, looping starts from this level number (1-based).")]
        public int loopFromLevel = 3;

        [Header("Levels")]
        public LevelData[] levels;

        public int LevelCount => levels != null ? levels.Length : 0;

        public int ResolveConfigIndex(int playerLevel)
        {
            if (LevelCount == 0)
                return 0;

            int level = Mathf.Max(1, playerLevel);

            // Tüm leveller bitince loopFromLevel'dan döngüye gir.
            // (Eski while-loop loopFromLevel >= LevelCount durumunda sonsuza dönüyordu.)
            if (level > LevelCount)
            {
                int safeLoop   = Mathf.Clamp(loopFromLevel, 1, LevelCount);
                int loopLength = Mathf.Max(1, LevelCount - safeLoop + 1);
                int overflow   = level - LevelCount - 1; // 0 = ilk loop level'ı
                level = safeLoop + (overflow % loopLength);
            }

            return Mathf.Clamp(level - 1, 0, LevelCount - 1);
        }

        public LevelData GetLevelData(int playerLevel)
        {
            if (LevelCount == 0)
            {
                Debug.LogError("LevelConfig has no levels assigned.");
                return null;
            }

            return levels[ResolveConfigIndex(playerLevel)];
        }

        /// <summary>
        /// Maps absolute player level to a config entry without looping (1-based).
        /// Returns null when the level is outside the config range.
        /// </summary>
        public LevelData GetLevelAtPlayerLevel(int playerLevel)
        {
            if (LevelCount == 0)
                return null;

            int index = playerLevel - 1;
            if (index < 0 || index >= LevelCount)
                return null;

            return levels[index];
        }

        /// <summary>
        /// Resolves level data with optional feature-milestone tutorial gate before loop.
        /// </summary>
        public LevelData ResolveLevelData(int playerLevel, FeatureProgressionConfig featureProgression)
        {
            if (LevelCount == 0)
            {
                Debug.LogError("LevelConfig has no levels assigned.");
                return null;
            }

            if (featureProgression != null
                && featureProgression.HasMilestones
                && playerLevel > LevelCount
                && !FeatureProgressionStore.HasReachedLastMilestone(featureProgression))
            {
                int milestoneLevel = featureProgression.LastMilestoneLevel;
                LevelData milestoneData = GetLevelAtPlayerLevel(milestoneLevel);
                if (milestoneData != null
                    && milestoneData.hasTutorial
                    && milestoneData.tutorialStages != null
                    && milestoneData.tutorialStages.Count > 0
                    && !TutorialManager.IsCompleted(milestoneData))
                {
                    return milestoneData;
                }
            }

            return GetLevelData(playerLevel);
        }
    }
}
