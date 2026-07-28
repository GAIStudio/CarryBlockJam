using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Replaces the system mouse cursor with the hand sprite used by tutorials.
    /// </summary>
    public static class HandMouseCursor
    {
        private const string ResourcePath = "Sprites/HandCursor";

        // Fingertip on the authored 512px hand art (pointing up).
        private const float SourceTipX = 171f;
        private const float SourceTipY = 28f;
        private const float SourceSize = 512f;

        // Display size relative to the imported HandCursor texture.
        private const float CursorScale = 0.62f;

        private static Texture2D _scaledCursor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyOnLoad()
        {
            Apply();
        }

        public static void Apply()
        {
            Texture2D source = Resources.Load<Texture2D>(ResourcePath);
            if (source == null)
            {
                Debug.LogWarning(
                    "[HandMouseCursor] Missing Resources/" + ResourcePath + ".");
                return;
            }

            Texture2D cursor = BuildScaledCursor(source, CursorScale);
            if (cursor == null)
                return;

            Vector2 hotspot = new Vector2(
                SourceTipX * cursor.width / SourceSize,
                SourceTipY * cursor.height / SourceSize);

            Cursor.SetCursor(cursor, hotspot, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private static Texture2D BuildScaledCursor(Texture2D source, float scale)
        {
            scale = Mathf.Clamp(scale, 0.1f, 1f);
            int width = Mathf.Max(16, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(16, Mathf.RoundToInt(source.height * scale));

            if (_scaledCursor != null &&
                _scaledCursor.width == width &&
                _scaledCursor.height == height)
                return _scaledCursor;

            if (_scaledCursor != null)
                Object.Destroy(_scaledCursor);

            _scaledCursor = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _scaledCursor.name = "HandCursorScaled";
            _scaledCursor.filterMode = FilterMode.Bilinear;
            _scaledCursor.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    pixels[y * width + x] = source.GetPixelBilinear(u, v);
                }
            }

            _scaledCursor.SetPixels(pixels);
            _scaledCursor.Apply(false, true);
            return _scaledCursor;
        }
    }
}
