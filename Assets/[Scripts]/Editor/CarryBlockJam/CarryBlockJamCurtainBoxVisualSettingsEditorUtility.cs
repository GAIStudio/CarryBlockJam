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
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("curtainMaterial"), new GUIContent("Curtain Material"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("curtainTint"), new GUIContent("Curtain Tint"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("colorSprite"), new GUIContent("Color Sprite (optional)"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("curtainScale"), new GUIContent("Curtain Scale"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("curtainOffset"), new GUIContent("Curtain Offset"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("autoFitToTable"), new GUIContent("Auto Fit To Table"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("coverPadding"), new GUIContent("Cover Padding"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Curtain Badge (Color Circle)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Badge Offset moves the colored circle on the curtain. Badge Scale controls how large it appears in the scene.",
                MessageType.None);
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("badgeOffset"), new GUIContent("Badge Offset"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("badgeScale"), new GUIContent("Badge Scale"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("badgeRotation"), new GUIContent("Badge Rotation"));
            EditorGUILayout.EndVertical();
        }
    }
}
