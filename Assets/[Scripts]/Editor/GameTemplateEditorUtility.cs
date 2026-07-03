using System.IO;
using UnityEditor;
using UnityEngine;

namespace GAITemplate.Editor
{
    internal static class GameTemplateEditorUtility
    {
        public static GameObject TryLoadPrefab(UnityEngine.Object reference)
        {
            if (reference == null)
                return null;

            string path = AssetDatabase.GetAssetPath(reference);
            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject NormalizePrefabReference(UnityEngine.Object reference)
        {
            return TryLoadPrefab(reference);
        }

        /// <summary>
        /// Re-saves legacy hand-authored prefab YAML into a standard Unity prefab asset.
        /// </summary>
        public static void EnsureStandardPrefabFormat(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab"))
                return;

            if (!File.Exists(prefabPath))
                return;

            string yaml = File.ReadAllText(prefabPath);
            if (yaml.Contains("!u!1001 &100100000"))
                return;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static bool RepairBrokenReferences(GameTemplateDefinition template)
        {
            if (template == null)
                return false;

            string templateAssetPath = AssetDatabase.GetAssetPath(template);
            if (string.IsNullOrEmpty(templateAssetPath))
                return false;

            string templateFolder = Path.GetDirectoryName(templateAssetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(templateFolder))
                return false;

            bool changed = false;
            GameObject boardPrefab = TryLoadPrefab(template.boardRootPrefab);

            if (boardPrefab == null)
            {
                boardPrefab = LoadDefaultBoardPrefab(templateFolder, template.mechanicType);
                if (boardPrefab != null)
                {
                    template.boardRootPrefab = boardPrefab;
                    changed = true;
                }
            }

            if (boardPrefab != null)
            {
                string boardPath = AssetDatabase.GetAssetPath(boardPrefab);
                EnsureStandardPrefabFormat(boardPath);
                boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(boardPath);
            }

            if (TryLoadPrefab(template.sampleLevelPrefab) == null && boardPrefab != null)
            {
                template.sampleLevelPrefab = boardPrefab;
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(template);

            return changed;
        }

        private static GameObject LoadDefaultBoardPrefab(string templateFolder, PuzzleMechanicType mechanicType)
        {
            string prefabName = mechanicType == PuzzleMechanicType.Grid
                ? "GridBoardRoot.prefab"
                : "SlideLaneRoot.prefab";

            string path = $"{templateFolder}/Prefabs/{prefabName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                return prefab;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { $"{templateFolder}/Prefabs" });
            foreach (string guid in guids)
            {
                string candidatePath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);
                if (candidate != null && candidate.GetComponent<PuzzleBoardLayout>() != null)
                    return candidate;
            }

            return null;
        }
    }
}
