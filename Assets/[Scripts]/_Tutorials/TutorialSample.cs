using UnityEngine;
using DG.Tweening;

namespace GAITemplate
{
    public class TutorialSample : TutorialBase
    {
        private bool killed;
        private Tween tween;

        private void Awake()
        {
            base.Construct("tutorial-sample", 1);
        }

        private void Start()
        {
            ShowTutorial();
        }

        private void ShowTutorial()
        {
            textRect.anchoredPosition = new Vector2(0f, -250f);
            handRect.anchoredPosition = new Vector2(0f, -500f);
            handRect.eulerAngles = new Vector3(0f, 0f, 30f);

            instructionText.text = "TAP & HOLD TO CONTROL";

            textRect.DOScale(1.1f, 2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
            tween = DOTween
                    .Sequence()
                    .Append(handRect.DOScale(1.2f, 0.3f).SetEase(Ease.Linear))
                    .Append(handRect.DOScale(0.8f, 0.5f).SetEase(Ease.Linear))
                    .AppendInterval(1.5f)
                    .SetLoops(-1, LoopType.Restart);

            base.Activate();
        }

        private void KillTutorial()
        {
            killed = true;
            tween.Kill();
            base.Deactivate();
        }
    }
}