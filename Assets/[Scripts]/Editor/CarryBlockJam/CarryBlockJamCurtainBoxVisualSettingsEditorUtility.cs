using CarryBlockJam;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    internal static class CarryBlockJamCurtainBoxVisualSettingsEditorUtility
    {
        public static void DrawCurtainBoxVisualSettings(string title, SerializedProperty visualProperty)
        {
            if (visualProperty == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                visualProperty.FindPropertyRelative("colorSprite"),
                new GUIContent("Color Sprite (optional)"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Accept Color Badge", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Badge Offset / Scale / Rotation control the colored circle on the color-accept table.",
                MessageType.None);
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("badgeOffset"), new GUIContent("Badge Offset"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("badgeScale"), new GUIContent("Badge Scale"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("badgeRotation"), new GUIContent("Badge Rotation"));
            EditorGUILayout.EndVertical();
        }
    }
}
