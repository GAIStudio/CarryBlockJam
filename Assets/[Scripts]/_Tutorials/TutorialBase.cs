using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GAITemplate
{
    public abstract class TutorialBase : Panel
    {
        protected RectTransform   handRect;
        protected RectTransform   textRect;
        protected TextMeshProUGUI instructionText;
        private   string          playerPrefs;

        protected void Construct(string _playerPrefs, int _count = 1)
        {
            playerPrefs = _playerPrefs;

            if (GetCount() >= _count) DestroyImmediate(this);

            handRect        = UIManager.instance.tutorialPanel.hand;
            textRect        = UIManager.instance.tutorialPanel.instruction.rectTransform;
            instructionText = UIManager.instance.tutorialPanel.instruction;

            handRect.localScale = Vector3.one;
            Image handImage = handRect.GetComponentInChildren<Image>(true);
            if (handImage != null) handImage.color = Color.white;

            textRect.localScale  = Vector3.one;
            instructionText.color = Color.white;
        }

        private int GetCount()
        {
            return PlayerPrefs.GetInt(playerPrefs, 0);
        }

        protected void Activate()
        {
            UIManager.instance.tutorialPanel.ActiveSmooth(true);
        }

        protected void Deactivate()
        {
            textRect.DOKill();
            handRect.DOKill();
            PlayerPrefs.SetInt(playerPrefs, PlayerPrefs.GetInt(playerPrefs, 0) + 1);
            UIManager.instance.tutorialPanel.ActiveSmooth(false, 0.5f, () =>
            {
                Destroy(this);
            });
        }
    }
}
