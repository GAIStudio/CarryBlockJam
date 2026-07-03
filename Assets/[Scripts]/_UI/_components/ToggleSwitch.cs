using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GAITemplate
{
    /// <summary>
    /// iOS tarzı bit/toggle switch. Track rengi değişir, thumb On/Off pozisyonları arasında kayar.
    ///
    /// Setup (UI prefab):
    ///  - Bir Button (boş Image) → bu komponent buraya gelir
    ///    - Child: Track Image (renk geçişi için)
    ///    - Child: Thumb RectTransform (kayan top)
    ///  - Button.OnClick → ToggleSwitch.Toggle()
    /// </summary>
    [DisallowMultipleComponent]
    public class ToggleSwitch : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Off ↔ On arasında kayan thumb (yuvarlak buton).")]
        public RectTransform thumb;

        [Tooltip("Renk değişen track (arka plan).")]
        public Image trackImage;

        [Header("Positions (anchoredPosition)")]
        public Vector2 offThumbPosition = new Vector2(-25f, 0f);
        public Vector2 onThumbPosition  = new Vector2( 25f, 0f);

        [Header("Colors")]
        public Color offTrackColor = new Color(0.65f, 0.65f, 0.65f);
        public Color onTrackColor  = new Color(0.30f, 0.80f, 0.40f);

        [Header("Animation")]
        [Min(0f)] public float animDuration = 0.18f;
        public Ease animEase = Ease.OutQuad;

        [Header("State")]
        [SerializeField] private bool isOn;

        [Header("Events")]
        public UnityEvent<bool> onValueChanged;

        public bool IsOn => isOn;

        private void OnEnable()
        {
            ApplyVisual(animate: false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyVisual(animate: false);
        }
#endif

        // ── Public API ────────────────────────────────────────────────────────────────

        /// <summary>Mevcut değeri tersine çevirir. UI Button.OnClick'e bağlanır.</summary>
        public void Toggle() => SetValue(!isOn);

        /// <summary>Değeri programatik olarak ayarlar.</summary>
        public void SetValue(bool value, bool animate = true, bool fireEvent = true)
        {
            if (isOn == value) return;
            isOn = value;
            ApplyVisual(animate);
            if (fireEvent)
                onValueChanged?.Invoke(isOn);
        }

        /// <summary>Sessizce (event tetiklemeden) initial state'i set eder — Start'ta kullanılır.</summary>
        public void SetValueWithoutEvent(bool value) => SetValue(value, animate: false, fireEvent: false);

        // ── Visual ────────────────────────────────────────────────────────────────────

        private void ApplyVisual(bool animate)
        {
            Vector2 targetPos = isOn ? onThumbPosition : offThumbPosition;
            Color   targetCol = isOn ? onTrackColor    : offTrackColor;

            if (thumb != null) thumb.DOKill();
            if (trackImage != null) trackImage.DOKill();

            if (animate && Application.isPlaying && animDuration > 0f)
            {
                if (thumb != null)
                    thumb.DOAnchorPos(targetPos, animDuration).SetEase(animEase);
                if (trackImage != null)
                    trackImage.DOColor(targetCol, animDuration).SetEase(animEase);
            }
            else
            {
                if (thumb != null) thumb.anchoredPosition = targetPos;
                if (trackImage != null) trackImage.color = targetCol;
            }
        }
    }
}
