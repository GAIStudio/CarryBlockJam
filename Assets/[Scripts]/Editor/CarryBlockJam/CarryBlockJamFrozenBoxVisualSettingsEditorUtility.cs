using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    internal static class CarryBlockJamFrozenBoxVisualSettingsEditorUtility
    {
        public static void DrawFrozenBoxVisualSettings(string title, SerializedProperty visualProperty)
        {
            if (visualProperty == null)
                return;

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("model"), new GUIContent("Ice Model"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("material"), new GUIContent("Ice Material"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("scale"), new GUIContent("Scale"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("offset"), new GUIContent("Offset"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("autoFitToBox"), new GUIContent("Auto Fit To Box"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("coverPadding"), new GUIContent("Cover Padding"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Unlock Text", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("font"), new GUIContent("Font"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("fontSize"), new GUIContent("Font Size"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("textOffset"), new GUIContent("Text Offset"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("textScale"), new GUIContent("Text Scale"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("textColor"), new GUIContent("Text Color"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("bold"), new GUIContent("Bold"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("useOutline"), new GUIContent("Use Outline"));
            EditorGUILayout.EndVertical();
        }
    }
}
