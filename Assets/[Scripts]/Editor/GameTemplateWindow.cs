using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GAITemplate.Editor
{
    public class GameTemplateWindow : EditorWindow
    {
        private Vector2 _scroll;
        private List<GameTemplateDefinition> _templates = new List<GameTemplateDefinition>();

        [MenuItem("GAITemplate/Game Template Setup")]
        public static void Open()
        {
            var window = GetWindow<GameTemplateWindow>("Game Templates");
            window.minSize = new Vector2(420f, 320f);
            window.RefreshTemplates();
        }

        private void OnEnable() => RefreshTemplates();

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Game Template Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Apply Template wires the open scene. Clear Applied Setup undoes that wiring without deleting template assets under Assets/[Templates].",
                MessageType.Info);

            if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
                ScheduleGuiAction(RefreshTemplates);

            EditorGUILayout.Space(4f);
            DrawAppliedSetupStatus();

            using (new EditorGUI.DisabledScope(!HasAppliedSetupInOpenScene()))
            {
                if (GUILayout.Button("Clear Applied Setup", GUILayout.Height(26f)))
                    ScheduleGuiAction(() => ClearAppliedSetup());
            }

            EditorGUILayout.Space(8f);

            if (_templates.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No GameTemplateDefinition assets found. Create one via Assets > Create > GAITemplate > Game Template and assign your GameData, LevelConfig, and levels.",
                    MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (GameTemplateDefinition template in _templates)
            {
                if (template != null)
                    DrawTemplateCard(template);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void ScheduleGuiAction(Action action)
        {
            if (action == null)
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            };
        }

        private void DrawTemplateCard(GameTemplateDefinition template)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            if (template.icon != null)
            {
                GUILayout.Label(template.icon, GUILayout.Width(48f), GUILayout.Height(48f));
            }
            else
            {
                GUILayout.Label(EditorGUIUtility.IconContent("d_Prefab Icon").image,
                    GUILayout.Width(48f), GUILayout.Height(48f));
            }

            EditorGUILayout.BeginVertical();
            string title = string.IsNullOrEmpty(template.displayName) ? template.name : template.displayName;
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Mechanic: {template.mechanicType}", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(template.description))
                EditorGUILayout.LabelField(template.description, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            bool canApply = CanApplyTemplate(template, out string blockReason);

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Apply Template", GUILayout.Height(28f)))
                {
                    GameTemplateDefinition capturedTemplate = template;
                    ScheduleGuiAction(() => ApplyTemplate(capturedTemplate));
                }
            }

            if (!canApply && !string.IsNullOrEmpty(blockReason))
                EditorGUILayout.HelpBox(blockReason, MessageType.Warning);

            bool canClear = CanClearAppliedTemplate(template, out string clearReason);

            using (new EditorGUI.DisabledScope(!canClear))
            {
                if (GUILayout.Button("Clear Applied Setup", GUILayout.Height(24f)))
                {
                    GameTemplateDefinition capturedTemplate = template;
                    ScheduleGuiAction(() => ClearAppliedSetup(capturedTemplate));
                }
            }

            if (!canClear && !string.IsNullOrEmpty(clearReason))
                EditorGUILayout.HelpBox(clearReason, MessageType.None);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private bool CanApplyTemplate(GameTemplateDefinition template, out string blockReason)
        {
            blockReason = null;

            if (template == null)
            {
                blockReason = "Template asset is missing.";
                return false;
            }

            if (template.gameData == null || template.levelConfig == null)
            {
                blockReason = "Template is missing GameData or LevelConfig.";
                return false;
            }

            GameManager gameManager = FindGameManagerInOpenScenes();
            if (gameManager == null)
            {
                blockReason = "Open a scene that contains GameManager (e.g. SampleScene).";
                return false;
            }

            // Only one template can be active at a time. If a different template is already applied,
            // require Clear Applied Setup first.
            GameTemplateApplySnapshot snapshot =
                GameTemplateApplySnapshotStore.LoadForScene(gameManager.gameObject.scene.path);
            if (snapshot != null && snapshot.appliedTemplateId != template.templateId)
            {
                blockReason = $"\"{snapshot.appliedTemplateId}\" is already applied. " +
                              "Use Clear Applied Setup before switching templates.";
                return false;
            }

            return true;
        }

        private void RefreshTemplates()
        {
            _templates.Clear();
            string[] guids = AssetDatabase.FindAssets("t:GameTemplateDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var template = AssetDatabase.LoadAssetAtPath<GameTemplateDefinition>(path);
                if (template != null)
                    _templates.Add(template);
            }

            _templates = _templates
                .OrderBy(t => t.displayName)
                .ThenBy(t => t.name)
                .ToList();

            Repaint();
        }

        private static void ApplyTemplate(GameTemplateDefinition template)
        {
            GameManager gameManager = FindGameManagerInOpenScenes();
            if (gameManager == null)
            {
                EditorUtility.DisplayDialog("Apply Template", "GameManager was not found in open scenes.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(gameManager.gameObject.scene.path))
            {
                EditorUtility.DisplayDialog(
                    "Apply Template",
                    "Save the scene before applying a template so Clear Applied Setup can restore it later.",
                    "OK");
                return;
            }

            bool levelConfigWillChange = template.levelConfig != null &&
                (template.templateLevels != null && template.templateLevels.Length > 0);

            if (levelConfigWillChange)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Apply Game Template",
                    $"This will assign \"{template.displayName}\" to GameManager and may replace LevelConfig level entries. Continue?",
                    "Apply",
                    "Cancel");

                if (!confirmed)
                    return;
            }

            GameTemplateEditorUtility.RepairBrokenReferences(template);

            GameObject boardPrefab = GameTemplateEditorUtility.TryLoadPrefab(template.boardRootPrefab);
            if (boardPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Apply Template",
                    $"Board prefab is missing on \"{template.displayName}\".\n" +
                    "Assign boardRootPrefab on the template asset in the Inspector.",
                    "OK");
                return;
            }

            GameTemplateApplySnapshot snapshot =
                GameTemplateApplySnapshotUtility.CaptureBeforeApply(gameManager, template);

            Undo.RecordObject(gameManager, "Apply Game Template");
            if (template.levelConfig != null)
                Undo.RecordObject(template.levelConfig, "Apply Game Template Levels");

            SerializedObject gameManagerSo = new SerializedObject(gameManager);
            gameManagerSo.FindProperty("gameData").objectReferenceValue = template.gameData;
            gameManagerSo.FindProperty("levelConfig").objectReferenceValue = template.levelConfig;
            gameManagerSo.ApplyModifiedPropertiesWithoutUndo();

            // Apply'da her zaman levelBasePrefab'i template'in board prefab'ına çevir,
            // böylece Grid ↔ SlideLane geçişinde doğru LevelBase yüklenir.
            if (template.gameData != null && template.gameData.pieceCatalog != null &&
                template.boardRootPrefab != null)
            {
                Undo.RecordObject(template.gameData.pieceCatalog, "Apply Game Template Level Base");
                template.gameData.pieceCatalog.levelBasePrefab =
                    GameTemplateEditorUtility.NormalizePrefabReference(template.boardRootPrefab);
                EditorUtility.SetDirty(template.gameData.pieceCatalog);
            }

            ApplyLevelConfig(template);
            ApplyMechanicToAllLevelData(template);
            EnsureBoardRootPrefab(template);

            GameTemplateApplySnapshotStore.Save(snapshot);

            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);

            Debug.Log(
                $"[GAITemplate] Applied \"{template.displayName}\" ({template.mechanicType}). " +
                $"GameData: {AssetDatabase.GetAssetPath(template.gameData)}, " +
                $"LevelConfig: {AssetDatabase.GetAssetPath(template.levelConfig)}.",
                gameManager);

            EditorUtility.DisplayDialog(
                "Template Applied",
                $"\"{template.displayName}\" is now wired on GameManager in {gameManager.gameObject.scene.name}.",
                "OK");
        }

        // Projedeki TÜM LevelData asset'lerinin mechanicType'ını template'inkine çevirir.
        private static void ApplyMechanicToAllLevelData(GameTemplateDefinition template)
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            int updated = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (levelData == null) continue;
                if (levelData.mechanicType == template.mechanicType) continue;

                Undo.RecordObject(levelData, "Apply Template Mechanic to LevelData");
                levelData.mechanicType = template.mechanicType;
                EditorUtility.SetDirty(levelData);
                updated++;
            }

            if (updated > 0)
                AssetDatabase.SaveAssets();
        }

        private static void ApplyLevelConfig(GameTemplateDefinition template)
        {
            if (template.levelConfig == null)
                return;

            LevelData[] levels = template.templateLevels?
                .Where(l => l != null)
                .ToArray();

            if (levels == null || levels.Length == 0)
                return;

            SerializedObject levelConfigSo = new SerializedObject(template.levelConfig);
            SerializedProperty levelsProp = levelConfigSo.FindProperty("levels");
            levelsProp.arraySize = levels.Length;

            for (int i = 0; i < levels.Length; i++)
            {
                SerializedProperty element = levelsProp.GetArrayElementAtIndex(i);
                element.objectReferenceValue = levels[i];
            }

            levelConfigSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(template.levelConfig);
        }

        private static void EnsureBoardRootPrefab(GameTemplateDefinition template)
        {
            GameObject boardPrefab = GameTemplateEditorUtility.TryLoadPrefab(template.boardRootPrefab);
            if (boardPrefab == null)
                return;

            string prefabPath = AssetDatabase.GetAssetPath(boardPrefab);
            if (string.IsNullOrEmpty(prefabPath))
                return;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PuzzleBoardLayout layout = prefabRoot.GetComponent<PuzzleBoardLayout>();
                if (layout == null)
                    layout = prefabRoot.AddComponent<PuzzleBoardLayout>();

                Undo.RegisterFullObjectHierarchyUndo(prefabRoot, "Apply Game Template Board Layout");

                layout.mechanicType = template.mechanicType;

                if (template.mechanicType == PuzzleMechanicType.Grid)
                {
                    layout.rows = 4;
                    layout.columns = 4;
                }
                else
                {
                    layout.laneCount = 3;
                    layout.depth = 5;
                }

                layout.RebuildLayout();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void DrawAppliedSetupStatus()
        {
            GameManager gameManager = FindGameManagerInOpenScenes();
            if (gameManager == null)
            {
                EditorGUILayout.HelpBox("Open SampleScene (or any scene with GameManager) to clear an applied template.", MessageType.None);
                return;
            }

            string scenePath = gameManager.gameObject.scene.path;
            GameTemplateApplySnapshot snapshot = GameTemplateApplySnapshotStore.LoadForScene(scenePath);

            if (snapshot == null)
            {
                EditorGUILayout.HelpBox("No applied template snapshot for this scene. Apply a template first.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Applied on this scene: {snapshot.appliedTemplateId}\n" +
                "Clear Applied Setup restores GameManager bindings and undoes Apply changes to LevelConfig, LevelData, and board prefabs.",
                MessageType.Info);
        }

        private static bool HasAppliedSetupInOpenScene()
        {
            GameManager gameManager = FindGameManagerInOpenScenes();
            if (gameManager == null)
                return false;

            return GameTemplateApplySnapshotStore.HasSnapshotForScene(gameManager.gameObject.scene.path);
        }

        private static bool CanClearAppliedTemplate(GameTemplateDefinition template, out string reason)
        {
            reason = null;

            if (!HasAppliedSetupInOpenScene())
            {
                reason = "No applied template snapshot for the open scene.";
                return false;
            }

            GameManager gameManager = FindGameManagerInOpenScenes();
            GameTemplateApplySnapshot snapshot =
                GameTemplateApplySnapshotStore.LoadForScene(gameManager.gameObject.scene.path);

            if (snapshot == null)
            {
                reason = "No snapshot found.";
                return false;
            }

            // Sadece snapshot'taki template'in Clear butonu aktif olsun.
            // (Birden fazla template aynı GameData/LevelConfig'i paylaşabildiği için
            //  IsGameManagerUsingTemplate'e güvenmiyoruz.)
            if (snapshot.appliedTemplateId == template.templateId)
                return true;

            reason = $"Last applied template was \"{snapshot.appliedTemplateId}\".";
            return false;
        }

        private static bool IsGameManagerUsingTemplate(GameManager gameManager, GameTemplateDefinition template)
        {
            SerializedObject so = new SerializedObject(gameManager);
            UnityEngine.Object gameData = so.FindProperty("gameData").objectReferenceValue;
            UnityEngine.Object levelConfig = so.FindProperty("levelConfig").objectReferenceValue;
            return gameData == template.gameData && levelConfig == template.levelConfig;
        }

        private static void ClearAppliedSetup(GameTemplateDefinition template = null)
        {
            GameManager gameManager = FindGameManagerInOpenScenes();
            if (gameManager == null)
            {
                EditorUtility.DisplayDialog("Clear Applied Setup", "GameManager was not found in open scenes.", "OK");
                return;
            }

            string scenePath = gameManager.gameObject.scene.path;
            GameTemplateApplySnapshot snapshot = GameTemplateApplySnapshotStore.LoadForScene(scenePath);
            if (snapshot == null)
            {
                EditorUtility.DisplayDialog(
                    "Clear Applied Setup",
                    "Nothing to clear. No Apply Template snapshot exists for this scene.",
                    "OK");
                return;
            }

            if (template != null &&
                snapshot.appliedTemplateId != template.templateId &&
                !IsGameManagerUsingTemplate(gameManager, template))
            {
                EditorUtility.DisplayDialog(
                    "Clear Applied Setup",
                    $"The open scene last had \"{snapshot.appliedTemplateId}\" applied, not \"{template.displayName}\".",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Applied Setup",
                "This will undo Apply Template for the open scene:\n" +
                "- GameManager GameData / LevelConfig references\n" +
                "- LevelConfig level list changes\n" +
                "- LevelData prefab assignments made by Apply\n" +
                "- Generated grid/lane slots on board prefabs\n" +
                "- Spawned board objects left in the scene\n\n" +
                "Template assets under Assets/[Templates] are kept.",
                "Clear",
                "Cancel");

            if (!confirmed)
                return;

            DestroySpawnedBoardsInOpenScenes();
            GameTemplateApplySnapshotUtility.Restore(snapshot, gameManager);

            // "Hiçbir template applied değil" durumunu garantilemek için GameManager'ın
            // gameData/levelConfig referanslarını null'a çek.
            Undo.RecordObject(gameManager, "Clear Applied Template Refs");
            SerializedObject so = new SerializedObject(gameManager);
            so.FindProperty("gameData").objectReferenceValue = null;
            so.FindProperty("levelConfig").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);

            EditorUtility.DisplayDialog(
                "Clear Applied Setup",
                "Apply Template changes were removed from the scene and related assets.",
                "OK");
        }

        private static void DestroySpawnedBoardsInOpenScenes()
        {
            PuzzleBoardLayout[] layouts = UnityEngine.Object.FindObjectsOfType<PuzzleBoardLayout>(true);
            foreach (PuzzleBoardLayout layout in layouts)
            {
                if (layout == null || EditorUtility.IsPersistent(layout))
                    continue;

                Undo.DestroyObjectImmediate(layout.gameObject);
            }
        }

        private static GameManager FindGameManagerInOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    GameManager manager = root.GetComponentInChildren<GameManager>(true);
                    if (manager != null)
                        return manager;
                }
            }

            return null;
        }
    }
}
