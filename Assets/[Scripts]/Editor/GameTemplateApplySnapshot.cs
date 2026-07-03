using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GAITemplate.Editor
{
    [Serializable]
    internal class GameTemplateApplySnapshot
    {
        public string scenePath;
        public string appliedTemplateId;
        public string previousGameDataGuid;
        public string previousLevelConfigGuid;
        public string modifiedLevelConfigGuid;
        public string[] modifiedLevelConfigLevelGuids = Array.Empty<string>();
        public LevelDataPrefabEntry[] levelDataPrefabs = Array.Empty<LevelDataPrefabEntry>();
        public BoardPrefabEntry[] boardPrefabs = Array.Empty<BoardPrefabEntry>();
    }

    [Serializable]
    internal class LevelDataPrefabEntry
    {
        public string levelDataGuid;
        public string previousLevelPrefabGuid;
    }

    [Serializable]
    internal class BoardPrefabEntry
    {
        public string prefabAssetPath;
        public bool hadPuzzleBoardLayout;
    }

    internal static class GameTemplateApplySnapshotStore
    {
        private const string EditorPrefsKeyPrefix = "GAITemplate_ApplySnapshot_";

        public static void Save(GameTemplateApplySnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.scenePath))
                return;

            string key = GetKey(snapshot.scenePath);
            string json = JsonUtility.ToJson(snapshot);
            EditorPrefs.SetString(key, json);
        }

        public static GameTemplateApplySnapshot LoadForScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return null;

            string key = GetKey(scenePath);
            if (!EditorPrefs.HasKey(key))
                return null;

            string json = EditorPrefs.GetString(key);
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<GameTemplateApplySnapshot>(json);
        }

        public static void ClearForScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return;

            EditorPrefs.DeleteKey(GetKey(scenePath));
        }

        public static bool HasSnapshotForScene(string scenePath) =>
            LoadForScene(scenePath) != null;

        private static string GetKey(string scenePath) =>
            EditorPrefsKeyPrefix + scenePath.Replace('\\', '/');
    }

    internal static class GameTemplateApplySnapshotUtility
    {
        public static GameTemplateApplySnapshot CaptureBeforeApply(GameManager gameManager, GameTemplateDefinition template)
        {
            SerializedObject gameManagerSo = new SerializedObject(gameManager);
            var snapshot = new GameTemplateApplySnapshot
            {
                scenePath = gameManager.gameObject.scene.path,
                appliedTemplateId = template.templateId,
                previousGameDataGuid = GetGuid(gameManagerSo.FindProperty("gameData").objectReferenceValue),
                previousLevelConfigGuid = GetGuid(gameManagerSo.FindProperty("levelConfig").objectReferenceValue)
            };

            if (template.levelConfig != null)
            {
                snapshot.modifiedLevelConfigGuid = GetGuid(template.levelConfig);
                snapshot.modifiedLevelConfigLevelGuids = GetLevelGuids(template.levelConfig.levels);
            }

            snapshot.levelDataPrefabs = System.Array.Empty<LevelDataPrefabEntry>();

            var boardEntries = new List<BoardPrefabEntry>();
            GameObject boardPrefab = GameTemplateEditorUtility.TryLoadPrefab(template.boardRootPrefab);
            if (boardPrefab != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(boardPrefab);
                boardEntries.Add(new BoardPrefabEntry
                {
                    prefabAssetPath = prefabPath,
                    hadPuzzleBoardLayout = boardPrefab.GetComponent<PuzzleBoardLayout>() != null
                });
            }

            snapshot.boardPrefabs = boardEntries.ToArray();
            return snapshot;
        }

        public static void Restore(GameTemplateApplySnapshot snapshot, GameManager gameManager)
        {
            if (snapshot == null || gameManager == null)
                return;

            Undo.SetCurrentGroupName("Clear Applied Game Template");
            int undoGroup = Undo.GetCurrentGroup();

            GameData previousGameData = LoadAssetByGuid<GameData>(snapshot.previousGameDataGuid);
            LevelConfig previousLevelConfig = LoadAssetByGuid<LevelConfig>(snapshot.previousLevelConfigGuid);

            Undo.RecordObject(gameManager, "Clear Applied Game Template");
            SerializedObject gameManagerSo = new SerializedObject(gameManager);
            gameManagerSo.FindProperty("gameData").objectReferenceValue = previousGameData;
            gameManagerSo.FindProperty("levelConfig").objectReferenceValue = previousLevelConfig;
            gameManagerSo.ApplyModifiedProperties();

            RestoreLevelConfig(snapshot);
            RestoreLevelDataPrefabs(snapshot);
            RestoreBoardPrefabs(snapshot);

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);
            GameTemplateApplySnapshotStore.ClearForScene(snapshot.scenePath);

            Debug.Log($"[GAITemplate] Cleared applied template \"{snapshot.appliedTemplateId}\" from scene.", gameManager);
        }

        private static void RestoreLevelConfig(GameTemplateApplySnapshot snapshot)
        {
            LevelConfig levelConfig = LoadAssetByGuid<LevelConfig>(snapshot.modifiedLevelConfigGuid);
            if (levelConfig == null)
                return;

            Undo.RecordObject(levelConfig, "Clear Applied Game Template");

            var restoredLevels = new List<LevelData>();
            foreach (string levelGuid in snapshot.modifiedLevelConfigLevelGuids)
            {
                LevelData level = LoadAssetByGuid<LevelData>(levelGuid);
                if (level != null)
                    restoredLevels.Add(level);
            }

            levelConfig.levels = restoredLevels.ToArray();
            EditorUtility.SetDirty(levelConfig);
        }

        private static void RestoreLevelDataPrefabs(GameTemplateApplySnapshot snapshot)
        {
            foreach (LevelDataPrefabEntry entry in snapshot.levelDataPrefabs)
            {
                LevelData levelData = LoadAssetByGuid<LevelData>(entry.levelDataGuid);
                if (levelData == null)
                    continue;

                Undo.RecordObject(levelData, "Clear Applied Game Template");
                levelData.levelPrefab = LoadAssetByGuid<GameObject>(entry.previousLevelPrefabGuid);
                EditorUtility.SetDirty(levelData);
            }
        }

        private static void RestoreBoardPrefabs(GameTemplateApplySnapshot snapshot)
        {
            foreach (BoardPrefabEntry entry in snapshot.boardPrefabs)
            {
                if (string.IsNullOrEmpty(entry.prefabAssetPath))
                    continue;

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(entry.prefabAssetPath);
                try
                {
                    Undo.RegisterFullObjectHierarchyUndo(prefabRoot, "Clear Applied Game Template");

                    Transform slots = prefabRoot.transform.Find("Slots");
                    if (slots != null)
                        Undo.DestroyObjectImmediate(slots.gameObject);

                    PuzzleBoardLayout layout = prefabRoot.GetComponent<PuzzleBoardLayout>();
                    if (layout != null && !entry.hadPuzzleBoardLayout)
                        Undo.DestroyObjectImmediate(layout);

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, entry.prefabAssetPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private static string[] GetLevelGuids(LevelData[] levels)
        {
            if (levels == null || levels.Length == 0)
                return Array.Empty<string>();

            var guids = new List<string>();
            foreach (LevelData level in levels)
            {
                string guid = GetGuid(level);
                if (!string.IsNullOrEmpty(guid))
                    guids.Add(guid);
            }

            return guids.ToArray();
        }

        private static string GetGuid(UnityEngine.Object asset)
        {
            if (asset == null)
                return string.Empty;

            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
