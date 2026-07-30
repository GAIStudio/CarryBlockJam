using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AdjustSdk;
using Facebook.Unity;
using Firebase;
using Firebase.Analytics;
using GameAnalyticsSDK;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Advertisement.IosSupport;
#endif

namespace DEVELOPER_SYSTEM.Main.SdkSystem.Scripts
{
    [DefaultExecutionOrder(-10000)]
    public class AnalyticsManager : MonoBehaviour
    {
        [Header("Adjust Settings")]
        [SerializeField] private string adjustAppToken;
        [SerializeField] private AdjustEnvironment environment;

        [Header("Adjust Event Tokens")]
        [SerializeField] private string tokenLevelStart;
        [SerializeField] private string tokenLevelComplete;
        [SerializeField] private string tokenLevelFail;

        private bool _firebaseReady;
        private bool _firebaseUnavailable;
        private float _levelStartTime;
        private int _levelMoveCount;
        private bool _gameAnalyticsReady;
        private bool _subscribed;
        private readonly List<Action> _pendingFirebaseEvents = new List<Action>();

        public static bool IsReady { get; private set; }
        public static AnalyticsManager Instance { get; private set; }

        #region PLAYER PREFS

        private int LastLevelIndex
        {
            get => PlayerPrefs.GetInt("LastLevelIndex", 0);
            set => PlayerPrefs.SetInt("LastLevelIndex", value);
        }

        private int LevelFailCounter
        {
            get => PlayerPrefs.GetInt("LevelFailCounter", 1);
            set => PlayerPrefs.SetInt("LevelFailCounter", value);
        }

        #endregion

        #region UNITY LIFECYCLE

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<AnalyticsManager>() != null)
                return;

            var analyticsObject = new GameObject("AnalyticsManager");
            analyticsObject.AddComponent<AnalyticsManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyResourceSettings();

            AnalyticsEvents.OnLevelStart    += TrackLevelStart;
            AnalyticsEvents.OnLevelFail     += TrackLevelFail;
            AnalyticsEvents.OnLevelComplete += TrackLevelComplete;
            _subscribed = true;

            _ = InitializeFirebase();
        }

        async void Start()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();

                while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                       == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    await Task.Delay(100);
                }
            }
#endif

            GameAnalytics.Initialize();
            InitAdjust();
            InitFacebookAndAppLovin();
            _gameAnalyticsReady = true;
            IsReady = true;
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;

            AnalyticsEvents.OnLevelStart -= TrackLevelStart;
            AnalyticsEvents.OnLevelFail -= TrackLevelFail;
            AnalyticsEvents.OnLevelComplete -= TrackLevelComplete;
            _subscribed = false;

            if (Instance == this)
            {
                Instance = null;
                IsReady = false;
            }
        }

        private void ApplyResourceSettings()
        {
            SdkSettings settings = Resources.Load<SdkSettings>("SdkSettings");
            if (settings == null)
            {
                Debug.LogWarning("[Analytics] Resources/SdkSettings is missing.");
                return;
            }

            adjustAppToken = settings.adjustAppToken;
            environment = settings.environment;
            tokenLevelStart = settings.tokenLevelStart;
            tokenLevelComplete = settings.tokenLevelComplete;
            tokenLevelFail = settings.tokenLevelFail;
        }

        #endregion

        #region FIREBASE

        private async Task InitializeFirebase()
        {
#if UNITY_EDITOR
            string desktopConfig = Path.Combine(
                Application.streamingAssetsPath,
                "google-services-desktop.json");
            string fallbackConfig = Path.Combine(
                Application.streamingAssetsPath,
                "google-services.json");
            if (!File.Exists(desktopConfig) && !File.Exists(fallbackConfig))
            {
                _firebaseUnavailable = true;
                _pendingFirebaseEvents.Clear();
                Debug.LogWarning(
                    "[Analytics] Firebase disabled in Editor: no Firebase app configuration was found.");
                return;
            }
#endif

            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                _firebaseReady = true;
                Debug.Log("[Analytics] Firebase Ready");

                for (int i = 0; i < _pendingFirebaseEvents.Count; i++)
                    _pendingFirebaseEvents[i]?.Invoke();
                _pendingFirebaseEvents.Clear();
            }
            else
            {
                Debug.LogError($"[Firebase] Dependency error: {dependencyStatus}");
            }
        }

        #endregion

        #region ADJUST

        private void InitAdjust()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(adjustAppToken))
            {
                AdjustConfig config = new AdjustConfig(adjustAppToken, environment);
                config.LogLevel = AdjustLogLevel.Info;
                Adjust.InitSdk(config);
                Debug.Log("[Analytics] Adjust SDK Initialized");
            }
            else
            {
                Debug.LogWarning("[Analytics] Adjust token is empty!");
            }
