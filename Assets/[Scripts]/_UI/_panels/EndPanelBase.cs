using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GAITemplate
{
    /// <summary>
    /// Success / Fail end paneller için ortak iskelet.
    /// Title + Continue button reveal mantığını yönetir; SuccessPanel ek olarak reward + confetti ekler.
    /// </summary>
    public abstract class EndPanelBase : MonoBehaviour
    {
        [Header("References")]
        public Image         title;
        public RectTransform continueButton;

        [Header("Reveal Timing (seconds)")]
        public float titleDelay  = 0.00f;
        public float buttonDelay = 0.60f;

        [Header("Reveal Durations")]
        public float titleDuration  = 0.40f;
        public float buttonDuration = 0.30f;

        [Range(0f, 1f)]
        [Tooltip("Title'ın fade in olacağı hedef alpha (1 = tam opak, 0.6 = %60).")]
        public float titleTargetAlpha = 0.6f;

        [Header("Sound")]
        [Tooltip("Panel Show()'da çalınacak ses adı (SoundManager.sounds içinden). " +
                 "Boş bırakılırsa ses çalınmaz.")]
        public string soundName;

        [Tooltip("Sesin Show'dan ne kadar sonra çalınacağı.")]
        [Min(0f)] public float soundDelay = 0f;

        // ── Public API ────────────────────────────────────────────────────────────────

        private bool _shown;

        public virtual void Show()
        {
            if (_shown) return;
            _shown = true;

            gameObject.SetActive(true);
            ResetVisuals();
            BuildSequence().Play();
        }

        protected virtual void OnDisable()
        {
            _shown = false;
            DOTween.Kill(this);
        }

        public virtual void OnPressRestart()
        {
            Haptic.MediumTaptic();
            if (GameManager.instance != null)
                GameManager.instance.RestartScene();
        }

        // ── Reveal pipeline ───────────────────────────────────────────────────────────

        protected virtual void ResetVisuals()
        {
            if (title != null)
            {
                Color c = title.color; c.a = 0f; title.color = c;
            }

            if (continueButton != null)
                continueButton.localScale = Vector3.zero;
        }

        protected virtual Sequence BuildSequence()
        {
            DOTween.Kill(this);

            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.InsertCallback(titleDelay,  RevealTitle);
            seq.InsertCallback(buttonDelay, RevealButton);

            if (!string.IsNullOrEmpty(soundName))
                seq.InsertCallback(soundDelay, PlaySound);

            return seq;
        }

        private void PlaySound()
        {
            if (SoundManager.instance != null)
                SoundManager.instance.Play(soundName);
        }

        protected virtual void RevealTitle()
        {
            if (title == null) return;
            title.DOFade(titleTargetAlpha, titleDuration);
        }

        protected virtual void RevealButton()
        {
            if (continueButton == null) return;
            continueButton.DOScale(1f, buttonDuration).SetEase(Ease.OutBack);
        }
    }
}
