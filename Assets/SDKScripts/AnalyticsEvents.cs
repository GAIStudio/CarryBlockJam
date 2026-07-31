using System;

namespace DEVELOPER_SYSTEM.Main.SdkSystem.Scripts
{
    public static class AnalyticsEvents
    {
        public static Action<int> OnLevelStart;
        public static Action<int> OnLevelFail;
        public static Action<int> OnLevelComplete;
    }
}
