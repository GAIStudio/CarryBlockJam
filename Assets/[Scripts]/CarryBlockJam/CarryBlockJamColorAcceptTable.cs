using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Table that only accepts plates of one color. Shows a color badge sprite on top
    /// (same circle art formerly used by curtain). Replaces the old curtain lock mechanic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarryBlockJamColorAcceptTable : MonoBehaviour
    {
        private const int CircleTextureSize = 64;
        private const string ColorSpritePath = "Assets/[Sprites]/ColorSprite_Cricle.png";
        private const float BadgeSurfaceClearance = 0.02f;

        private static Sprite _cachedCircleSprite;

        private CarryBlockJamBoardPiece _tablePiece;
        private Transform _tableVisualRoot;
        private GameObject _colorBadge;
        private BoardCurtainBoxVisualSettings _visualSettings;
        private PieceColorType _acceptedColor = PieceColorType.None;

        public PieceColorType AcceptedColor => _acceptedColor;

        public void RefreshVisualSettings(BoardCurtainBoxVisualSettings visualSettings)
        {
            _visualSettings = visualSettings ?? BoardCurtainBoxVisualSettings.CreateDefault();
            CreateColorBadge();
        }

        public void Bind(
            CarryBlockJamBoardPiece tablePiece,
            Transform tableVisualRoot,
            PieceColorType acceptedColor,
            BoardCurtainBoxVisualSettings visualSettings)
        {
            _tablePiece = tablePiece;
            _tableVisualRoot = tableVisualRoot;
            _acceptedColor = PieceColorPalette.IsPaintable(acceptedColor)
                ? acceptedColor
                : PieceColorType.None;
            _visualSettings = visualSettings ?? BoardCurtainBoxVisualSettings.CreateDefault();
            CreateColorBadge();
        }

        public bool AcceptsColor(PieceColorType color) =>
            PieceColorPalette.IsPaintable(_acceptedColor) &&
            PieceColorPalette.IsPaintable(color) &&
            color == _acceptedColor;

        private void CreateColorBadge()
        {
            if (_colorBadge != null)
                Destroy(_colorBadge);

            if (!PieceColorPalette.IsPaintable(_acceptedColor))
                return;

            _colorBadge = new GameObject("ColorAcceptBadge");
            _colorBadge.transform.SetParent(transform, false);
            _colorBadge.transform.localPosition = GetBadgeLocalPosition();
            _colorBadge.transform.localRotation = Quaternion.Euler(GetResolvedBadgeRotation());

            SpriteRenderer spriteRenderer = _colorBadge.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ResolveColorSprite(_visualSettings);
            spriteRenderer.color = PieceColorPalette.GetColor(_acceptedColor);
            spriteRenderer.sortingOrder = 50;
            _colorBadge.transform.localScale = GetNormalizedBadgeScale(spriteRenderer.sprite);
        }

        private Vector3 GetNormalizedBadgeScale(Sprite sprite)
        {
            Vector3 badgeScale = _visualSettings != null
                ? _visualSettings.GetResolvedBadgeScale()
                : new Vector3(0.85f, 0.85f, 0.85f);
            if (sprite == null)
                return badgeScale;

            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return spriteSize > 0.0001f ? badgeScale / spriteSize : badgeScale;
        }

        private static Sprite ResolveColorSprite(BoardCurtainBoxVisualSettings settings)
        {
            if (settings?.colorSprite != null)
                return settings.colorSprite;

            Sprite sprite = Resources.Load<Sprite>("Sprites/ColorSprite_Cricle");
#if UNITY_EDITOR
            if (sprite == null)
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(ColorSpritePath);
#endif
            if (sprite != null)
                return sprite;

            return GetOrCreateCircleSprite();
        }

        private Vector3 GetResolvedBadgeRotation()
        {
            Vector3 rotation = _visualSettings != null
                ? _visualSettings.badgeRotation
                : new Vector3(90f, 180f, 0f);
            if (rotation == Vector3.zero)
                return new Vector3(90f, 180f, 0f);
            return rotation;
        }

        private Vector3 GetBadgeLocalPosition()
        {
            Vector3 offset = _visualSettings != null ? _visualSettings.badgeOffset : Vector3.zero;
            if (_tableVisualRoot == null)
                return offset + Vector3.up * 0.55f;

            Renderer[] renderers = _tableVisualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return offset + Vector3.up * 0.55f;

            Bounds worldBounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer is SpriteRenderer)
                    continue;

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
                return offset + Vector3.up * 0.55f;

            Vector3 worldTop = new Vector3(
                worldBounds.center.x,
                worldBounds.max.y + BadgeSurfaceClearance,
                worldBounds.center.z);
            return transform.InverseTransformPoint(worldTop) + offset;
        }

        private static Sprite GetOrCreateCircleSprite()
        {
            if (_cachedCircleSprite != null)
                return _cachedCircleSprite;

            var texture = new Texture2D(CircleTextureSize, CircleTextureSize, TextureFormat.RGBA32, false)
            {
                name = "ColorAcceptCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            float center = (CircleTextureSize - 1) * 0.5f;
            float radius = center * 0.92f;
            for (int y = 0; y < CircleTextureSize; y++)
            {
                for (int x = 0; x < CircleTextureSize; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = distance >= 1f
                        ? 0f
                        : (distance < 0.75f ? 1f : 1f - ((distance - 0.75f) / 0.25f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            _cachedCircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, CircleTextureSize, CircleTextureSize),
                new Vector2(0.5f, 0.5f),
                CircleTextureSize);
            _cachedCircleSprite.name = "ColorAcceptCircleSprite";
            return _cachedCircleSprite;
        }

        private void OnDestroy()
        {
            if (_colorBadge != null)
                Destroy(_colorBadge);
        }
    }
}
