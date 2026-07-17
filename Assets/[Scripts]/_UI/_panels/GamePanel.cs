using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using CarryBlockJam;

namespace GAITemplate
{
    public class GamePanel : Panel
    {
        public RectTransform coinPanelRect;
        public RectTransform restartButtonRect;
        public TextMeshProUGUI levelText;
        public GameObject timerPanel;
        public TextMeshProUGUI timerText;
        [HideInInspector] int inGameCurrency;

        private Tween tween;
        private TextMeshProUGUI _moneyText;
        private Button _restartButton;

        private bool _timerRunning;
        private bool _timerEnabled;
        private float _remainingSeconds;
        private bool _timerExpired;
        private bool _levelEventsBound;

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

        private void OnEnable() => BindLevelEvents();

        private void OnDisable() => UnbindLevelEvents();

        private void BindLevelEvents()
        {
            if (_levelEventsBound || LevelManager.instance == null)
                return;

            LevelManager.instance.startEvent.AddListener(SetupTimer);
            LevelManager.instance.endGameEvent.AddListener(OnEndGame);
            _levelEventsBound = true;
        }

        private void UnbindLevelEvents()
        {
            if (!_levelEventsBound || LevelManager.instance == null)
                return;

            LevelManager.instance.startEvent.RemoveListener(SetupTimer);
            LevelManager.instance.endGameEvent.RemoveListener(OnEndGame);
            _levelEventsBound = false;
        }

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

            BindLevelEvents();
            SetupTimer();
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

            UpdateTimer();
        }

        private void SetupTimer()
        {
            _timerExpired = false;
            _timerRunning = false;
            _timerEnabled = false;
            _remainingSeconds = 0f;

            CarryBlockJamLevelSettings settings = LevelManager.instance != null
                ? LevelManager.instance.currentLevelData?.carryBlockJam
                : null;

            _timerEnabled = settings != null && settings.hasTimer && settings.timeLimitSeconds > 0f;
            if (timerPanel != null)
                timerPanel.SetActive(_timerEnabled);

            if (!_timerEnabled)
            {
                if (timerText != null)
                    timerText.text = string.Empty;
                return;
            }

            _remainingSeconds = settings.timeLimitSeconds;
            _timerRunning = true;
            RefreshTimerText();
        }

        private void UpdateTimer()
        {
            if (!_timerRunning || !_timerEnabled)
                return;

            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds > 0f)
            {
                RefreshTimerText();
                return;
            }

            _remainingSeconds = 0f;
            _timerRunning = false;
            RefreshTimerText();
            TriggerTimeUpFail();
        }

        private void TriggerTimeUpFail()
        {
            if (_timerExpired)
                return;

            _timerExpired = true;

            if (LevelManager.instance != null)
            {
                LevelManager.instance.Fail();
                return;
            }

            if (UIManager.instance != null)
                UIManager.instance.EndGame(false);
        }

        private void RefreshTimerText()
        {
            if (timerText == null)
                return;

            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(_remainingSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = $"{minutes}:{seconds:00}";
        }

        private void OnEndGame(bool _)
        {
            _timerRunning = false;
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
            Haptic.MediumTaptic();
            GameManager.instance.RestartScene();
        }
    }
}
