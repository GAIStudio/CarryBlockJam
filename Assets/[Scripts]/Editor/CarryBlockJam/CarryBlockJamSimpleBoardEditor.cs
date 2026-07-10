using CarryBlockJam;
using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomEditor(typeof(CarryBlockJamSimpleBoard))]
    public class CarryBlockJamSimpleBoardEditor : UnityEditor.Editor
    {
        private SerializedProperty _cellColor;
        private SerializedProperty _cellPrefab;
        private SerializedProperty _cellMaterial;
        private SerializedProperty _cellsRoot;
        private SerializedProperty _exitsRoot;
        private SerializedProperty _prefabSettings;
        private SerializedProperty _exitLabel;

        private void OnEnable()
        {
            _cellColor = serializedObject.FindProperty("cellColor");
            _cellPrefab = serializedObject.FindProperty("cellPrefab");
            _cellMaterial = serializedObject.FindProperty("cellMaterial");
            _cellsRoot = serializedObject.FindProperty("cellsRoot");
            _exitsRoot = serializedObject.FindProperty("exitsRoot");
            _prefabSettings = serializedObject.FindProperty("prefabSettings");
            _exitLabel = serializedObject.FindProperty("exitLabel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Board layout, exits, rows, columns, spacing, and cell scale are controlled by LevelData in Level Creator.",
                MessageType.Info);

            PieceColorType cellColor = (PieceColorType)_cellColor.enumValueIndex;
            _cellColor.enumValueIndex = (int)PieceColorTypeEditorUtility.DrawPopup("Cell Color", cellColor);

            EditorGUILayout.PropertyField(_prefabSettings);
            EditorGUILayout.PropertyField(_cellPrefab);
            EditorGUILayout.PropertyField(_cellMaterial);
            EditorGUILayout.PropertyField(_cellsRoot);
            EditorGUILayout.PropertyField(_exitsRoot);

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Exit number text comes from Level Creator (required plate count per exit) and shows as x2, x3, …. " +
                "These prefab settings control label font, size, scale, and separate top/bottom offsets. " +
                "Label color follows the active gate color.",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            DrawExitLabelSettings("Exit Label Render (All Levels)", _exitLabel);
            bool labelChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (labelChanged && target is CarryBlockJamSimpleBoard board)
            {
                board.RefreshExitLabelVisuals();
                EditorUtility.SetDirty(board);
                if (!Application.isPlaying && board.gameObject.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
            }
        }

        private static void DrawExitLabelSettings(string title, SerializedProperty labelProperty)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("font"), new GUIContent("Font"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("material"), new GUIContent("Material"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("fontSize"), new GUIContent("Font Size"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("scale"), new GUIContent("Scale"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("topOffset"), new GUIContent("Top Gate Offset"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("bottomOffset"), new GUIContent("Bottom Gate Offset"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("bold"), new GUIContent("Bold"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("useOutline"), new GUIContent("Use Outline"));
            EditorGUILayout.EndVertical();
        }
    }
}
