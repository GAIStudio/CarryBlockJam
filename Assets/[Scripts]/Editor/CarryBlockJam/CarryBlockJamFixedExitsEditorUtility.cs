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
                "Choose Border Side to pick the art model: Top→M_GateUp, Bottom→M_GateBottom, " +
                "Left→M_GateLeft, Right→M_GateRight. Prefab Settings control shared side offset/scale; " +
                "rotation, per-exit scale, and goals are authored here.",
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
                    {
                        // Moving the cell should pick the matching border model.
                        // On corners prefer Left/Right so a top-right cell becomes Right,
                        // not a 3rd Top that has no free M_GateUp slot.
                        ResolveSideFromCellPreferVertical(exitProperty, rows, columns);
                    }
                }

                SerializedProperty sideProperty = exitProperty.FindPropertyRelative("side");
                if (sideProperty != null)
                {
                    EditorGUI.BeginChangeCheck();
                    BoardBorderSide selectedSide = (BoardBorderSide)EditorGUILayout.EnumPopup(
                        new GUIContent(
                            "Border Side",
                            "Top/Bottom use M_GateUp/M_GateBottom. Left/Right use M_GateLeft/M_GateRight."),
                        (BoardBorderSide)sideProperty.intValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        sideProperty.intValue = (int)selectedSide;
                        ApplySideToCell(exitProperty, selectedSide, rows, columns);
                    }
                }

                SerializedProperty rotationProperty = exitProperty.FindPropertyRelative("rotation");
                if (rotationProperty != null)
                    EditorGUILayout.PropertyField(rotationProperty, new GUIContent("Gate Rotation"));

                SerializedProperty scaleProperty = exitProperty.FindPropertyRelative("modelScale");
                if (scaleProperty != null)
                {
                    if (scaleProperty.vector3Value == Vector3.zero)
                        scaleProperty.vector3Value = Vector3.one;
                    EditorGUILayout.PropertyField(
                        scaleProperty,
                        new GUIContent(
                            "Model Scale",
                            "Per-exit multiplier on top of Prefab Settings side scale."));
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
                exitProperty.FindPropertyRelative("rotation").vector3Value = Vector3.zero;
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

            bool topTied = distTop == minEdge;
            bool bottomTied = distBottom == minEdge;
            bool leftTied = distLeft == minEdge;
            bool rightTied = distRight == minEdge;

            // Preserve an authored Left/Right (or Top/Bottom) choice on corners
            // where two borders share the same distance.
            BoardBorderSide resolvedSide;
            int resolvedStart;
            if (side == BoardBorderSide.Left && leftTied)
            {
                resolvedSide = BoardBorderSide.Left;
                column = 0;
                resolvedStart = row;
            }
            else if (side == BoardBorderSide.Right && rightTied)
            {
                resolvedSide = BoardBorderSide.Right;
                column = safeColumns - 1;
                resolvedStart = row;
            }
            else if (side == BoardBorderSide.Top && topTied)
            {
                resolvedSide = BoardBorderSide.Top;
                row = 0;
                resolvedStart = column;
            }
            else if (side == BoardBorderSide.Bottom && bottomTied)
            {
                resolvedSide = BoardBorderSide.Bottom;
                row = safeRows - 1;
                resolvedStart = column;
            }
            else
            {
                ResolveCornerPreferVertical(
                    topTied,
                    bottomTied,
                    leftTied,
                    rightTied,
                    safeRows,
                    safeColumns,
                    ref row,
                    ref column,
                    out resolvedSide,
                    out resolvedStart);
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
            if (scaleProperty != null && scaleProperty.vector3Value == Vector3.zero)
                scaleProperty.vector3Value = Vector3.one;
        }

        /// <summary>
        /// After a row/col edit, always recompute Border Side from the cell.
        /// Corner ties prefer Left/Right so side gates use M_GateLeft/Right.
        /// </summary>
        private static void ResolveSideFromCellPreferVertical(
            SerializedProperty exitProperty,
            int rows,
            int columns)
        {
            if (exitProperty == null)
                return;

            SerializedProperty rowProperty = exitProperty.FindPropertyRelative("row");
            SerializedProperty columnProperty = exitProperty.FindPropertyRelative("column");
            SerializedProperty sideProperty = exitProperty.FindPropertyRelative("side");
            SerializedProperty startIndexProperty = exitProperty.FindPropertyRelative("startIndex");
            SerializedProperty lengthProperty = exitProperty.FindPropertyRelative("length");
            if (rowProperty == null || columnProperty == null || sideProperty == null)
                return;

            int safeRows = Mathf.Max(1, rows);
            int safeColumns = Mathf.Max(1, columns);
            int row = Mathf.Clamp(rowProperty.intValue, 0, safeRows - 1);
            int column = Mathf.Clamp(columnProperty.intValue, 0, safeColumns - 1);

            int distTop = row;
            int distBottom = (safeRows - 1) - row;
            int distLeft = column;
            int distRight = (safeColumns - 1) - column;
            int minEdge = Mathf.Min(Mathf.Min(distTop, distBottom), Mathf.Min(distLeft, distRight));

            bool topTied = distTop == minEdge;
            bool bottomTied = distBottom == minEdge;
            bool leftTied = distLeft == minEdge;
            bool rightTied = distRight == minEdge;

            ResolveCornerPreferVertical(
                topTied,
                bottomTied,
                leftTied,
                rightTied,
                safeRows,
                safeColumns,
                ref row,
                ref column,
                out BoardBorderSide resolvedSide,
                out int resolvedStart);

            rowProperty.intValue = row;
            columnProperty.intValue = column;
            sideProperty.intValue = (int)resolvedSide;
            if (startIndexProperty != null)
                startIndexProperty.intValue = resolvedStart;
            if (lengthProperty != null)
                lengthProperty.intValue = 1;
        }

        private static void ResolveCornerPreferVertical(
            bool topTied,
            bool bottomTied,
            bool leftTied,
            bool rightTied,
            int safeRows,
            int safeColumns,
            ref int row,
            ref int column,
            out BoardBorderSide resolvedSide,
            out int resolvedStart)
        {
            // Prefer vertical borders on corners so Right/Left models are used.
            if (leftTied)
            {
                resolvedSide = BoardBorderSide.Left;
                column = 0;
                resolvedStart = row;
            }
            else if (rightTied)
            {
                resolvedSide = BoardBorderSide.Right;
                column = safeColumns - 1;
                resolvedStart = row;
            }
            else if (topTied)
            {
                resolvedSide = BoardBorderSide.Top;
                row = 0;
                resolvedStart = column;
            }
            else
            {
                resolvedSide = BoardBorderSide.Bottom;
                row = safeRows - 1;
                resolvedStart = column;
            }
        }

        private static void ApplySideToCell(
            SerializedProperty exitProperty,
            BoardBorderSide side,
            int rows,
            int columns)
        {
            if (exitProperty == null)
                return;

            SerializedProperty rowProperty = exitProperty.FindPropertyRelative("row");
            SerializedProperty columnProperty = exitProperty.FindPropertyRelative("column");
            SerializedProperty startIndexProperty = exitProperty.FindPropertyRelative("startIndex");
            if (rowProperty == null || columnProperty == null)
                return;

            int safeRows = Mathf.Max(1, rows);
            int safeColumns = Mathf.Max(1, columns);
            int row = Mathf.Clamp(rowProperty.intValue, 0, safeRows - 1);
            int column = Mathf.Clamp(columnProperty.intValue, 0, safeColumns - 1);

            switch (side)
            {
                case BoardBorderSide.Top:
                    row = 0;
                    break;
                case BoardBorderSide.Bottom:
                    row = safeRows - 1;
                    break;
                case BoardBorderSide.Left:
                    column = 0;
                    break;
                default:
                    column = safeColumns - 1;
                    break;
            }

            rowProperty.intValue = row;
            columnProperty.intValue = column;
            if (startIndexProperty != null)
            {
                startIndexProperty.intValue =
                    side == BoardBorderSide.Top || side == BoardBorderSide.Bottom ? column : row;
            }
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
