using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Persists which feature milestone levels the player has reached.
    /// Used to gate tutorial replay before level loop (Beads_Out parity).
    /// </summary>
    public static class FeatureProgressionStore
    {
        private const string PassedKeyPrefix = "FeatureMilestonePassed_";

        public static bool HasReachedMilestone(int milestoneLevel) =>
            PlayerPrefs.GetInt(PassedKeyPrefix + milestoneLevel, 0) >= 1;

        public static void MarkMilestoneReached(int milestoneLevel)
        {
            if (milestoneLevel <= 0)
                return;

            PlayerPrefs.SetInt(PassedKeyPrefix + milestoneLevel, 1);
        }

        public static bool HasReachedLastMilestone(FeatureProgressionConfig config)
        {
            if (config == null || !config.HasMilestones)
                return true;

            return HasReachedMilestone(config.LastMilestoneLevel);
        }
    }
}
