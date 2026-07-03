using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace GAITemplate
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class Panel : MonoBehaviour
    {
        private CanvasGroup _group;
        public CanvasGroup group
        {
            get
            {
                if (_group == null)
                    _group = GetComponent<CanvasGroup>();
                return _group;
            }
        }

        public void Active(bool isActive)
        {
            group.alpha = isActive ? 1 : 0;
            gameObject.SetActive(isActive);
        }

        public void ActiveSmooth(bool isActive, float duration = 0.5f, UnityAction onComplete = null)
        {
            if (isActive)
            {
                gameObject.SetActive(true);
                group.DOFade(1f, duration).OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                group.DOFade(0f, duration).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
            }
        }
    }
}