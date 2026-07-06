using CarryBlockJam;
using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomEditor(typeof(CarryBlockJamSimpleBoard))]
    public class CarryBlockJamSimpleBoardEditor : UnityEditor.Editor
    {
        private SerializedProperty _rows;
        private SerializedProperty _columns;
        private SerializedProperty _gridSpacingX;
        private SerializedProperty _gridSpacingZ;
        private SerializedProperty _cellScale;
        private SerializedProperty _cellColor;
        private SerializedProperty _cellPrefab;
        private SerializedProperty _cellsRoot;
        private SerializedProperty _exitsRoot;
        private SerializedProperty _exits;

        private void OnEnable()
        {
            _rows = serializedObject.FindProperty("rows");
            _columns = serializedObject.FindProperty("columns");
            _gridSpacingX = serializedObject.FindProperty("gridSpacingX");
            _gridSpacingZ = serializedObject.FindProperty("gridSpacingZ");
            _cellScale = serializedObject.FindProperty("cellScale");
            _cellColor = serializedObject.FindProperty("cellColor");
            _cellPrefab = serializedObject.FindProperty("cellPrefab");
            _cellsRoot = serializedObject.FindProperty("cellsRoot");
            _exitsRoot = serializedObject.FindProperty("exitsRoot");
            _exits = serializedObject.FindProperty("exits");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_rows);
            EditorGUILayout.PropertyField(_columns);
            EditorGUILayout.PropertyField(_gridSpacingX);
            EditorGUILayout.PropertyField(_gridSpacingZ);
            EditorGUILayout.PropertyField(_cellScale);

            PieceColorType cellColor = (PieceColorType)_cellColor.enumValueIndex;
            _cellColor.enumValueIndex = (int)PieceColorTypeEditorUtility.DrawPopup("Cell Color", cellColor);

            EditorGUILayout.PropertyField(_cellPrefab);
            EditorGUILayout.PropertyField(_cellsRoot);
            EditorGUILayout.PropertyField(_exitsRoot);

            EditorGUILayout.Space(6f);
            DrawExitList(_exits);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Changing values here does not update the scene until you click Build Board.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Board", GUILayout.Height(28f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    CarryBlockJamSimpleBoardBuilder.BuildBoard((CarryBlockJamSimpleBoard)target);
                }

                if (GUILayout.Button("Clear", GUILayout.Height(28f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    CarryBlockJamSimpleBoardBuilder.ClearBoard((CarryBlockJamSimpleBoard)target);
                }
            }
        }

        private static void DrawExitList(SerializedProperty exitsProperty)
        {
            if (exitsProperty == null)
                return;

            EditorGUILayout.LabelField("Exits", EditorStyles.boldLabel);

            for (int i = 0; i < exitsProperty.arraySize; i++)
            {
                SerializedProperty exitProperty = exitsProperty.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Exit {i + 1}", EditorStyles.boldLabel);
                        if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                        {
                            exitsProperty.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }

                    EditorGUILayout.PropertyField(exitProperty.FindPropertyRelative("side"));
                    EditorGUILayout.PropertyField(exitProperty.FindPropertyRelative("startIndex"));
                    EditorGUILayout.PropertyField(exitProperty.FindPropertyRelative("length"));

                    SerializedProperty colorProperty = exitProperty.FindPropertyRelative("color");
                    PieceColorType currentColor = (PieceColorType)colorProperty.enumValueIndex;
                    colorProperty.enumValueIndex = (int)PieceColorTypeEditorUtility.DrawPopup("Color", currentColor);

                    EditorGUILayout.PropertyField(exitProperty.FindPropertyRelative("positionOffset"));
                    EditorGUILayout.PropertyField(exitProperty.FindPropertyRelative("rotation"));
                }
            }

            if (GUILayout.Button("Add Exit"))
            {
                int newIndex = exitsProperty.arraySize;
                exitsProperty.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newExit = exitsProperty.GetArrayElementAtIndex(newIndex);
                newExit.FindPropertyRelative("side").enumValueIndex = (int)BoardBorderSide.Left;
                newExit.FindPropertyRelative("startIndex").intValue = 2;
                newExit.FindPropertyRelative("length").intValue = 2;
                newExit.FindPropertyRelative("color").enumValueIndex = (int)PieceColorType.Red;
                newExit.FindPropertyRelative("positionOffset").vector3Value = Vector3.zero;
                newExit.FindPropertyRelative("rotation").vector3Value = new Vector3(0f, 90f, 0f);
            }
        }
    }
}
