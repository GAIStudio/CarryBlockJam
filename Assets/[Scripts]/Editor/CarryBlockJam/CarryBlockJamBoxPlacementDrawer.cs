using CarryBlockJam;
using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomPropertyDrawer(typeof(CarryBlockJamBoxPlacement))]
    public class CarryBlockJamBoxPlacementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty colorProperty = property.FindPropertyRelative("color");
            SerializedProperty rowProperty = property.FindPropertyRelative("row");
            SerializedProperty columnProperty = property.FindPropertyRelative("column");
            SerializedProperty hiddenProperty = property.FindPropertyRelative("isHidden");
            SerializedProperty frozenProperty = property.FindPropertyRelative("isFrozen");
            SerializedProperty unlockMovesProperty = property.FindPropertyRelative("unlockMoves");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);

            string suffix = string.Empty;
            if (hiddenProperty != null && hiddenProperty.boolValue)
                suffix = " (Hidden)";
            else if (frozenProperty != null && frozenProperty.boolValue)
                suffix = " (Frozen)";

            EditorGUI.LabelField(rowRect, label.text + suffix, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            rowRect.y += lineHeight + spacing;

            if (colorProperty != null)
            {
                PieceColorType color = (PieceColorType)colorProperty.enumValueIndex;
                color = PieceColorTypeEditorUtility.DrawPopup("Color", color);
                colorProperty.enumValueIndex = (int)color;
                rowRect.y += lineHeight + spacing;
            }

            if (rowProperty != null)
            {
                EditorGUI.PropertyField(rowRect, rowProperty);
                rowRect.y += lineHeight + spacing;
            }

            if (columnProperty != null)
            {
                EditorGUI.PropertyField(rowRect, columnProperty);
                rowRect.y += lineHeight + spacing;
            }

            if (hiddenProperty != null)
            {
                EditorGUI.PropertyField(rowRect, hiddenProperty, new GUIContent("Hidden Box"));
                rowRect.y += lineHeight + spacing;
            }

            if (frozenProperty != null)
            {
                EditorGUI.PropertyField(rowRect, frozenProperty, new GUIContent("Frozen Box"));
                rowRect.y += lineHeight + spacing;
            }

            if (unlockMovesProperty != null && frozenProperty != null && frozenProperty.boolValue)
                EditorGUI.PropertyField(rowRect, unlockMovesProperty, new GUIContent("Unlock Moves"));

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty frozenProperty = property.FindPropertyRelative("isFrozen");
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            int lines = frozenProperty != null && frozenProperty.boolValue ? 7 : 6;
            return (lineHeight + spacing) * lines;
        }
    }
}
