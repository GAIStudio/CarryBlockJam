using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GAITemplate.Editor
{
    [InitializeOnLoad]
    internal static class ClearPlayerPrefsToolbarButton
    {
        private const string ToolbarTypeName = "UnityEditor.Toolbar";
        private const string ContainerName = "GAITemplate-ClearPlayerPrefs";
        private const string RootFieldName = "m_Root";
        private static readonly string[] PreferredZones =
        {
            "ToolbarZonePlayModes",
            "ToolbarZonePlayMode",
            "ToolbarZoneMiddle",
            "ToolbarZoneRightAlign",
        };

        private static ScriptableObject _cachedToolbar;

        static ClearPlayerPrefsToolbarButton()
        {
            EditorApplication.update += TryAttachButton;
        }

        private static void TryAttachButton()
        {
            if (_cachedToolbar == null)
                _cachedToolbar = FindToolbar();

            if (_cachedToolbar == null)
                return;

            if (!(_cachedToolbar is UnityEngine.Object toolbarObject))
                return;

            VisualElement toolbarRoot = GetToolbarRoot(toolbarObject);
            if (toolbarRoot == null)
                return;

            VisualElement targetZone = FindTargetZone(toolbarRoot);
            if (targetZone == null)
                return;

            if (targetZone.Q<IMGUIContainer>(ContainerName) != null)
                return;

            var container = new IMGUIContainer(DrawButton)
            {
                name = ContainerName,
                style =
                {
                    marginLeft = 6f,
                    marginRight = 4f,
                }
            };

            targetZone.Add(container);
        }

        private static ScriptableObject FindToolbar()
        {
            Type toolbarType = typeof(UnityEditor.Editor).Assembly.GetType(ToolbarTypeName);
            if (toolbarType == null)
                return null;

            UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            return toolbars != null && toolbars.Length > 0 ? toolbars[0] as ScriptableObject : null;
        }

        private static VisualElement GetToolbarRoot(UnityEngine.Object toolbarObject)
        {
            if (toolbarObject == null)
                return null;

            FieldInfo rootField = toolbarObject.GetType().GetField(RootFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return rootField != null ? rootField.GetValue(toolbarObject) as VisualElement : null;
        }

        private static VisualElement FindTargetZone(VisualElement toolbarRoot)
        {
            if (toolbarRoot == null)
                return null;

            for (int i = 0; i < PreferredZones.Length; i++)
            {
                VisualElement zone = toolbarRoot.Q(PreferredZones[i]);
                if (zone != null)
                    return zone;
            }

            return null;
        }

        private static void DrawButton()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear PlayerPrefs", EditorStyles.toolbarButton, GUILayout.Width(130f)))
                {
                    if (!EditorUtility.DisplayDialog(
                            "Clear PlayerPrefs",
                            "Delete all PlayerPrefs for this project?",
                            "Clear",
                            "Cancel"))
                    {
                        return;
                    }

                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("All PlayerPrefs cleared.");
                }
            }
        }
    }
}
