using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    internal static class CarryBlockJamFixedExitsEditorUtility
    {
        public static void DrawFixedExits(SerializedProperty exitsProperty, int columns)
        {
            DrawExits(exitsProperty, rows: 8, columns);
        }

        public static void DrawExits(SerializedProperty exitsProperty, int rows, int columns)
        {
            if (exitsProperty == null || !exitsProperty.isArray)
                return;

            NormalizeArray(exitsProperty, rows, columns);

            EditorGUILayout.LabelField("Exits (Optional Gates)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Exits are optional. Each gate covers one grid cell — Row / Col are 0-based (0 = first). " +
                "Unused gate models are hidden. Gate color follows the current goal.",
                MessageType.Info);

            for (int i = 0; i < exitsProperty.arraySize; i++)
            {
                SerializedProperty exitProperty = exitsProperty.GetArrayElementAtIndex(i);
                SyncLayoutToProperty(exitProperty, rows, columns);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Exit {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    exitsProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                SerializedProperty rowProperty = exitProperty.FindPropertyRelative("row");
                SerializedProperty columnProperty = exitProperty.FindPropertyRelative("column");
                if (rowProperty != null && columnProperty != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Position", GUILayout.Width(56f));
                    EditorGUILayout.LabelField("Row", GUILayout.Width(28f));
                    int displayRow = Mathf.Clamp(rowProperty.intValue, 0, Mathf.Max(0, rows - 1));
                    int displayCol = Mathf.Clamp(columnProperty.intValue, 0, Mathf.Max(0, columns - 1));
                    displayRow = Mathf.Clamp(
                        EditorGUILayout.IntField(displayRow, GUILayout.Width(48f)),
                        0,
                        Mathf.Max(0, rows - 1));
                    EditorGUILayout.LabelField("Col", GUILayout.Width(24f));
                    displayCol = Mathf.Clamp(
                        EditorGUILayout.IntField(displayCol, GUILayout.Width(48f)),
                        0,
                        Mathf.Max(0, columns - 1));
                    rowProperty.intValue = displayRow;
                    columnProperty.intValue = displayCol;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                        SyncLayoutToProperty(exitProperty, rows, columns);
                }

                SerializedProperty sideProperty = exitProperty.FindPropertyRelative("side");
                if (sideProperty != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.EnumPopup("Border Side", (BoardBorderSide)sideProperty.intValue);
                    EditorGUI.EndDisabledGroup();
                }

                SerializedProperty scaleProperty = exitProperty.FindPropertyRelative("modelScale");
                if (scaleProperty != null)
                {
                    if (scaleProperty.vector3Value == Vector3.zero)
                        scaleProperty.vector3Value = Vector3.one;
                    EditorGUILayout.PropertyField(scaleProperty, new GUIContent("Model Scale"));
                }

                SerializedProperty goalsProperty = exitProperty.FindPropertyRelative("goals");
                if (goalsProperty != null)
                    DrawGoals(goalsProperty);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("+ Add Exit", GUILayout.Height(24f)))
            {
                int index = exitsProperty.arraySize;
                exitsProperty.arraySize++;
                SerializedProperty exitProperty = exitsProperty.GetArrayElementAtIndex(index);
                exitProperty.FindPropertyRelative("row").intValue = 0;
                exitProperty.FindPropertyRelative("column").intValue = Mathf.Clamp(columns / 2, 0, Mathf.Max(0, columns - 1));
                exitProperty.FindPropertyRelative("modelScale").vector3Value = Vector3.one;
                SerializedProperty goalsProperty = exitProperty.FindPropertyRelative("goals");
                goalsProperty.arraySize = 1;
                SerializedProperty goal = goalsProperty.GetArrayElementAtIndex(0);
                goal.FindPropertyRelative("color").intValue = (int)GateColorEditorUtility.GetDefaultColor();
                goal.FindPropertyRelative("requiredPlateCount").intValue = 3;
                SyncLayoutToProperty(exitProperty, rows, columns);
            }
        }

        public static void EnsureArraySize(SerializedProperty exitsProperty, int columns)
        {
            NormalizeArray(exitsProperty, rows: 8, columns);
        }

        private static void NormalizeArray(SerializedProperty exitsProperty, int rows, int columns)
        {
            if (exitsProperty == null || !exitsProperty.isArray)
                return;

            for (int i = 0; i < exitsProperty.arraySize; i++)
            {
                SerializedProperty exitProperty = exitsProperty.GetArrayElementAtIndex(i);
                SyncLayoutToProperty(exitProperty, rows, columns);

                SerializedProperty goalsProperty = exitProperty.FindPropertyRelative("goals");
                if (goalsProperty != null && goalsProperty.arraySize == 0)
                {
                    goalsProperty.arraySize = 1;
                    SerializedProperty goal = goalsProperty.GetArrayElementAtIndex(0);
                    goal.FindPropertyRelative("color").intValue = (int)GateColorEditorUtility.GetDefaultColor();
                    goal.FindPropertyRelative("requiredPlateCount").intValue = 3;
                }
            }
        }

        private static void SyncLayoutToProperty(SerializedProperty exitProperty, int rows, int columns)
        {
            if (exitProperty == null)
                return;

            SerializedProperty rowProperty = exitProperty.FindPropertyRelative("row");
            SerializedProperty columnProperty = exitProperty.FindPropertyRelative("column");
            SerializedProperty sideProperty = exitProperty.FindPropertyRelative("side");
            SerializedProperty startIndexProperty = exitProperty.FindPropertyRelative("startIndex");
            SerializedProperty lengthProperty = exitProperty.FindPropertyRelative("length");
            SerializedProperty offsetProperty = exitProperty.FindPropertyRelative("positionOffset");
            SerializedProperty rotationProperty = exitProperty.FindPropertyRelative("rotation");
            SerializedProperty scaleProperty = exitProperty.FindPropertyRelative("modelScale");

            int safeRows = Mathf.Max(1, rows);
            int safeColumns = Mathf.Max(1, columns);

            int row = rowProperty != null ? rowProperty.intValue : -1;
            int column = columnProperty != null ? columnProperty.intValue : -1;
            BoardBorderSide side = sideProperty != null
                ? (BoardBorderSide)sideProperty.intValue
                : BoardBorderSide.Top;
            int startIndex = startIndexProperty != null ? startIndexProperty.intValue : 0;

            bool cellUnset = row < 0 || column < 0;
            bool cellDefaultZero = row == 0 && column == 0;
            bool borderLooksAuthored = side != BoardBorderSide.Top || startIndex != 0;

            // Legacy migration: row/column unset or default — derive from side/startIndex.
            if (cellUnset || (cellDefaultZero && borderLooksAuthored))
            {
                switch (side)
                {
                    case BoardBorderSide.Top:
                        row = 0;
                        column = Mathf.Clamp(startIndex, 0, safeColumns - 1);
                        break;
                    case BoardBorderSide.Bottom:
                        row = safeRows - 1;
                        column = Mathf.Clamp(startIndex, 0, safeColumns - 1);
                        break;
                    case BoardBorderSide.Left:
                        row = Mathf.Clamp(startIndex, 0, safeRows - 1);
                        column = 0;
                        break;
                    default:
                        row = Mathf.Clamp(startIndex, 0, safeRows - 1);
                        column = safeColumns - 1;
                        break;
                }
            }

            row = Mathf.Clamp(row, 0, safeRows - 1);
            column = Mathf.Clamp(column, 0, safeColumns - 1);

            int distTop = row;
            int distBottom = (safeRows - 1) - row;
            int distLeft = column;
            int distRight = (safeColumns - 1) - column;
            int minEdge = Mathf.Min(Mathf.Min(distTop, distBottom), Mathf.Min(distLeft, distRight));

            BoardBorderSide resolvedSide;
            int resolvedStart;
            if (minEdge == distTop)
            {
                resolvedSide = BoardBorderSide.Top;
                row = 0;
                resolvedStart = column;
            }
            else if (minEdge == distBottom)
            {
                resolvedSide = BoardBorderSide.Bottom;
                row = safeRows - 1;
                resolvedStart = column;
            }
            else if (minEdge == distLeft)
            {
                resolvedSide = BoardBorderSide.Left;
                column = 0;
                resolvedStart = row;
            }
            else
            {
                resolvedSide = BoardBorderSide.Right;
                column = safeColumns - 1;
                resolvedStart = row;
            }

            if (rowProperty != null)
                rowProperty.intValue = row;
            if (columnProperty != null)
                columnProperty.intValue = column;
            if (sideProperty != null)
                sideProperty.intValue = (int)resolvedSide;
            if (startIndexProperty != null)
                startIndexProperty.intValue = resolvedStart;
            if (lengthProperty != null)
                lengthProperty.intValue = 1;
            if (offsetProperty != null)
                offsetProperty.vector3Value = Vector3.zero;
            if (rotationProperty != null)
                rotationProperty.vector3Value = Vector3.zero;
            if (scaleProperty != null && scaleProperty.vector3Value == Vector3.zero)
                scaleProperty.vector3Value = Vector3.one;
        }

        private static void DrawGoals(SerializedProperty goalsProperty)
        {
            EditorGUILayout.LabelField("Goals (Color + Plates)", EditorStyles.miniBoldLabel);

            for (int i = 0; i < goalsProperty.arraySize; i++)
            {
                SerializedProperty goalProperty = goalsProperty.GetArrayElementAtIndex(i);
                SerializedProperty colorProperty = goalProperty.FindPropertyRelative("color");
                SerializedProperty countProperty = goalProperty.FindPropertyRelative("requiredPlateCount");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Goal {i + 1}", GUILayout.Width(56f));

                PieceColorType color = GateColorEditorUtility.Sanitize((PieceColorType)colorProperty.intValue);
                if (colorProperty.intValue != (int)color)
                    colorProperty.intValue = (int)color;

                PieceColorType selected = GateColorEditorUtility.DrawPopupNoLabel(color, GUILayout.MinWidth(90f));
                if (selected != color)
                    colorProperty.intValue = (int)selected;

                EditorGUILayout.LabelField("Plates", GUILayout.Width(42f));
                countProperty.intValue = Mathf.Max(1, EditorGUILayout.IntField(countProperty.intValue, GUILayout.Width(48f)));

                using (new EditorGUI.DisabledScope(goalsProperty.arraySize <= 1))
                {
                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                    {
                        goalsProperty.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Goal (Color / Plate Count)", GUILayout.Height(20f)))
            {
                int index = goalsProperty.arraySize;
                goalsProperty.arraySize++;
                SerializedProperty goal = goalsProperty.GetArrayElementAtIndex(index);
                goal.FindPropertyRelative("color").intValue = (int)GateColorEditorUtility.GetDefaultColor();
                goal.FindPropertyRelative("requiredPlateCount").intValue = 3;
            }
        }
    }
}
