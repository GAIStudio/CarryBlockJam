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
            if (_prefabSettings.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a CarryBlockJam Prefab Settings asset to edit Stickman Offset and Exit Model offsets.",
                    MessageType.None);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                DrawStickmanOffset(_prefabSettings.objectReferenceValue as CarryBlockJamPrefabSettings);
                DrawGateModelOffsets(_prefabSettings.objectReferenceValue as CarryBlockJamPrefabSettings);
                if (EditorGUI.EndChangeCheck() && target is CarryBlockJamSimpleBoard boardForGates)
                {
                    CarryBlockJamArtGateUtility.ApplyGateModelOffsets(boardForGates);
                    EditorUtility.SetDirty(boardForGates);
                    if (_prefabSettings.objectReferenceValue != null)
                        EditorUtility.SetDirty(_prefabSettings.objectReferenceValue);
                    if (!Application.isPlaying && boardForGates.gameObject.scene.IsValid())
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boardForGates.gameObject.scene);
                }
            }

            EditorGUILayout.PropertyField(_cellPrefab);
            EditorGUILayout.PropertyField(_cellMaterial);
            EditorGUILayout.PropertyField(_cellsRoot);
            EditorGUILayout.PropertyField(_exitsRoot);

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Exit number text comes from Level Creator (required plate count per exit) and shows as x2, x3, …. " +
                "These prefab settings control label font, size, scale, and separate offsets for each gate side. " +
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
            EditorGUILayout.PropertyField(
                labelProperty.FindPropertyRelative("material"),
                new GUIContent("TMP Material", "Optional TMP font material. Label color comes from Gate Up materials."));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("fontSize"), new GUIContent("Font Size"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("scale"), new GUIContent("Scale"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("topOffset"), new GUIContent("Top Gate Offset"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("bottomOffset"), new GUIContent("Bottom Gate Offset"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("leftOffset"), new GUIContent("Left Gate Offset"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("rightOffset"), new GUIContent("Right Gate Offset"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("bold"), new GUIContent("Bold"));
            EditorGUILayout.PropertyField(labelProperty.FindPropertyRelative("useOutline"), new GUIContent("Use Outline"));
            EditorGUILayout.HelpBox(
                "Exit label tint uses Mat_GateUp-{Color} from Assets/[Materials]/-GateUp Materials.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private static void DrawStickmanOffset(CarryBlockJamPrefabSettings settings)
        {
            if (settings == null)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Stickman", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Undo.RecordObject(settings, "Stickman Offset");
            settings.stickmanOffset = EditorGUILayout.Vector3Field("Offset", settings.stickmanOffset);
            settings.stickmanCarryOffset = EditorGUILayout.Vector3Field("Walk Anim Offset", settings.stickmanCarryOffset);
            settings.stickmanRotation = EditorGUILayout.Vector3Field("Rotation", settings.stickmanRotation);
            EditorGUILayout.HelpBox(
                "Offset is root grid height. Walk Anim Offset compensates Mixamo walk/carry vs EmptyIdle bind pose; it blends with animator transitions.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private static void DrawGateModelOffsets(CarryBlockJamPrefabSettings settings)
        {
            if (settings == null)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Exit Model Offset", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Undo.RecordObject(settings, "Exit Model Offset");
            settings.topExitModelOffset = EditorGUILayout.Vector3Field("Top", settings.topExitModelOffset);
            settings.bottomExitModelOffset = EditorGUILayout.Vector3Field("Bottom", settings.bottomExitModelOffset);
            settings.leftExitModelOffset = EditorGUILayout.Vector3Field("Left", settings.leftExitModelOffset);
            settings.rightExitModelOffset = EditorGUILayout.Vector3Field("Right", settings.rightExitModelOffset);
            EditorGUILayout.HelpBox(
                "Offsets apply according to each exit's authored border side.",
                MessageType.None);
            EditorGUILayout.EndVertical();
        }
    }
}
