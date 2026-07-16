using CarryBlockJam;
using GAITemplate;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    [CustomPropertyDrawer(typeof(CarryBlockJamPlatePlacement))]
    public class CarryBlockJamPlatePlacementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty colorProperty = property.FindPropertyRelative("color");
            SerializedProperty rowProperty = property.FindPropertyRelative("row");
            SerializedProperty columnProperty = property.FindPropertyRelative("column");
            SerializedProperty countProperty = property.FindPropertyRelative("count");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);

            EditorGUI.LabelField(rowRect, label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            rowRect.y += lineHeight + spacing;

            if (colorProperty != null)
            {
                PieceColorType color = (PieceColorType)colorProperty.enumValueIndex;
                color = PlateColorEditorUtility.DrawPopup(rowRect, "Color", color);
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

            if (countProperty != null)
            {
                EditorGUI.PropertyField(rowRect, countProperty);
                rowRect.y += lineHeight + spacing;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (lineHeight + spacing) * 5;
        }
    }

    [CustomPropertyDrawer(typeof(BoardPlatePlacement))]
    public class BoardPlatePlacementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty colorProperty = property.FindPropertyRelative("color");
            SerializedProperty rowProperty = property.FindPropertyRelative("row");
            SerializedProperty columnProperty = property.FindPropertyRelative("column");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);

            EditorGUI.LabelField(rowRect, label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            rowRect.y += lineHeight + spacing;

            if (colorProperty != null)
            {
                PieceColorType color = (PieceColorType)colorProperty.enumValueIndex;
                color = PlateColorEditorUtility.DrawPopup(rowRect, "Color", color);
                colorProperty.enumValueIndex = (int)color;
                rowRect.y += lineHeight + spacing;
            }

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
            return (lineHeight + spacing) * 4;
        }
    }

    [CustomPropertyDrawer(typeof(CarryBlockJamColorSetup))]
    public class CarryBlockJamColorSetupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty colorProperty = property.FindPropertyRelative("color");
            SerializedProperty minPlateCountProperty = property.FindPropertyRelative("minPlateCount");
            SerializedProperty maxPlateCountProperty = property.FindPropertyRelative("maxPlateCount");
            SerializedProperty minPlateStacksProperty = property.FindPropertyRelative("minPlateStacks");
            SerializedProperty maxPlateStacksProperty = property.FindPropertyRelative("maxPlateStacks");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect rowRect = new Rect(position.x, position.y, position.width, lineHeight);

            EditorGUI.LabelField(rowRect, label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            rowRect.y += lineHeight + spacing;

            if (colorProperty != null)
            {
                PieceColorType color = (PieceColorType)colorProperty.enumValueIndex;
                color = PlateColorEditorUtility.DrawPopup(rowRect, "Color", color);
                colorProperty.enumValueIndex = (int)color;
                rowRect.y += lineHeight + spacing;
            }

            if (minPlateCountProperty != null)
            {
                EditorGUI.PropertyField(rowRect, minPlateCountProperty);
                rowRect.y += lineHeight + spacing;
            }

            if (maxPlateCountProperty != null)
            {
                EditorGUI.PropertyField(rowRect, maxPlateCountProperty);
                rowRect.y += lineHeight + spacing;
            }

            if (minPlateStacksProperty != null)
            {
                EditorGUI.PropertyField(rowRect, minPlateStacksProperty);
                rowRect.y += lineHeight + spacing;
            }

            if (maxPlateStacksProperty != null)
                EditorGUI.PropertyField(rowRect, maxPlateStacksProperty);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (lineHeight + spacing) * 6;
        }
    }
}
