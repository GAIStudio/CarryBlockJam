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

            SerializedProperty rowProperty = property.FindPropertyRelative("row");
            SerializedProperty columnProperty = property.FindPropertyRelative("column");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);

            EditorGUI.LabelField(rowRect, label.text, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            rowRect.y += lineHeight + spacing;

            if (rowProperty != null)
            {
                EditorGUI.PropertyField(rowRect, rowProperty);
                rowRect.y += lineHeight + spacing;
            }

            if (columnProperty != null)
                EditorGUI.PropertyField(rowRect, columnProperty);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (lineHeight + spacing) * 3;
        }
    }
}
