using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
namespace GAITemplate
{
    public class GamePanel : Panel
    {
        public RectTransform coinPanelRect;
        public RectTransform restartButtonRect;
        public TextMeshProUGUI levelText;
        [HideInInspector] int inGameCurrency;

        private Tween tween;
        private TextMeshProUGUI _moneyText;
        private Button _restartButton;

        private TextMeshProUGUI moneyText
        {
            get
            {
                if (_moneyText == null)
                    _moneyText = coinPanelRect.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
                return _moneyText;
            }
        }

        private Button restartButton
        {
            get
            {
                if (_restartButton == null && restartButtonRect != null)
                    _restartButton = restartButtonRect.GetComponent<Button>();
                return _restartButton;
            }
        }

        private int _lastDisplayedMoney = -1;

        private void Start()
        {
            if (GameManager.instance == null || GameManager.instance.Data == null)
                return;

            if (restartButton != null)
                restartButton.onClick.AddListener(OnClickRestartButton);

            if (levelText != null)
                levelText.text = GameManager.instance.Data.FormatLevelText(GameManager.instance.level);

            _lastDisplayedMoney = GameManager.instance.money;
            if (moneyText != null)
                moneyText.text = _lastDisplayedMoney.ToString();
        }
        private void Update()
        {
            if (GameManager.instance == null)
                return;

            // Do not override if tween is actively animating the text
            if (tween != null && tween.IsActive() && tween.IsPlaying())
            {
                int parsed;
                if (int.TryParse(moneyText.text, out parsed))
                {
                    _lastDisplayedMoney = parsed;
                }
            }
            else
            {
                int currentMoney = GameManager.instance.money;
                if (currentMoney != _lastDisplayedMoney)
                {
                    _lastDisplayedMoney = currentMoney;
                    moneyText.text = currentMoney.ToString();
                }
            }

            if (Input.GetKeyDown(KeyCode.H) && LevelManager.instance != null)
                LevelManager.instance.Fail();
        }

        public void SetMoney(float to, float duration = 0.3f)
        {
            if (tween != null) tween.Kill();

            coinPanelRect
            .DOScale(1.2f, duration * 0.5f)
            .SetEase(Ease.Linear)
            .SetLoops(2, LoopType.Yoyo);

            float startFrom = int.Parse(moneyText.text);
            tween = DOTween.To((x) => startFrom = x, startFrom, to, duration)
            .OnUpdate(() =>
            {
                moneyText.text = ((int)startFrom).ToString();
            })
            .OnComplete(() => moneyText.text = ((int)to).ToString());
        }

        public void AddMoney(int amount)
        {
            float startFrom = int.Parse(moneyText.text);
            SetMoney(inGameCurrency + amount);
        }

        private void OnClickRestartButton()
        {
            GameManager.instance.RestartScene();
        }
    }
}