#endif
        }

        private void SendAdjustEvent(string token, string key, string value)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(token))
            {
                AdjustEvent adjEvent = new AdjustEvent(token);
                adjEvent.AddCallbackParameter(key, value);
                Adjust.TrackEvent(adjEvent);
            }
#endif
        }

        #endregion

        #region FACEBOOK & APPLOVIN

        private void InitFacebookAndAppLovin()
        {
            if (!FB.IsInitialized)
            {
                FB.Init(() =>
                {
                    if (FB.IsInitialized)
                        FB.ActivateApp();

                    InitializeAppLovinReflection();
                });
            }
            else
            {
                FB.ActivateApp();
                InitializeAppLovinReflection();
            }
        }

        private void InitializeAppLovinReflection()
        {
            System.Type maxSdkType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                maxSdkType = assembly.GetType("MaxSdk");
                if (maxSdkType != null) break;
            }

            if (maxSdkType != null)
            {
                var method = maxSdkType.GetMethod("InitializeSdk", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, System.Type.EmptyTypes, null);
                if (method != null)
                {
                    method.Invoke(null, null);
                    Debug.Log("[Analytics] AppLovin MAX SDK Initialized successfully.");
                }
                else
                {
                    Debug.LogWarning("[Analytics] AppLovin MAX SDK 'InitializeSdk' method not found!");
                }
            }
            else
            {
                Debug.LogWarning("[Analytics] AppLovin MAX SDK (MaxSdk) not found in the project. Skipping initialization.");
            }
        }

        #endregion

        #region LEVEL EVENTS

        private void SendFirebaseEvent(Action eventAction)
        {
            if (eventAction == null)
                return;

            if (_firebaseUnavailable)
                return;

            if (_firebaseReady)
            {
                eventAction.Invoke();
                return;
            }

            _pendingFirebaseEvents.Add(eventAction);
        }

        public void TrackLevelStart(int level)
        {
            _levelStartTime = Time.time;
            _levelMoveCount = 0;

            if (LastLevelIndex != level)
            {
                LevelFailCounter = 1;
                LastLevelIndex = level;
            }

            SendAdjustEvent(tokenLevelStart, "level", level.ToString());
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, $"Level_{level}");

            SendFirebaseEvent(() =>
                FirebaseAnalytics.LogEvent("level_start",
                    new Parameter("level", level)));
        }

        public void TrackLevelFail(int level)
        {
            if (LastLevelIndex == level)
                LevelFailCounter++;

            _levelStartTime = 0;
            SendAdjustEvent(tokenLevelFail, "level", level.ToString());
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, $"Level_{level}");

            SendFirebaseEvent(() =>
                FirebaseAnalytics.LogEvent("level_fail",
                    new Parameter("level", level)));
        }

        public void TrackLevelComplete(int level)
        {
            int duration = Mathf.RoundToInt(Time.time - _levelStartTime);
            _levelStartTime = 0;
            int moveCount = _levelMoveCount;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(tokenLevelComplete))
            {
                AdjustEvent adjEvent = new AdjustEvent(tokenLevelComplete);
                adjEvent.AddCallbackParameter("level", level.ToString());
                adjEvent.AddCallbackParameter("duration", duration.ToString());
                adjEvent.AddCallbackParameter("move_count", moveCount.ToString());
                adjEvent.AddCallbackParameter("average_try_to_pass", LevelFailCounter.ToString());
                Adjust.TrackEvent(adjEvent);
            }
#endif

            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, $"Level_{level}", duration);

            SendFirebaseEvent(() =>
                FirebaseAnalytics.LogEvent("level_complete",
                    new Parameter("level", level),
                    new Parameter("duration_seconds", duration),
                    new Parameter("move_count", moveCount),
                    new Parameter("average_try_to_pass", LevelFailCounter)));
        }

        #endregion
    }
}
