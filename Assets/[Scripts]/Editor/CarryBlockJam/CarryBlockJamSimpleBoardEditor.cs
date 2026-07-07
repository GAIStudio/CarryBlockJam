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
        private SerializedProperty _cellsRoot;
        private SerializedProperty _exitsRoot;
        private SerializedProperty _prefabSettings;

        private void OnEnable()
        {
            _cellColor = serializedObject.FindProperty("cellColor");
            _cellPrefab = serializedObject.FindProperty("cellPrefab");
            _cellsRoot = serializedObject.FindProperty("cellsRoot");
            _exitsRoot = serializedObject.FindProperty("exitsRoot");
            _prefabSettings = serializedObject.FindProperty("prefabSettings");
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
            EditorGUILayout.PropertyField(_cellsRoot);
            EditorGUILayout.PropertyField(_exitsRoot);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
