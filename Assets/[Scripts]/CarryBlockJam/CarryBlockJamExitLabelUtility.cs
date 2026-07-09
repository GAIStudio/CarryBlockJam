using GAITemplate;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    public static class CarryBlockJamExitLabelUtility
    {
        private const string LabelObjectName = "GoalLabel";

        public static TMP_Text EnsureGoalLabel(
            Transform exitTransform,
            BoardBorderSide side,
            BoardExitLabelSettings settings)
        {
            if (exitTransform == null)
                return null;

            RemoveInvalidLabels(exitTransform);

            Transform existing = exitTransform.Find(LabelObjectName);
            if (existing != null)
            {
                TMP_Text existingText = existing.GetComponent<TextMeshPro>();
                if (existingText != null)
                {
                    ApplyLabelTransform(existing, side, settings);
                    return existingText;
                }

                DestroyLabelObject(existing.gameObject);
            }

            return CreateLabel(exitTransform, side, settings);
        }

        public static TMP_Text CreateLabel(Transform parent, BoardBorderSide side, BoardExitLabelSettings settings)
        {
            if (parent == null)
                return null;

            BoardExitLabelSettings resolvedSettings = settings ?? BoardExitLabelSettings.CreateDefault();

            var labelObject = new GameObject(LabelObjectName);
            labelObject.transform.SetParent(parent, false);

            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.enableWordWrapping = false;
            text.text = string.Empty;
            ApplyLabelSettings(text, resolvedSettings);
            ApplyLabelTransform(labelObject.transform, side, resolvedSettings);

            return text;
        }

        public static void ApplyLabelTransform(
            Transform labelTransform,
            BoardBorderSide side,
            BoardExitLabelSettings settings)
        {
            if (labelTransform == null)
                return;

            BoardExitLabelSettings resolvedSettings = settings ?? BoardExitLabelSettings.CreateDefault();
            labelTransform.localPosition = GetLabelLocalPosition(side, resolvedSettings);
            labelTransform.localScale = resolvedSettings.scale;
            labelTransform.localRotation = Quaternion.identity;
            EnsureBillboard(labelTransform).Refresh();

            TMP_Text text = labelTransform.GetComponent<TMP_Text>();
            if (text != null)
                ApplyLabelSettings(text, resolvedSettings);
        }

        public static void ApplyLabelSettings(TMP_Text text, BoardExitLabelSettings settings)
        {
            if (text == null)
                return;

            BoardExitLabelSettings resolvedSettings = settings ?? BoardExitLabelSettings.CreateDefault();

            if (resolvedSettings.font != null)
                text.font = resolvedSettings.font;
            else if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;

            if (resolvedSettings.material != null)
                text.fontSharedMaterial = resolvedSettings.material;

            text.fontSize = resolvedSettings.fontSize;
            text.fontStyle = resolvedSettings.bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = resolvedSettings.color;
            text.ForceMeshUpdate();
            ApplyRuntimeOutline(text, resolvedSettings);
        }

        public static void ApplyRuntimeOutline(TMP_Text text, BoardExitLabelSettings settings = null)
        {
            if (text == null || !Application.isPlaying)
                return;

            BoardExitLabelSettings resolvedSettings = settings ?? BoardExitLabelSettings.CreateDefault();
            if (!resolvedSettings.useOutline)
                return;

            text.outlineColor = Color.black;
            text.outlineWidth = 0.2f;
        }

        private static void RemoveInvalidLabels(Transform exitTransform)
        {
            for (int i = exitTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = exitTransform.GetChild(i);
                if (!string.Equals(child.name, LabelObjectName))
                    continue;

                if (!IsValidLabelRoot(child))
                    DestroyLabelObject(child.gameObject);
            }
        }

        private static bool IsValidLabelRoot(Transform labelTransform)
        {
            if (labelTransform == null)
                return false;

            if (labelTransform is RectTransform)
                return false;

            return labelTransform.GetComponent<TextMeshPro>() != null
                && labelTransform.GetComponent<CarryBlockJamExitLabelBillboard>() != null;
        }

        private static void DestroyLabelObject(GameObject labelObject)
        {
            if (labelObject == null)
                return;

            labelObject.transform.SetParent(null);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(labelObject);
                return;
            }
#endif
            Object.Destroy(labelObject);
        }

        private static CarryBlockJamExitLabelBillboard EnsureBillboard(Transform labelTransform)
        {
            CarryBlockJamExitLabelBillboard billboard = labelTransform.GetComponent<CarryBlockJamExitLabelBillboard>();
            if (billboard == null)
                billboard = labelTransform.gameObject.AddComponent<CarryBlockJamExitLabelBillboard>();

            return billboard;
        }

        private static Vector3 GetLabelLocalPosition(BoardBorderSide side, BoardExitLabelSettings settings)
        {
            const float faceInset = 0.09f;
            Vector3 sideOffset = side switch
            {
                BoardBorderSide.Left => new Vector3(faceInset, 0f, 0f),
                BoardBorderSide.Right => new Vector3(-faceInset, 0f, 0f),
                BoardBorderSide.Top => new Vector3(0f, 0f, -faceInset),
                BoardBorderSide.Bottom => new Vector3(0f, 0f, faceInset),
                _ => Vector3.zero,
            };

            return sideOffset + settings.offset;
        }
    }
}
