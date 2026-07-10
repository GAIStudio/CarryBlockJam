using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomPropertyDrawer(typeof(CarryBlockJamExitDefinition))]
    public class CarryBlockJamExitDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty goalsProperty = property.FindPropertyRelative("goals");
            if (goalsProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Missing goals");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(
                position,
                goalsProperty,
                new GUIContent(label.text, "Exit goal colors and plate counts"),
                true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty goalsProperty = property.FindPropertyRelative("goals");
            if (goalsProperty == null)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUI.GetPropertyHeight(goalsProperty, true);
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
