using UnityEngine;
using UnityEditor;
using System.IO;
using DEVELOPER_SYSTEM.Main.SdkSystem.Scripts;
using AdjustSdk;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Setup;

public class SdkSettingsWindow : EditorWindow
{
    private SdkSettings sdkSettings;
    private ScriptableObject facebookSettings;
    private GameAnalyticsSDK.Setup.Settings gaSettings;
    private ScriptableObject appLovinSettings;

    // Temporary variables for Facebook
    private string fbAppId;
    private string fbAppName;
    private string fbClientToken;

    // Temporary variables for GameAnalytics
    private string gaAndroidGameKey;
    private string gaAndroidSecretKey;
    private string gaiOSGameKey;
    private string gaiOSSecretKey;

    // Temporary variables for AppLovin
    private string appLovinSdkKey;
    private string appLovinAdMobAndroidAppId;
    private string appLovinAdMobIosAppId;

    private Vector2 scrollPosition;

    [MenuItem("Tools/SDK Settings Editor")]
    public static void ShowWindow()
    {
        GetWindow<SdkSettingsWindow>("SDK Settings Editor");
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void AutoInstallIosSupportPackage()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (File.Exists(manifestPath))
        {
            string manifestText = File.ReadAllText(manifestPath);
            if (!manifestText.Contains("com.unity.ads.ios-support"))
            {
                Debug.Log("[SDK Settings] iOS 14 Advertising Support package (com.unity.ads.ios-support) is missing in this project. Automatically adding it to Package Manager...");
                UnityEditor.PackageManager.Client.Add("com.unity.ads.ios-support");
            }
        }
    }

    private void LoadSettings()
    {
        AutoInstallIosSupportPackage();

        // 1. Load/Create Adjust SdkSettings
        sdkSettings = Resources.Load<SdkSettings>("SdkSettings");
        if (sdkSettings == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:SdkSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                sdkSettings = AssetDatabase.LoadAssetAtPath<SdkSettings>(path);
            }
            else
            {
                if (!Directory.Exists(Application.dataPath + "/Resources"))
                {
                    Directory.CreateDirectory(Application.dataPath + "/Resources");
                }
                sdkSettings = ScriptableObject.CreateInstance<SdkSettings>();
                AssetDatabase.CreateAsset(sdkSettings, "Assets/Resources/SdkSettings.asset");
                AssetDatabase.SaveAssets();
            }
        }

        // 2. Load Facebook Settings
        facebookSettings = Resources.Load<ScriptableObject>("FacebookSettings");
        if (facebookSettings != null)
        {
            SerializedObject so = new SerializedObject(facebookSettings);
            SerializedProperty appIds = so.FindProperty("appIds");
            SerializedProperty appLabels = so.FindProperty("appLabels");
            SerializedProperty clientTokens = so.FindProperty("clientTokens");

            fbAppId = (appIds != null && appIds.arraySize > 0) ? appIds.GetArrayElementAtIndex(0).stringValue : "";
            fbAppName = (appLabels != null && appLabels.arraySize > 0) ? appLabels.GetArrayElementAtIndex(0).stringValue : "";
            fbClientToken = (clientTokens != null && clientTokens.arraySize > 0) ? clientTokens.GetArrayElementAtIndex(0).stringValue : "";
        }

        // 3. Load GameAnalytics Settings
        gaSettings = Resources.Load<GameAnalyticsSDK.Setup.Settings>("GameAnalytics/Settings");
        if (gaSettings != null)
        {
            int androidIdx = gaSettings.Platforms.IndexOf(RuntimePlatform.Android);
            if (androidIdx != -1)
            {
                gaAndroidGameKey = gaSettings.GetGameKey(androidIdx);
                gaAndroidSecretKey = gaSettings.GetSecretKey(androidIdx);
            }
            else
            {
                gaAndroidGameKey = "";
                gaAndroidSecretKey = "";
            }

            int iosIdx = gaSettings.Platforms.IndexOf(RuntimePlatform.IPhonePlayer);
            if (iosIdx != -1)
            {
                gaiOSGameKey = gaSettings.GetGameKey(iosIdx);
                gaiOSSecretKey = gaSettings.GetSecretKey(iosIdx);
            }
            else
            {
                gaiOSGameKey = "";
                gaiOSSecretKey = "";
            }
        }

