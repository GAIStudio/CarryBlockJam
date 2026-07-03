using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Level başında, levelData.hasTutorial true ise tutorial sahnesini oynatır.
    /// Stage'lere göre hand/instruction'ı günceller, sadece izin verilen cell'lerin
    /// tıklanmasına izin verir, her başarılı tıklamada sonraki stage'e geçer.
    /// </summary>
    [DisallowMultipleComponent]
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        public bool IsActive { get; private set; }

        [Header("Hand Animation")]
        [Tooltip("Parmak/el başlangıç scale'i (yoyo'nun bir ucu).")]
        public float handVisualStartScale = 1f;

        [Tooltip("Parmak/el hedef scale'i (yoyo'nun diğer ucu).")]
        public float handVisualScale = 0.8f;

        [Tooltip("Circle vurgu başlangıç scale'i.")]
        public float circleVisualStartScale = 1f;

        [Tooltip("Circle vurgu hedef scale'i.")]
        public float circleVisualScale = 0.5f;

        [Tooltip("Yoyo tween süresi.")]
        public float pulseDuration = 0.5f;

        private LevelData     _levelData;
        private TutorialPanel _panel;
        private int           _stageIndex = -1;
        private Camera        _gameCamera;

        private Tween _handTween;
        private Tween _circleTween;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Scene reload sonrası garanti reset.
            IsActive    = false;
            _stageIndex = -1;
            _levelData  = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private TutorialStage CurrentStage =>
            (_levelData != null && _stageIndex >= 0 && _stageIndex < _levelData.tutorialStages.Count)
                ? _levelData.tutorialStages[_stageIndex] : null;

        // ── Lifecycle ────────────────────────────────────────────────────────────────

        private const string PrefKeyPrefix = "Tutorial_";

        public static bool IsCompleted(LevelData levelData) =>
            levelData != null && PlayerPrefs.GetInt(PrefKeyPrefix + levelData.name, 0) >= 1;

        public static void ResetCompletion(LevelData levelData)
        {
            if (levelData == null) return;
            PlayerPrefs.DeleteKey(PrefKeyPrefix + levelData.name);
            PlayerPrefs.Save();
        }

        public void StartTutorial(LevelData levelData)
        {
            Cleanup(); // önceki tutorial varsa state'i sıfırla

            if (levelData == null || !levelData.hasTutorial) return;
            if (levelData.tutorialStages == null || levelData.tutorialStages.Count == 0) return;

            // Daha önce tamamlandıysa atla.
            if (IsCompleted(levelData)) return;

            _levelData  = levelData;
            _panel      = UIManager.instance != null ? UIManager.instance.tutorialPanel : null;
            _gameCamera = LevelCameraUtility.ResolveGameplayCamera();
            if (_panel == null) return;

            IsActive = true;
            _panel.Active(true);

            // Instruction text'inin world pozisyonunu screen-space'e çevir.
            if (_panel.instruction != null && _gameCamera != null)
            {
                Vector3 screen = _gameCamera.WorldToScreenPoint(levelData.tutorialTextWorldPosition);
                _panel.instruction.rectTransform.position = screen;
            }

            StartHandAnimation();
            ShowStage(0);
        }

        private void StartHandAnimation()
        {
            if (_panel == null) return;

            if (_handTween == null && _panel.handVisual != null)
            {
                _panel.handVisual.localScale = Vector3.one * handVisualStartScale;
                _handTween = _panel.handVisual
                    .DOScale(handVisualScale, pulseDuration)
                    .SetEase(Ease.InOutCubic)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (_circleTween == null && _panel.circleVisual != null)
            {
                // Ters faz: hand "start"tayken circle "target"ta, hand büyürken circle küçülür.
                _panel.circleVisual.localScale = Vector3.one * circleVisualScale;
                _circleTween = _panel.circleVisual
                    .DOScale(circleVisualStartScale, pulseDuration)
                    .SetEase(Ease.InOutCubic)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void StopHandAnimation()
        {
            if (_handTween != null) { _handTween.Kill(); _handTween = null; }
            if (_circleTween != null) { _circleTween.Kill(); _circleTween = null; }

            if (_panel != null)
            {
                if (_panel.handVisual != null)
                    _panel.handVisual.localScale   = Vector3.one * handVisualStartScale;
                if (_panel.circleVisual != null)
                    _panel.circleVisual.localScale = Vector3.one * circleVisualStartScale;
            }
        }

        private void ShowStage(int index)
        {
            if (_levelData == null || _panel == null) return;

            if (index < 0 || index >= _levelData.tutorialStages.Count)
            {
                // Tüm stage'ler bitti → tamamlandı olarak işaretle ve kapat.
                if (_levelData != null)
                {
                    PlayerPrefs.SetInt(PrefKeyPrefix + _levelData.name, 1);
                    PlayerPrefs.Save();
                }
                EndTutorial();
                return;
            }

            _stageIndex = index;
            TutorialStage stage = _levelData.tutorialStages[index];

            // Instruction text
            if (_panel.instruction != null)
                _panel.instruction.text = stage.instruction;

            // Hand world → screen pozisyonu
            if (_panel.hand != null && _gameCamera != null)
            {
                Vector3 screen = _gameCamera.WorldToScreenPoint(stage.targetPos);
                _panel.hand.position = screen;
            }

            // Hand rotation (handVisual üzerinde)
            if (_panel.handVisual != null)
                _panel.handVisual.localEulerAngles = stage.handRotation;
        }

        private void EndTutorial()
        {
            StopHandAnimation();
            if (_panel != null) _panel.ActiveSmooth(false);
            Cleanup();
        }

        private void Cleanup()
        {
            StopHandAnimation();
            IsActive    = false;
            _stageIndex = -1;
            _levelData  = null;
        }

        // ── Click gate / advance ─────────────────────────────────────────────────────

        /// <summary>Tutorial aktifken cell tıklanabilir mi.</summary>
        public bool IsCellClickable(int row, int col)
        {
            if (!IsActive) return true;
            var stage = CurrentStage;
            if (stage == null) return true;
            if (stage.clickableCells == null || stage.clickableCells.Count == 0) return true;

            foreach (var c in stage.clickableCells)
                if (c.x == row && c.y == col) return true;

            return false;
        }

        /// <summary>Bir cell başarıyla tıklanıp gönderildiğinde çağrılır.</summary>
        public void NotifyCellSent(int row, int col)
        {
            if (!IsActive) return;
            if (!IsCellClickable(row, col)) return;

            ShowStage(_stageIndex + 1);
        }
    }
}
