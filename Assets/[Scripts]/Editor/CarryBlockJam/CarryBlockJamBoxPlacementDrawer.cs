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

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);

            string hiddenSuffix = hiddenProperty != null && hiddenProperty.boolValue ? " (Hidden)" : string.Empty;
            EditorGUI.LabelField(rowRect, label.text + hiddenSuffix, EditorStyles.boldLabel);

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
                EditorGUI.PropertyField(rowRect, hiddenProperty, new GUIContent("Hidden Box"));

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (lineHeight + spacing) * 5f;
        }
    }
}
