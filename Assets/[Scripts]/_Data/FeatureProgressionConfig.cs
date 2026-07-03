using System.Collections.Generic;
using UnityEngine;

namespace GAITemplate
{
    [CreateAssetMenu(fileName = "FeatureProgressionConfig", menuName = "GAITemplate/Feature Progression Config")]
    public class FeatureProgressionConfig : ScriptableObject
    {
        [Tooltip("Player levels where a new game feature/mechanic unlocks.")]
        public List<int> featureLevels = new List<int>
        {
            5, 15, 35, 45, 55, 75, 105, 135, 150, 175, 200, 225, 250, 300, 350
        };

        [Header("Copy")]
        public string unlockedMessage = "New Feature Unlocked";
        public string progressFormat = "%{0}";

        public bool HasMilestones => featureLevels != null && featureLevels.Count > 0;

        public bool IsMilestoneLevel(int playerLevel)
        {
            if (!HasMilestones || playerLevel <= 0)
                return false;

            for (int i = 0; i < featureLevels.Count; i++)
            {
                if (featureLevels[i] == playerLevel)
                    return true;
            }

            return false;
        }

        public bool HasRemainingMilestones(int progressLevel)
        {
            if (!HasMilestones || progressLevel <= 0)
                return false;

            for (int i = 0; i < featureLevels.Count; i++)
            {
                if (progressLevel < featureLevels[i])
                    return true;
            }

            return false;
        }

        public int LastMilestoneLevel =>
            HasMilestones ? featureLevels[featureLevels.Count - 1] : 0;
    }
}
