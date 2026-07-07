using TMPro;
using UnityEngine;

namespace CarryBlockJam
{
    public static class CarryBlockJamExitLabelUtility
    {
        private static readonly Vector3 LabelLocalPosition = new Vector3(0f, 1.1f, 0f);
        private static readonly Vector3 LabelLocalScale = Vector3.one * 0.2f;
        private static readonly Quaternion LabelLocalRotation = Quaternion.Euler(270f, 0f, 0f);

        public static TMP_Text CreateLabel(Transform parent)
        {
            if (parent == null)
                return null;

            var labelObject = new GameObject("GoalLabel");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = LabelLocalPosition;
            labelObject.transform.localRotation = LabelLocalRotation;
            labelObject.transform.localScale = LabelLocalScale;

            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.enableWordWrapping = false;
            text.fontSize = 12f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.text = string.Empty;
            ApplyRuntimeOutline(text);
            return text;
        }

        public static void ApplyRuntimeOutline(TMP_Text text)
        {
            if (text == null || !Application.isPlaying)
                return;

            text.outlineColor = Color.black;
            text.outlineWidth = 0.2f;
        }
    }
}
