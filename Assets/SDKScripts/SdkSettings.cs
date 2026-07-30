using UnityEngine;
using AdjustSdk;

namespace DEVELOPER_SYSTEM.Main.SdkSystem.Scripts
{
    [CreateAssetMenu(fileName = "SdkSettings", menuName = "SDK/SdkSettings")]
    public class SdkSettings : ScriptableObject
    {
        [Header("Adjust Settings")]
        public string adjustAppToken;
        public AdjustEnvironment environment;

        [Header("Adjust Event Tokens")]
        public string tokenLevelStart;
        public string tokenLevelComplete;
        public string tokenLevelFail;
    }
}
