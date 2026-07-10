using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomEditor(typeof(CarryBlockJamRuntimePieceSpawner))]
    public class CarryBlockJamRuntimePieceSpawnerEditor : UnityEditor.Editor
    {
        private SerializedProperty _board;
        private SerializedProperty _cylinder;
        private SerializedProperty _cylinderVisualPrefab;
        private SerializedProperty _stickmanMaterial;
        private SerializedProperty _frozenTableVisual;
        private SerializedProperty _curtainTableVisual;
        private SerializedProperty _tableVisual;
        private SerializedProperty _plateVisual;
        private SerializedProperty _randomizeTables;
        private SerializedProperty _tables;
        private SerializedProperty _plates;
        private bool _showManualFallback;

        private void OnEnable()
        {
            _board = serializedObject.FindProperty("board");
            _cylinder = serializedObject.FindProperty("cylinder");
            _cylinderVisualPrefab = serializedObject.FindProperty("cylinderVisualPrefab");
            _stickmanMaterial = serializedObject.FindProperty("stickmanMaterial");
            _frozenTableVisual = serializedObject.FindProperty("frozenTableVisual");
            _curtainTableVisual = serializedObject.FindProperty("curtainTableVisual");
            _tableVisual = serializedObject.FindProperty("tableVisual");
            _plateVisual = serializedObject.FindProperty("plateVisual");
            _randomizeTables = serializedObject.FindProperty("randomizeTables");
            _tables = serializedObject.FindProperty("tables");
            _plates = serializedObject.FindProperty("plates");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_board);
            EditorGUILayout.PropertyField(_cylinder);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Stickman Visual", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(_cylinderVisualPrefab, new GUIContent("Model"));
            EditorGUILayout.PropertyField(_stickmanMaterial, new GUIContent("Material"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);
            DrawVisualSettings("Table Visual (All Levels)", _tableVisual);

            EditorGUILayout.Space(8f);
            DrawVisualSettings("Plate Visual (All Levels)", _plateVisual);

            EditorGUILayout.Space(8f);
            CarryBlockJamFrozenBoxVisualSettingsEditorUtility.DrawFrozenBoxVisualSettings(
                "Frozen Table Visual (Fallback)",
                _frozenTableVisual);

            EditorGUILayout.Space(8f);
            CarryBlockJamCurtainBoxVisualSettingsEditorUtility.DrawCurtainBoxVisualSettings(
                "Curtain Table Visual (Fallback)",
                _curtainTableVisual);

            EditorGUILayout.Space(8f);
            _showManualFallback = EditorGUILayout.Foldout(_showManualFallback, "Manual Fallback Placements", true);
            if (_showManualFallback)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_randomizeTables, new GUIContent("Randomize Tables"));
                EditorGUILayout.PropertyField(_tables, new GUIContent("Tables"), true);
                EditorGUILayout.PropertyField(_plates, true);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawVisualSettings(string title, SerializedProperty visualProperty)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("model"), new GUIContent("Model"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("material"), new GUIContent("Material"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("scale"), new GUIContent("Scale"));
            EditorGUILayout.PropertyField(visualProperty.FindPropertyRelative("offset"), new GUIContent("Offset"));
            EditorGUILayout.EndVertical();
        }
    }
}
