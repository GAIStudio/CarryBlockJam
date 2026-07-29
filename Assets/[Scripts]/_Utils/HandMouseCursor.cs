using UnityEngine;
using UnityEngine.UI;

namespace GAITemplate
{
    /// <summary>
    /// Software hand cursor drawn in-game (Screen Space Overlay) so Unity Recorder
    /// and other captures include it. Hardware OS cursors are not part of the
    /// game framebuffer and do not appear in recordings.
    /// </summary>
    public static class HandMouseCursor
    {
        private const string ResourcePath = "Sprites/HandCursor";

        // Fingertip on the authored 512px hand art (pointing up).
        private const float SourceTipX = 171f;
        private const float SourceTipY = 28f;
        private const float SourceSize = 512f;

        // On-screen size in pixels.
        private const float CursorPixelSize = 72f;

        private static HandMouseCursorOverlay _overlay;

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

            // Hide OS cursor — Recorder cannot capture it.
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            if (_overlay != null)
                return;

            Sprite handSprite = Sprite.Create(
                source,
                new Rect(0f, 0f, source.width, source.height),
                new Vector2(SourceTipX / SourceSize, 1f - SourceTipY / SourceSize),
                100f);

            var root = new GameObject("HandMouseCursorCanvas");
            Object.DontDestroyOnLoad(root);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            root.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ConstantPixelSize;
            root.AddComponent<GraphicRaycaster>();

            var cursorObject = new GameObject("HandCursor", typeof(RectTransform));
            cursorObject.transform.SetParent(root.transform, false);

            RectTransform rect = cursorObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(SourceTipX / SourceSize, 1f - SourceTipY / SourceSize);
            rect.sizeDelta = new Vector2(CursorPixelSize, CursorPixelSize);

            Image image = cursorObject.AddComponent<Image>();
            image.sprite = handSprite;
            image.raycastTarget = false;
            image.preserveAspect = true;

            _overlay = root.AddComponent<HandMouseCursorOverlay>();
            _overlay.Initialize(rect);
        }

        private sealed class HandMouseCursorOverlay : MonoBehaviour
        {
            private RectTransform _cursor;

            public void Initialize(RectTransform cursor)
            {
                _cursor = cursor;
            }

            private void LateUpdate()
            {
                if (_cursor == null)
                    return;

                Cursor.visible = false;
                Vector3 mouse = Input.mousePosition;
                mouse.z = 0f;
                _cursor.position = mouse;
                _cursor.gameObject.SetActive(
                    mouse.x >= 0f &&
                    mouse.y >= 0f &&
                    mouse.x <= Screen.width &&
                    mouse.y <= Screen.height);
            }
        }
    }
}
