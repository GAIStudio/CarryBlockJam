using System.Collections.Generic;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    [DisallowMultipleComponent]
    public sealed class CarryBlockJamCurtainBox : MonoBehaviour
    {
        private const int CircleTextureSize = 64;

        private static Sprite _cachedCircleSprite;

        private CarryBlockJamBoardPiece _boxPiece;
        private Transform _boxVisualRoot;
        private readonly List<Renderer> _hiddenBoxRenderers = new List<Renderer>();
        private GameObject _curtainOverlay;
        private GameObject _colorCircle;
        private BoardCurtainBoxVisualSettings _visualSettings;
        private PieceColorType _curtainColor;
        private int _remainingRequiredDeliveries;
        private bool _isOpen;

        public bool IsOpen => _isOpen;
        public PieceColorType CurtainColor => _curtainColor;
        public int RemainingRequiredDeliveries => _remainingRequiredDeliveries;

        public void Bind(
            CarryBlockJamBoardPiece boxPiece,
            Transform boxVisualRoot,
            PieceColorType curtainColor,
            int requiredDeliveries,
            BoardCurtainBoxVisualSettings visualSettings)
        {
            _boxPiece = boxPiece;
            _boxVisualRoot = boxVisualRoot;
            _curtainColor = curtainColor;
            _visualSettings = visualSettings ?? BoardCurtainBoxVisualSettings.CreateDefault();
            _remainingRequiredDeliveries = Mathf.Max(0, requiredDeliveries);
            _isOpen = false;

            CreateCurtainOverlay();
            HideBoxVisual();

            if (_remainingRequiredDeliveries <= 0)
            {
                string cellLabel = _boxPiece != null
                    ? $"[{_boxPiece.Row},{_boxPiece.Column}]"
                    : "(unknown cell)";
                Debug.LogWarning(
                    $"[CarryBlockJam] Curtain box at {cellLabel} has no exit goals for {_curtainColor}; opening immediately.");
                Open();
            }
        }

        public static void NotifyPlatesDeliveredToExit(PieceColorType color, int deliveredCount)
        {
            if (!PieceColorPalette.IsPaintable(color) || deliveredCount <= 0)
                return;

            CarryBlockJamCurtainBox[] curtainBoxes = FindObjectsOfType<CarryBlockJamCurtainBox>();
            for (int i = 0; i < curtainBoxes.Length; i++)
                curtainBoxes[i].HandlePlatesDelivered(color, deliveredCount);
        }

        private void HandlePlatesDelivered(PieceColorType color, int deliveredCount)
        {
            if (_isOpen || color != _curtainColor || deliveredCount <= 0)
                return;

            _remainingRequiredDeliveries = Mathf.Max(0, _remainingRequiredDeliveries - deliveredCount);
            if (_remainingRequiredDeliveries <= 0)
                Open();
        }

        private void Open()
        {
            if (_isOpen || _boxPiece == null)
                return;

            _isOpen = true;
            _boxPiece.OpenCurtain();
            ShowBoxVisual();

            if (_curtainOverlay != null)
                Destroy(_curtainOverlay);

            if (_colorCircle != null)
                Destroy(_colorCircle);

            enabled = false;
        }

        private void CreateCurtainOverlay()
        {
            _curtainOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _curtainOverlay.name = "CurtainOverlay";
            _curtainOverlay.transform.SetParent(transform, false);
            _curtainOverlay.transform.localRotation = Quaternion.identity;
            _curtainOverlay.transform.localScale = Vector3.one;
            _curtainOverlay.transform.localPosition = Vector3.zero;

            Collider collider = _curtainOverlay.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            ApplyCurtainMaterial(_curtainOverlay);

            if (_visualSettings.autoFitToBox)
                FitCurtainOverlayToBoxVisual();
            else
            {
                _curtainOverlay.transform.localScale = _visualSettings.curtainScale;
                _curtainOverlay.transform.localPosition = _visualSettings.curtainOffset;
            }

            CreateColorCircle();
        }

        private void CreateColorCircle()
        {
            if (_colorCircle != null)
                Destroy(_colorCircle);

            _colorCircle = new GameObject("CurtainColorCircle");
            _colorCircle.transform.SetParent(transform, false);
            _colorCircle.transform.localPosition = GetColorCircleLocalPosition();
            _colorCircle.transform.localRotation = Quaternion.Euler(GetResolvedBadgeRotation());
            _colorCircle.transform.localScale = _visualSettings.GetResolvedBadgeScale();

            SpriteRenderer spriteRenderer = _colorCircle.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _visualSettings.colorSprite != null
                ? _visualSettings.colorSprite
                : GetOrCreateCircleSprite();
            spriteRenderer.color = PieceColorPalette.GetColor(_curtainColor);
            spriteRenderer.sortingOrder = 50;
        }

        private Vector3 GetResolvedBadgeRotation()
        {
            Vector3 rotation = _visualSettings.badgeRotation;
            // Existing level data may deserialize new fields as zero; default to flat on box top.
            if (Mathf.Approximately(rotation.x, 0f) &&
                Mathf.Approximately(rotation.y, 0f) &&
                Mathf.Approximately(rotation.z, 0f))
            {
                return new Vector3(90f, 180f, 0f);
            }

            return rotation;
        }

        private Vector3 GetColorCircleLocalPosition()
        {
            Vector3 offset = _visualSettings.badgeOffset;
            if (_curtainOverlay != null &&
                TryGetLocalRendererBounds(_curtainOverlay.transform, transform, out Bounds curtainBounds))
            {
                return new Vector3(
                    curtainBounds.center.x,
                    curtainBounds.max.y + 0.04f,
                    curtainBounds.center.z) + offset;
            }

            return offset;
        }

        private static Sprite GetOrCreateCircleSprite()
        {
            if (_cachedCircleSprite != null)
                return _cachedCircleSprite;

            var texture = new Texture2D(CircleTextureSize, CircleTextureSize, TextureFormat.RGBA32, false)
            {
                name = "CurtainColorCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            float center = (CircleTextureSize - 1) * 0.5f;
            float radius = center - 1f;
            float radiusSqr = radius * radius;
            float outline = radius - 2.5f;
            float outlineSqr = outline * outline;

            for (int y = 0; y < CircleTextureSize; y++)
            {
                for (int x = 0; x < CircleTextureSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distanceSqr = dx * dx + dy * dy;

                    if (distanceSqr > radiusSqr)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Soft white rim so the color reads clearly on the white curtain.
                    if (distanceSqr > outlineSqr)
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0.95f));
                    else
                        texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply(false, true);
            _cachedCircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, CircleTextureSize, CircleTextureSize),
                new Vector2(0.5f, 0.5f),
                CircleTextureSize);
            _cachedCircleSprite.name = "CurtainColorCircleSprite";
            _cachedCircleSprite.hideFlags = HideFlags.HideAndDontSave;
            return _cachedCircleSprite;
        }

        private void ApplyCurtainMaterial(GameObject overlay)
        {
            Renderer renderer = overlay != null ? overlay.GetComponent<Renderer>() : null;
            if (renderer == null)
                return;

            if (_visualSettings.curtainMaterial != null)
            {
                renderer.sharedMaterial = _visualSettings.curtainMaterial;
                return;
            }

            renderer.sharedMaterial = CreateRuntimeColorMaterial(_visualSettings.curtainTint);
        }

        private static Material CreateRuntimeColorMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            return material;
        }

        private void HideBoxVisual()
        {
            _hiddenBoxRenderers.Clear();
            if (_boxVisualRoot == null)
                return;

            Renderer[] renderers = _boxVisualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                _hiddenBoxRenderers.Add(renderer);
                renderer.enabled = false;
            }
        }

        private void ShowBoxVisual()
        {
            for (int i = 0; i < _hiddenBoxRenderers.Count; i++)
            {
                Renderer renderer = _hiddenBoxRenderers[i];
                if (renderer != null)
                    renderer.enabled = true;
            }

            _hiddenBoxRenderers.Clear();
        }

        private void FitCurtainOverlayToBoxVisual()
        {
            if (_curtainOverlay == null || _boxVisualRoot == null)
                return;

            if (!TryGetLocalRendererBounds(_boxVisualRoot, transform, out Bounds boxBounds, includeDisabled: true))
                return;

            if (!TryGetLocalRendererBounds(_curtainOverlay.transform, transform, out Bounds curtainBounds))
                return;

            Vector3 curtainSize = curtainBounds.size;
            if (curtainSize.x <= 0.0001f || curtainSize.y <= 0.0001f || curtainSize.z <= 0.0001f)
                return;

            float padding = Mathf.Max(0.1f, _visualSettings.coverPadding);
            Vector3 targetSize = boxBounds.size * padding;
            Vector3 fitScale = new Vector3(
                targetSize.x / curtainSize.x,
                targetSize.y / curtainSize.y,
                targetSize.z / curtainSize.z);

            Vector3 resolvedScale = Vector3.Scale(fitScale, _visualSettings.curtainScale);
            _curtainOverlay.transform.localScale = resolvedScale;

            if (!TryGetLocalRendererBounds(_curtainOverlay.transform, transform, out Bounds fittedBounds))
                fittedBounds = curtainBounds;

            Vector3 positionDelta = boxBounds.center - fittedBounds.center;
            _curtainOverlay.transform.localPosition = positionDelta + _visualSettings.curtainOffset;
        }

        private static bool TryGetLocalRendererBounds(
            Transform target,
            Transform root,
            out Bounds bounds,
            bool includeDisabled = false)
        {
            bounds = default;
            if (target == null || root == null)
                return false;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer is ParticleSystemRenderer)
                    continue;

                if (!renderer.enabled && !includeDisabled)
                    continue;

                Bounds worldBounds = renderer.bounds;
                Vector3 localMin = root.InverseTransformPoint(worldBounds.min);
                Vector3 localMax = root.InverseTransformPoint(worldBounds.max);
                var localBounds = new Bounds((localMin + localMax) * 0.5f, localMax - localMin);

                if (!hasBounds)
                {
                    bounds = localBounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(localBounds.min);
                bounds.Encapsulate(localBounds.max);
            }

            return hasBounds;
        }
    }
}