        // 4. Load AppLovin Settings dynamically (Decoupled from type to prevent compile errors)
        
        // Force initialization if the SDK is installed but the asset is not created yet
        System.Type appLovinSettingsType = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            appLovinSettingsType = assembly.GetType("AppLovinSettings");
            if (appLovinSettingsType != null) break;
        }

        if (appLovinSettingsType != null)
        {
            var instanceProp = appLovinSettingsType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProp != null)
            {
                instanceProp.GetValue(null, null);
            }
        }

        appLovinSettings = null;
        string[] appLovinGuids = AssetDatabase.FindAssets("AppLovinSettings t:ScriptableObject");
        if (appLovinGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(appLovinGuids[0]);
            appLovinSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        }

        if (appLovinSettings != null)
        {
            SerializedObject so = new SerializedObject(appLovinSettings);
            appLovinSdkKey = so.FindProperty("sdkKey")?.stringValue ?? "";
            appLovinAdMobAndroidAppId = so.FindProperty("adMobAndroidAppId")?.stringValue ?? "";
            appLovinAdMobIosAppId = so.FindProperty("adMobIosAppId")?.stringValue ?? "";
        }
        else
        {
            appLovinSdkKey = "";
            appLovinAdMobAndroidAppId = "";
            appLovinAdMobIosAppId = "";
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        GUILayout.Label("Unified SDK Settings Manager", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Configure Adjust, Facebook, GameAnalytics, and AppLovin MAX in one place.", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);

        // --- ADJUST SECTION ---
        DrawAdjustSection();

        EditorGUILayout.Space(15);
        DrawSeparator();
        EditorGUILayout.Space(15);

        // --- FACEBOOK SECTION ---
        DrawFacebookSection();

        EditorGUILayout.Space(15);
        DrawSeparator();
        EditorGUILayout.Space(15);

        // --- GAMEANALYTICS SECTION ---
        DrawGameAnalyticsSection();

        EditorGUILayout.Space(15);
        DrawSeparator();
        EditorGUILayout.Space(15);

        // --- APPLOVIN SECTION ---
        DrawAppLovinSection();

        EditorGUILayout.Space(25);

        if (GUILayout.Button("Save & Apply All Settings", GUILayout.Height(40)))
        {
            SaveAllSettings();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Debug: Print manifest.json Content", GUILayout.Height(25)))
        {
            string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            if (File.Exists(manifestPath))
            {
                Debug.Log("[SDK Debug] manifest.json content:\n" + File.ReadAllText(manifestPath));
            }
            else
            {
                Debug.LogError("[SDK Debug] manifest.json not found!");
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void DrawAdjustSection()
    {
        GUILayout.Label("1. Adjust SDK Settings", EditorStyles.boldLabel);
        if (sdkSettings == null)
        {
            EditorGUILayout.HelpBox("Adjust SdkSettings asset is missing!", MessageType.Error);
            return;
        }

        SerializedObject so = new SerializedObject(sdkSettings);
        so.Update();

        SerializedProperty appToken = so.FindProperty("adjustAppToken");
        SerializedProperty env = so.FindProperty("environment");
        SerializedProperty lvlStart = so.FindProperty("tokenLevelStart");
        SerializedProperty lvlComplete = so.FindProperty("tokenLevelComplete");
        SerializedProperty lvlFail = so.FindProperty("tokenLevelFail");

        EditorGUILayout.PropertyField(appToken, new GUIContent("App Token"));
        EditorGUILayout.PropertyField(env, new GUIContent("Environment"));
        
        EditorGUILayout.Space(5);
        GUILayout.Label("Adjust Event Tokens", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(lvlStart, new GUIContent("Level Start Event"));
        EditorGUILayout.PropertyField(lvlComplete, new GUIContent("Level Complete Event"));
        EditorGUILayout.PropertyField(lvlFail, new GUIContent("Level Fail Event"));

        so.ApplyModifiedProperties();
    }

    private void DrawFacebookSection()
    {
        GUILayout.Label("2. Facebook SDK Settings", EditorStyles.boldLabel);
        if (facebookSettings == null)
        {
            EditorGUILayout.HelpBox("FacebookSettings.asset not found in Resources. Please create Facebook settings via 'Facebook > Edit Settings' menu first.", MessageType.Warning);
            return;
        }

        fbAppId = EditorGUILayout.TextField("App ID", fbAppId);
        fbAppName = EditorGUILayout.TextField("App Name", fbAppName);
        fbClientToken = EditorGUILayout.TextField("Client Token", fbClientToken);
    }

    private void DrawGameAnalyticsSection()
    {
        GUILayout.Label("3. GameAnalytics SDK Settings", EditorStyles.boldLabel);
        if (gaSettings == null)
        {
            EditorGUILayout.HelpBox("GameAnalytics Settings.asset not found in Resources. Please initialize GameAnalytics first.", MessageType.Warning);
            return;
        }

        GUILayout.Label("Android Platform Keys", EditorStyles.miniBoldLabel);
        gaAndroidGameKey = EditorGUILayout.TextField("Game Key", gaAndroidGameKey);
        gaAndroidSecretKey = EditorGUILayout.TextField("Secret Key", gaAndroidSecretKey);

        EditorGUILayout.Space(5);

        GUILayout.Label("iOS Platform Keys", EditorStyles.miniBoldLabel);
        gaiOSGameKey = EditorGUILayout.TextField("Game Key", gaiOSGameKey);
        gaiOSSecretKey = EditorGUILayout.TextField("Secret Key", gaiOSSecretKey);
    }

    private void DrawAppLovinSection()
    {
        GUILayout.Label("4. AppLovin MAX Settings", EditorStyles.boldLabel);
        if (appLovinSettings == null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("AppLovinSettings.asset not found. Please install the AppLovin MAX SDK first.", MessageType.Warning);
            if (GUILayout.Button("Trigger Automatic AppLovin MAX SDK Installation", GUILayout.Height(30)))
            {
                SdkDependencyInstaller.InstallDependencies();
                LoadSettings();
            }
            EditorGUILayout.EndVertical();
            return;
        }

        appLovinSdkKey = EditorGUILayout.TextField("SDK Key", appLovinSdkKey);
        
        EditorGUILayout.Space(5);
        GUILayout.Label("AdMob App IDs (If Mediated)", EditorStyles.miniBoldLabel);
        appLovinAdMobAndroidAppId = EditorGUILayout.TextField("AdMob Android App ID", appLovinAdMobAndroidAppId);
        appLovinAdMobIosAppId = EditorGUILayout.TextField("AdMob iOS App ID", appLovinAdMobIosAppId);
    }

    private void SaveAllSettings()
    {
        // Force focus out of any active text fields so current changes are committed
        GUI.FocusControl(null);

        // 1. Save Adjust Settings (to ScriptableObject asset)
        if (sdkSettings != null)
        {
            EditorUtility.SetDirty(sdkSettings);
        }

        // 2. Save Facebook Settings
        if (facebookSettings != null)
        {
            SerializedObject so = new SerializedObject(facebookSettings);
            SerializedProperty appIds = so.FindProperty("appIds");
            SerializedProperty appLabels = so.FindProperty("appLabels");
            SerializedProperty clientTokens = so.FindProperty("clientTokens");

            if (appIds.arraySize == 0) appIds.arraySize = 1;
            if (appLabels.arraySize == 0) appLabels.arraySize = 1;
            if (clientTokens.arraySize == 0) clientTokens.arraySize = 1;

            appIds.GetArrayElementAtIndex(0).stringValue = fbAppId;
            appLabels.GetArrayElementAtIndex(0).stringValue = fbAppName;
            clientTokens.GetArrayElementAtIndex(0).stringValue = fbClientToken;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(facebookSettings);
        }

        // 3. Save GameAnalytics Settings
        if (gaSettings != null)
        {
            // Android
            int androidIdx = gaSettings.Platforms.IndexOf(RuntimePlatform.Android);
            if (androidIdx == -1)
            {
                gaSettings.AddPlatform(RuntimePlatform.Android);
                androidIdx = gaSettings.Platforms.Count - 1;
            }
            gaSettings.UpdateGameKey(androidIdx, gaAndroidGameKey);
            gaSettings.UpdateSecretKey(androidIdx, gaAndroidSecretKey);

            // iOS
            int iosIdx = gaSettings.Platforms.IndexOf(RuntimePlatform.IPhonePlayer);
            if (iosIdx == -1)
            {
                gaSettings.AddPlatform(RuntimePlatform.IPhonePlayer);
                iosIdx = gaSettings.Platforms.Count - 1;
            }
            gaSettings.UpdateGameKey(iosIdx, gaiOSGameKey);
            gaSettings.UpdateSecretKey(iosIdx, gaiOSSecretKey);

            EditorUtility.SetDirty(gaSettings);
        }

        // 4. Save AppLovin Settings via SerializedObject (Decoupled from type)
        if (appLovinSettings != null)
        {
            SerializedObject so = new SerializedObject(appLovinSettings);
            so.Update();

            var sdkKeyProp = so.FindProperty("sdkKey");
            if (sdkKeyProp != null) sdkKeyProp.stringValue = appLovinSdkKey;

            var adMobAndroidProp = so.FindProperty("adMobAndroidAppId");
            if (adMobAndroidProp != null) adMobAndroidProp.stringValue = appLovinAdMobAndroidAppId;

            var adMobIosProp = so.FindProperty("adMobIosAppId");
            if (adMobIosProp != null) adMobIosProp.stringValue = appLovinAdMobIosAppId;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(appLovinSettings);
        }

        // 5. Create GameAnalytics GameObject in active scene if not exists
        var existingGA = FindObjectOfType<GameAnalyticsSDK.GameAnalytics>();
        if (existingGA == null)
        {
            GameObject gaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAnalytics/Plugins/Prefabs/GameAnalytics.prefab");
            if (gaPrefab != null)
            {
                GameObject gaInstance = PrefabUtility.InstantiatePrefab(gaPrefab) as GameObject;
                Undo.RegisterCreatedObjectUndo(gaInstance, "Create GameAnalytics");
                Debug.Log("[SDK Settings] Instantiated GameAnalytics prefab in the scene.");
            }
            else
            {
                Debug.LogWarning("[SDK Settings] GameAnalytics prefab not found at 'Assets/GameAnalytics/Plugins/Prefabs/GameAnalytics.prefab'.");
            }
        }

        // 6. Create AnalyticsManager GameObject and set Adjust fields from panel
        var amComp = FindObjectOfType<AnalyticsManager>();
        GameObject amGo = amComp != null ? amComp.gameObject : null;
        if (amGo == null)
        {
            amGo = new GameObject("AnalyticsManager");
            amComp = amGo.AddComponent<AnalyticsManager>();
            Undo.RegisterCreatedObjectUndo(amGo, "Create AnalyticsManager");
            Debug.Log("[SDK Settings] Created AnalyticsManager GameObject in the scene.");
        }

        if (amComp != null && sdkSettings != null)
        {
            // Direct reflection write to ensure values are copied into memory fields immediately
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var type = typeof(AnalyticsManager);

            type.GetField("adjustAppToken", flags)?.SetValue(amComp, sdkSettings.adjustAppToken);
            type.GetField("environment", flags)?.SetValue(amComp, sdkSettings.environment);
            type.GetField("tokenLevelStart", flags)?.SetValue(amComp, sdkSettings.tokenLevelStart);
            type.GetField("tokenLevelComplete", flags)?.SetValue(amComp, sdkSettings.tokenLevelComplete);
            type.GetField("tokenLevelFail", flags)?.SetValue(amComp, sdkSettings.tokenLevelFail);

            // SerializedObject write to ensure serialization & scene persistence
            SerializedObject amSo = new SerializedObject(amComp);
            amSo.Update();
            amSo.FindProperty("adjustAppToken").stringValue = sdkSettings.adjustAppToken;
            amSo.FindProperty("environment").enumValueIndex = (int)sdkSettings.environment;
            amSo.FindProperty("tokenLevelStart").stringValue = sdkSettings.tokenLevelStart;
            amSo.FindProperty("tokenLevelComplete").stringValue = sdkSettings.tokenLevelComplete;
            amSo.FindProperty("tokenLevelFail").stringValue = sdkSettings.tokenLevelFail;
            amSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(amComp);
            Debug.Log($"[SDK Settings] Updated AnalyticsManager fields. Token: '{sdkSettings.adjustAppToken}', Env: {sdkSettings.environment}");
        }

        // Mark active scene dirty so changes are saved to the scene file
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("SDK Settings Manager", "All SDK settings saved successfully, and objects configured in the scene!", "OK");
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }
}
