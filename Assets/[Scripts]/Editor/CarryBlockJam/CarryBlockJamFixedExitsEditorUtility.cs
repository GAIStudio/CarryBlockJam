using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    internal static class CarryBlockJamFixedExitsEditorUtility
    {
        public static void DrawFixedExits(SerializedProperty exitsProperty, int columns)
        {
            if (exitsProperty == null || !exitsProperty.isArray)
                return;

            EnsureArraySize(exitsProperty, columns);

            EditorGUILayout.LabelField("Exits (Fixed Gates)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Four gates are fixed on the board (2 top, 2 bottom). " +
                "Positions cannot change — only goal colors and plate counts.",
                MessageType.Info);

            for (int i = 0; i < CarryBlockJamFixedExitSlots.Count; i++)
            {
                SerializedProperty exitProperty = exitsProperty.GetArrayElementAtIndex(i);
                ApplyFixedLayoutToProperty(exitProperty, i, columns);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(CarryBlockJamFixedExitSlots.Labels[i], EditorStyles.boldLabel);

                SerializedProperty goalsProperty = exitProperty.FindPropertyRelative("goals");
                if (goalsProperty != null)
                    DrawGoals(goalsProperty);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }
        }

        public static void EnsureArraySize(SerializedProperty exitsProperty, int columns)
        {
            if (exitsProperty == null || !exitsProperty.isArray)
                return;

            while (exitsProperty.arraySize < CarryBlockJamFixedExitSlots.Count)
                exitsProperty.arraySize++;

            while (exitsProperty.arraySize > CarryBlockJamFixedExitSlots.Count)
                exitsProperty.DeleteArrayElementAtIndex(exitsProperty.arraySize - 1);

            for (int i = 0; i < CarryBlockJamFixedExitSlots.Count; i++)
            {
                SerializedProperty exitProperty = exitsProperty.GetArrayElementAtIndex(i);
                ApplyFixedLayoutToProperty(exitProperty, i, columns);

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

        private static void ApplyFixedLayoutToProperty(SerializedProperty exitProperty, int slotIndex, int columns)
        {
            if (exitProperty == null)
                return;

            int left = CarryBlockJamFixedExitSlots.GetLeftColumnIndex(columns);
            int right = CarryBlockJamFixedExitSlots.GetRightColumnIndex(columns);

            SerializedProperty sideProperty = exitProperty.FindPropertyRelative("side");
            SerializedProperty startIndexProperty = exitProperty.FindPropertyRelative("startIndex");
            SerializedProperty lengthProperty = exitProperty.FindPropertyRelative("length");
            SerializedProperty offsetProperty = exitProperty.FindPropertyRelative("positionOffset");
            SerializedProperty rotationProperty = exitProperty.FindPropertyRelative("rotation");

            switch (slotIndex)
            {
                case 0:
                    sideProperty.intValue = (int)BoardBorderSide.Top;
                    startIndexProperty.intValue = left;
                    break;
                case 1:
                    sideProperty.intValue = (int)BoardBorderSide.Top;
                    startIndexProperty.intValue = right;
                    break;
                case 2:
                    sideProperty.intValue = (int)BoardBorderSide.Bottom;
                    startIndexProperty.intValue = left;
                    break;
                default:
                    sideProperty.intValue = (int)BoardBorderSide.Bottom;
                    startIndexProperty.intValue = right;
                    break;
            }

            lengthProperty.intValue = 1;
            offsetProperty.vector3Value = Vector3.zero;
            rotationProperty.vector3Value = Vector3.zero;
        }

        private static void DrawGoals(SerializedProperty goalsProperty)
        {
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

                EditorGUILayout.LabelField("Count", GUILayout.Width(40f));
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

            if (GUILayout.Button("+ Add Goal Color / Count", GUILayout.Height(20f)))
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
