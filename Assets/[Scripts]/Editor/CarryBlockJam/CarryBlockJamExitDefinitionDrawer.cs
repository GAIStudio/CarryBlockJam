using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomPropertyDrawer(typeof(CarryBlockJamExitDefinition))]
    public class CarryBlockJamExitDefinitionDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            line.y += EditorGUIUtility.singleLineHeight + Spacing;

            SerializedProperty rowProperty = property.FindPropertyRelative("row");
            SerializedProperty columnProperty = property.FindPropertyRelative("column");
            if (rowProperty != null && columnProperty != null)
            {
                float half = (line.width - 4f) * 0.5f;
                Rect rowRect = new Rect(line.x, line.y, half, line.height);
                Rect colRect = new Rect(line.x + half + 4f, line.y, half, line.height);
                EditorGUI.PropertyField(rowRect, rowProperty, new GUIContent("Row"));
                EditorGUI.PropertyField(colRect, columnProperty, new GUIContent("Col"));
                line.y += EditorGUIUtility.singleLineHeight + Spacing;
            }

            SerializedProperty scaleProperty = property.FindPropertyRelative("modelScale");
            if (scaleProperty != null)
            {
                EditorGUI.PropertyField(line, scaleProperty);
                line.y += EditorGUI.GetPropertyHeight(scaleProperty, true) + Spacing;
            }

            SerializedProperty goalsProperty = property.FindPropertyRelative("goals");
            if (goalsProperty != null)
            {
                EditorGUI.PropertyField(
                    line,
                    goalsProperty,
                    new GUIContent("Goals", "Exit goal colors and plate counts"),
                    true);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            height += Spacing + EditorGUIUtility.singleLineHeight; // row/col
            SerializedProperty scaleProperty = property.FindPropertyRelative("modelScale");
            if (scaleProperty != null)
                height += Spacing + EditorGUI.GetPropertyHeight(scaleProperty, true);

            SerializedProperty goalsProperty = property.FindPropertyRelative("goals");
            if (goalsProperty != null)
                height += Spacing + EditorGUI.GetPropertyHeight(goalsProperty, true);

            return height;
        }
    }

    [CustomPropertyDrawer(typeof(CarryBlockJamExitGoal))]
    public class CarryBlockJamExitGoalDrawer : PropertyDrawer
    {
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty colorProperty = property.FindPropertyRelative("color");
            SerializedProperty countProperty = property.FindPropertyRelative("requiredPlateCount");
            if (colorProperty == null || countProperty == null)
                return;

            EditorGUI.BeginProperty(position, label, property);

            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            PieceColorType color = GateColorEditorUtility.Sanitize((PieceColorType)colorProperty.intValue);
            if (colorProperty.intValue != (int)color)
                colorProperty.intValue = (int)color;

            PieceColorType selected = GateColorEditorUtility.DrawPopup(line, "Color", color);
            if (selected != color)
                colorProperty.intValue = (int)selected;

            line.y += EditorGUIUtility.singleLineHeight + Spacing;
            EditorGUI.PropertyField(line, countProperty, new GUIContent("Plate Count"));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + Spacing;
        }
    }
}
