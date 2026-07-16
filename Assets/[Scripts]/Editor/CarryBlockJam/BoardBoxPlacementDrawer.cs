using CarryBlockJam;
using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomPropertyDrawer(typeof(BoardBoxPlacement))]
    public class BoardBoxPlacementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty colorProperty = property.FindPropertyRelative("color");
            SerializedProperty rowProperty = property.FindPropertyRelative("row");
            SerializedProperty columnProperty = property.FindPropertyRelative("column");
            SerializedProperty hiddenProperty = property.FindPropertyRelative("isHidden");
            SerializedProperty frozenProperty = property.FindPropertyRelative("isFrozen");
            SerializedProperty curtainProperty = property.FindPropertyRelative("isCurtain");
            SerializedProperty curtainColorProperty = property.FindPropertyRelative("curtainColor");
            SerializedProperty unlockMovesProperty = property.FindPropertyRelative("unlockMoves");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);

            string suffix = string.Empty;
            if (hiddenProperty != null && hiddenProperty.boolValue)
                suffix = " (Hidden)";
            else if (curtainProperty != null && curtainProperty.boolValue)
                suffix = " (Curtain)";
            else if (frozenProperty != null && frozenProperty.boolValue)
                suffix = " (Frozen)";

            EditorGUI.LabelField(rowRect, label.text + suffix, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            rowRect.y += lineHeight + spacing;

            if (colorProperty != null)
            {
                PieceColorType color = (PieceColorType)colorProperty.enumValueIndex;
                color = TableColorEditorUtility.DrawPopup(rowRect, "Color", color);
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
                EditorGUI.PropertyField(rowRect, hiddenProperty, new GUIContent("Hidden Table"));
                rowRect.y += lineHeight + spacing;
            }

            if (curtainProperty != null)
            {
                EditorGUI.PropertyField(rowRect, curtainProperty, new GUIContent("Curtain Table"));
                rowRect.y += lineHeight + spacing;
            }

            if (curtainColorProperty != null && curtainProperty != null && curtainProperty.boolValue)
            {
                PieceColorType curtainColor = (PieceColorType)curtainColorProperty.enumValueIndex;
                curtainColor = PlateColorEditorUtility.DrawPopup(rowRect, "Curtain Color", curtainColor);
                curtainColorProperty.enumValueIndex = (int)curtainColor;
                rowRect.y += lineHeight + spacing;
            }

            if (frozenProperty != null)
            {
                EditorGUI.PropertyField(rowRect, frozenProperty, new GUIContent("Frozen Table"));
                rowRect.y += lineHeight + spacing;
            }

            if (unlockMovesProperty != null && frozenProperty != null && frozenProperty.boolValue)
            {
                EditorGUI.PropertyField(rowRect, unlockMovesProperty, new GUIContent("Unlock Moves"));
                rowRect.y += lineHeight + spacing;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty frozenProperty = property.FindPropertyRelative("isFrozen");
            SerializedProperty curtainProperty = property.FindPropertyRelative("isCurtain");
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            int lines = 7;
            if (curtainProperty != null && curtainProperty.boolValue)
                lines++;
            if (frozenProperty != null && frozenProperty.boolValue)
                lines++;
            return (lineHeight + spacing) * lines;
        }
    }
}
