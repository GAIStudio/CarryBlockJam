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
            TMP_FontAsset font = ResolveFont(resolvedSettings.font);
            if (font == null)
            {
                Debug.LogWarning(
                    "[CarryBlockJam] Skipping exit label create: no TMP font available (check TMP Settings / LiberationSans SDF).");
                return null;
            }

            // Prefetch TMP Settings so Awake → LoadFontAsset does not NRE on a null settings instance.
            if (TMP_Settings.LoadDefaultSettings() == null)
            {
                Debug.LogWarning("[CarryBlockJam] Skipping exit label create: TMP Settings asset was not found.");
                return null;
            }

            var labelObject = new GameObject(LabelObjectName);
            labelObject.transform.SetParent(parent, false);

            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.font = font;
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

            TMP_FontAsset font = ResolveFont(resolvedSettings.font);
            if (font != null && text.font != font)
                text.font = font;

            if (resolvedSettings.material != null)
                text.fontSharedMaterial = resolvedSettings.material;

            text.fontSize = resolvedSettings.fontSize;
            text.fontStyle = resolvedSettings.bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = resolvedSettings.color;

            if (text.font != null && text.font.material != null)
                text.ForceMeshUpdate(true);

            ApplyOutline(text, resolvedSettings.useOutline);
        }

        public static TMP_FontAsset ResolveFont(TMP_FontAsset preferred)
        {
            if (IsUsableFont(preferred))
                return preferred;

            TMP_FontAsset fromSettings = null;
            try
            {
                if (TMP_Settings.LoadDefaultSettings() != null)
                    fromSettings = TMP_Settings.defaultFontAsset;
            }
            catch (System.Exception)
            {
                // TMP Settings resource may be missing during domain reload / early init.
            }

            if (IsUsableFont(fromSettings))
                return fromSettings;

            TMP_FontAsset fromResources = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (IsUsableFont(fromResources))
                return fromResources;

#if UNITY_EDITOR
            TMP_FontAsset fromProject = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Packages/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (IsUsableFont(fromProject))
                return fromProject;
#endif
            return null;
        }

        private static bool IsUsableFont(TMP_FontAsset font) =>
            font != null && font.material != null;

        public static void ApplyArtGateLabelPlacement(
            Transform labelTransform,
            BoardBorderSide side,
            BoardExitLabelSettings settings)
        {
            if (labelTransform == null)
                return;

            BoardExitLabelSettings resolvedSettings = settings ?? BoardExitLabelSettings.CreateDefault();
            Vector3 offset = GetArtGateLabelLocalPosition(side) + resolvedSettings.GetOffsetForSide(side);

            // Gates can be rotated per exit; keep the label offset board-aligned
            // instead of following the gate's rotated local axes.
            Transform gate = labelTransform.parent;
            if (gate != null && gate.parent != null)
                labelTransform.position = gate.position + gate.parent.TransformVector(offset);
            else
                labelTransform.localPosition = offset;

            labelTransform.localScale = resolvedSettings.scale;
            labelTransform.localRotation = Quaternion.identity;
            EnsureBillboard(labelTransform).Refresh();

            TMP_Text text = labelTransform.GetComponent<TMP_Text>();
            if (text != null)
                ApplyLabelSettings(text, resolvedSettings);
        }

        public static void ApplyRuntimeOutline(TMP_Text text, BoardExitLabelSettings settings = null)
        {
            if (text == null)
                return;

            BoardExitLabelSettings resolvedSettings = settings ?? BoardExitLabelSettings.CreateDefault();
            ApplyOutline(text, resolvedSettings.useOutline);
        }

        public static void ApplyRuntimeOutline(TMP_Text text, bool useOutline) =>
            ApplyOutline(text, useOutline);

        private static void ApplyOutline(TMP_Text text, bool useOutline)
        {
            if (text == null)
                return;

            // TMP outline mutates renderer.material and leaks instances in edit mode.
            if (!Application.isPlaying)
                return;

            if (!useOutline)
            {
                text.outlineWidth = 0f;
                return;
            }

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

            return sideOffset + settings.GetOffsetForSide(side);
        }

        private static Vector3 GetArtGateLabelLocalPosition(BoardBorderSide side)
        {
            // Sit the count on the gate face, slightly toward the board center.
            return side switch
            {
                BoardBorderSide.Top => new Vector3(0f, 0.55f, -0.35f),
                BoardBorderSide.Bottom => new Vector3(0f, 0.55f, 0.35f),
                BoardBorderSide.Left => new Vector3(0.35f, 0.55f, 0f),
                BoardBorderSide.Right => new Vector3(-0.35f, 0.55f, 0f),
                _ => new Vector3(0f, 0.55f, 0f),
            };
        }
    }
}
