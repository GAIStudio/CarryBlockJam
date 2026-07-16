using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Tutorial: whole hand (finger + click point) slides start → target.
    /// White click point stays under the fingertip and only shows on the start cell.
    /// </summary>
    [DisallowMultipleComponent]
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        public bool IsActive { get; private set; }

        /// <summary>While true, swipes are clamped to the current stage start→target path.</summary>
        public bool IsStagePathLocked => IsActive && _stagePathLocked;

        /// <summary>
        /// Guided start→target clamping. Off for stages that complete on hidden reveal
        /// so the player can freely collect surrounding plates (swipe-until-plate).
        /// </summary>
        public bool UsesGuidedPathLock
        {
            get
            {
                if (!IsActive)
                    return false;

                TutorialStage stage = CurrentStage;
                if (stage == null)
                    return false;

                return !stage.completeOnHiddenReveal;
            }
        }

        public void ReleaseStagePathLock() => _stagePathLocked = false;

        [Header("Hand Animation")]
        public float handVisualStartScale = 1f;
        public float handVisualScale = 0.8f;
        public float circleVisualStartScale = 1f;
        public float circleVisualScale = 0.5f;
        public float pulseDuration = 0.5f;

        [Tooltip("Local offset of the white click point under the fingertip (relative to hand root).")]
        public Vector2 clickPointFingerOffset = new Vector2(-32f, 48f);

        private LevelData _levelData;
        private TutorialPanel _panel;
        private int _stageIndex = -1;
        private bool _stagePathLocked;
        private Camera _gameCamera;
        private PuzzleGrid _grid;

        private Tween _circleTween;
        private Tween _handMoveTween;
        private Coroutine _startRoutine;
        private float _slideProgress;
        private Vector2 _slideStartLocal;
        private Vector2 _slideEndLocal;
        private RectTransform _handParent;
        private Vector2 _authoredClickOffset;
        private bool _hasAuthoredClickOffset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            IsActive = false;
            _stageIndex = -1;
            _levelData = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Cleanup();
        }

        private TutorialStage CurrentStage =>
            (_levelData != null && _stageIndex >= 0 && _stageIndex < _levelData.tutorialStages.Count)
                ? _levelData.tutorialStages[_stageIndex]
                : null;

        private const string PrefKeyPrefix = "Tutorial_";

        public static bool IsCompleted(LevelData levelData) =>
            levelData != null && PlayerPrefs.GetInt(PrefKeyPrefix + levelData.name, 0) >= 1;

        public static void ResetCompletion(LevelData levelData)
        {
            if (levelData == null)
                return;

            PlayerPrefs.DeleteKey(PrefKeyPrefix + levelData.name);
            PlayerPrefs.Save();
        }

        public void StartTutorial(LevelData levelData)
        {
            Cleanup();

            if (levelData == null || !levelData.hasTutorial)
                return;

            if (levelData.tutorialStages == null || levelData.tutorialStages.Count == 0)
            {
                Debug.LogWarning(
                    $"[TutorialManager] '{levelData.name}' hasTutorial is on but has no stages.");
                return;
            }

            if (IsCompleted(levelData))
                return;

            _levelData = levelData;
            if (_startRoutine != null)
                StopCoroutine(_startRoutine);
            _startRoutine = StartCoroutine(StartTutorialRoutine());
        }

        private IEnumerator StartTutorialRoutine()
        {
            const int maxFrames = 30;
            for (int i = 0; i < maxFrames; i++)
            {
                _grid = FindObjectOfType<PuzzleGrid>();
                if (_grid != null && _grid.IsBuilt)
                    break;
                yield return null;
            }

            _panel = UIManager.instance != null ? UIManager.instance.tutorialPanel : null;
            _gameCamera = LevelCameraUtility.ResolveGameplayCamera();
            if (_grid == null || !_grid.IsBuilt)
                _grid = FindObjectOfType<PuzzleGrid>();

            if (_panel == null)
            {
                Debug.LogWarning("[TutorialManager] TutorialPanel is missing on UIManager.");
                Cleanup();
                yield break;
            }

            IsActive = true;
            _panel.Active(true);

            if (_panel.hand != null)
                _panel.hand.gameObject.SetActive(true);
            if (_panel.handVisual != null)
                _panel.handVisual.gameObject.SetActive(true);
            if (_panel.circleVisual != null)
                _panel.circleVisual.gameObject.SetActive(true);

            if (_panel.instruction != null && _gameCamera != null && _levelData != null)
            {
                Vector3 screen = _gameCamera.WorldToScreenPoint(_levelData.tutorialTextWorldPosition);
                _panel.instruction.rectTransform.position = screen;
            }

            CaptureAuthoredClickOffset();
            PlaceClickPointUnderFinger();
            StartClickPulseAnimation();
            ShowStage(0, lockPath: false);
            _startRoutine = null;
        }

        private void CaptureAuthoredClickOffset()
        {
            if (_hasAuthoredClickOffset || _panel?.circleVisual == null)
                return;

            // Scene authored fingertip placement (sibling under hand).
            _authoredClickOffset = _panel.circleVisual.anchoredPosition;
            _hasAuthoredClickOffset = _authoredClickOffset.sqrMagnitude > 1f;
        }

        private Vector2 ResolveClickOffset()
        {
            // Prefer scene layout if it was authored near the fingertip.
            if (_hasAuthoredClickOffset)
                return _authoredClickOffset;
            return clickPointFingerOffset;
        }

        private void PlaceClickPointUnderFinger()
        {
            if (_panel?.hand == null || _panel.circleVisual == null)
                return;

            // Keep circle as sibling of the finger sprite under the same hand root,
            // so both always move together when the hand root slides.
            if (_panel.circleVisual.parent != _panel.hand)
                _panel.circleVisual.SetParent(_panel.hand, worldPositionStays: false);

            _panel.circleVisual.anchoredPosition = ResolveClickOffset();
            _panel.circleVisual.localRotation = Quaternion.identity;

            // Draw under the finger sprite.
            if (_panel.handVisual != null)
            {
                _panel.handVisual.anchoredPosition = Vector2.zero;
                _panel.circleVisual.SetSiblingIndex(_panel.handVisual.GetSiblingIndex());
            }
            else
            {
                _panel.circleVisual.SetAsFirstSibling();
            }
        }

        private void StartClickPulseAnimation()
        {
            if (_panel?.circleVisual == null)
                return;

            if (_circleTween != null)
            {
                _circleTween.Kill();
                _circleTween = null;
            }

            _panel.circleVisual.anchoredPosition = ResolveClickOffset();
            _panel.circleVisual.localScale = Vector3.one * circleVisualStartScale;
            _circleTween = _panel.circleVisual
                .DOScale(circleVisualScale, pulseDuration)
                .SetEase(Ease.InOutCubic)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);

            if (_panel.handVisual != null)
                _panel.handVisual.localScale = Vector3.one * handVisualStartScale;
        }

        private void StopClickPulseAnimation()
        {
            if (_circleTween != null)
            {
                _circleTween.Kill();
                _circleTween = null;
            }

            if (_panel?.circleVisual != null)
                _panel.circleVisual.localScale = Vector3.one * circleVisualStartScale;
            if (_panel?.handVisual != null)
                _panel.handVisual.localScale = Vector3.one * handVisualStartScale;
        }

        private void StopHandMoveAnimation()
        {
            if (_handMoveTween != null)
            {
                _handMoveTween.Kill();
                _handMoveTween = null;
            }

            if (_panel?.handVisual != null)
                _panel.handVisual.anchoredPosition = Vector2.zero;

            if (_panel?.circleVisual != null)
            {
                _panel.circleVisual.anchoredPosition = ResolveClickOffset();
                _panel.circleVisual.gameObject.SetActive(true);
            }
        }

        private void ShowStage(int index, bool lockPath)
        {
            if (_levelData == null || _panel == null)
                return;

            if (index < 0 || index >= _levelData.tutorialStages.Count)
            {
                PlayerPrefs.SetInt(PrefKeyPrefix + _levelData.name, 1);
                PlayerPrefs.Save();
                EndTutorial();
                return;
            }

            _stageIndex = index;
            _stagePathLocked = lockPath;
            TutorialStage stage = _levelData.tutorialStages[index];

            if (_panel.instruction != null)
                _panel.instruction.text = stage.instruction;

            if (_panel.handVisual != null)
                _panel.handVisual.localEulerAngles = stage.handRotation;

            // Only place stickman for the first stage. Later stages only update the hand UI —
            // stickman stays where the player left them after the previous action.
            if (index == 0)
                SnapStickmanToStageStart(stage);

            StartHandPathLoop(stage);
        }

        private void SnapStickmanToStageStart(TutorialStage stage)
        {
            if (stage == null)
                return;

            CarryBlockJam.CarryBlockJamSwipeController swipe =
                FindObjectOfType<CarryBlockJam.CarryBlockJamSwipeController>();
            if (swipe != null)
                swipe.PlaceStickmanAtCell(stage.startCell.x, stage.startCell.y, force: true);
        }

        private void StartHandPathLoop(TutorialStage stage)
        {
            StopHandMoveAnimation();
            if (_panel?.hand == null || _gameCamera == null || stage == null)
                return;

            if (_grid == null || !_grid.IsBuilt)
                _grid = FindObjectOfType<PuzzleGrid>();

            ResolveStageWorldPoints(stage, out Vector3 startWorld, out Vector3 endWorld);

            Vector3 startScreen = _gameCamera.WorldToScreenPoint(startWorld);
            Vector3 endScreen = _gameCamera.WorldToScreenPoint(endWorld);

            _handParent = _panel.hand.parent as RectTransform;
            Camera uiCamera = ResolveUiCamera(_panel.hand);

            if (_handParent == null)
            {
                Debug.LogWarning("[TutorialManager] hand parent RectTransform missing.");
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _handParent, startScreen, uiCamera, out Vector2 startLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _handParent, endScreen, uiCamera, out Vector2 endLocal);

            // Finger sprite stays at local zero; click point stays at fingertip offset.
            // Shift the hand root so the click point (not the hand pivot) sits on the cell.
            Vector2 tipOffset = ResolveClickOffset();
            _slideStartLocal = startLocal - tipOffset;
            _slideEndLocal = endLocal - tipOffset;

            if (_panel.handVisual != null)
                _panel.handVisual.anchoredPosition = Vector2.zero;

            PlaceClickPointUnderFinger();

            _slideProgress = 0f;
            ApplyHandSlide(0f);

            float moveDuration = Mathf.Max(0.05f, stage.handMoveDuration);
            float pause = Mathf.Max(0f, stage.handPauseAtEnds);

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(
                DOTween.To(() => _slideProgress, ApplyHandSlide, 1f, moveDuration)
                    .SetEase(Ease.InOutSine));

            if (pause > 0f)
                sequence.AppendInterval(pause);

            sequence.Append(
                DOTween.To(() => _slideProgress, ApplyHandSlide, 0f, moveDuration)
                    .SetEase(Ease.InOutSine));

            if (pause > 0f)
                sequence.AppendInterval(pause);

            sequence.SetLoops(-1, LoopType.Restart);
            _handMoveTween = sequence;
        }

        private void ApplyHandSlide(float t)
        {
            _slideProgress = t;
            if (_panel?.hand == null)
                return;

            _panel.hand.anchoredPosition = Vector2.LerpUnclamped(_slideStartLocal, _slideEndLocal, t);

            if (_panel.circleVisual == null)
                return;

            // Stay glued under fingertip; only visible on the start cell.
            _panel.circleVisual.anchoredPosition = ResolveClickOffset();
            bool onStartCell = t <= 0.001f;
            if (_panel.circleVisual.gameObject.activeSelf != onStartCell)
                _panel.circleVisual.gameObject.SetActive(onStartCell);
        }

        private static Camera ResolveUiCamera(RectTransform rect)
        {
            if (rect == null)
                return null;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private void ResolveStageWorldPoints(TutorialStage stage, out Vector3 startWorld, out Vector3 endWorld)
        {
            startWorld = stage.targetPos;
            endWorld = stage.targetPos;

            if (stage.useGridHandPath && _grid != null && _grid.IsBuilt)
            {
                int startRow = Mathf.Clamp(stage.startCell.x, 0, Mathf.Max(0, _grid.Rows - 1));
                int startCol = Mathf.Clamp(stage.startCell.y, 0, Mathf.Max(0, _grid.Columns - 1));
                int endRow = Mathf.Clamp(stage.targetCell.x, 0, Mathf.Max(0, _grid.Rows - 1));
                int endCol = Mathf.Clamp(stage.targetCell.y, 0, Mathf.Max(0, _grid.Columns - 1));

                startWorld = _grid.GetWorldPosition(startRow, startCol) + stage.startPositionOffset;
                endWorld = _grid.GetWorldPosition(endRow, endCol) + stage.targetPositionOffset;

                if (Mathf.Abs(stage.startPositionOffset.y) < 0.001f)
                    startWorld += Vector3.up * 0.35f;
                if (Mathf.Abs(stage.targetPositionOffset.y) < 0.001f)
                    endWorld += Vector3.up * 0.35f;
                return;
            }

            endWorld = stage.targetPos + stage.targetPositionOffset;
            startWorld = endWorld + stage.startPositionOffset;
            if (stage.startPositionOffset.sqrMagnitude < 0.0001f)
                startWorld = endWorld + new Vector3(-0.8f, 0f, 0f);
        }

        private void EndTutorial()
        {
            ReleaseStagePathLock();
            StopHandMoveAnimation();
            StopClickPulseAnimation();
            if (_panel != null)
                _panel.ActiveSmooth(false);
            Cleanup();
        }

        private void Cleanup()
        {
            if (_startRoutine != null)
            {
                StopCoroutine(_startRoutine);
                _startRoutine = null;
            }

            StopHandMoveAnimation();
            StopClickPulseAnimation();
            IsActive = false;
            _stagePathLocked = false;
            _stageIndex = -1;
            _levelData = null;
            _grid = null;
            _handParent = null;
        }

        public bool IsCellClickable(int row, int col)
        {
            if (!IsActive || !_stagePathLocked)
                return true;

            TutorialStage stage = CurrentStage;
            if (stage == null)
                return true;

            // Prefer explicit whitelist when authored; otherwise only the target plate cell.
            if (stage.clickableCells != null && stage.clickableCells.Count > 0)
            {
                for (int i = 0; i < stage.clickableCells.Count; i++)
                {
                    Vector2Int cell = stage.clickableCells[i];
                    if (cell.x == row && cell.y == col)
                        return true;
                }

                // Also allow the authored target cell so plate moves stay consistent.
                if (stage.targetCell.x == row && stage.targetCell.y == col)
                    return true;

                return false;
            }

            return IsTutorialTargetCell(row, col);
        }

        /// <summary>True while a tutorial stage is teaching a grid path.</summary>
        public bool TryGetActivePath(out Vector2Int startCell, out Vector2Int targetCell)
        {
            startCell = default;
            targetCell = default;
            if (!IsActive)
                return false;

            TutorialStage stage = CurrentStage;
            if (stage == null)
                return false;

            startCell = stage.startCell;
            targetCell = stage.targetCell;
            return true;
        }

        public bool IsTutorialTargetCell(int row, int col)
        {
            if (!IsActive)
                return false;

            TutorialStage stage = CurrentStage;
            if (stage == null)
                return false;

            return stage.targetCell.x == row && stage.targetCell.y == col;
        }

        /// <summary>
        /// Path cells from from→target (target inclusive, from exclusive).
        /// </summary>
        public bool TryBuildPathCells(
            int fromRow,
            int fromCol,
            Vector2Int target,
            List<Vector2Int> pathCells)
        {
            if (pathCells == null)
                return false;
            pathCells.Clear();

            var from = new Vector2Int(fromRow, fromCol);
            if (!TryGetRequiredSwipe(from, target, out int rowStep, out int colStep, out int steps))
                return false;

            int row = fromRow;
            int col = fromCol;
            for (int i = 0; i < steps; i++)
            {
                row += rowStep;
                col += colStep;
                pathCells.Add(new Vector2Int(row, col));
            }

            return pathCells.Count > 0;
        }

        public bool TryBuildAuthoredPathCells(int fromRow, int fromCol, List<Vector2Int> pathCells)
        {
            if (!TryGetActivePath(out Vector2Int start, out Vector2Int target))
                return false;

            if (!IsOnAuthoredPathSegment(fromRow, fromCol, start, target))
                return false;

            return TryBuildPathCells(fromRow, fromCol, target, pathCells);
        }

        /// <summary>
        /// When the stage path is unlocked, engage it only if the player swipes along the
        /// taught direction while standing on the authored start→target segment.
        /// </summary>
        public bool TryEngageStagePathLock(int row, int col, int rowStep, int columnStep)
        {
            if (!UsesGuidedPathLock)
                return false;

            if (_stagePathLocked)
            {
                if (!TryGetActivePath(out Vector2Int lockedStart, out Vector2Int lockedTarget) ||
                    !IsOnAuthoredPathSegment(row, col, lockedStart, lockedTarget))
                {
                    _stagePathLocked = false;
                    return false;
                }

                return true;
            }

            if (!TryGetActivePath(out Vector2Int start, out Vector2Int target))
                return false;

            if (!IsOnAuthoredPathSegment(row, col, start, target))
                return false;

            if (!TryGetRequiredSwipe(start, target, out int requiredRowStep, out int requiredColStep, out _))
                return false;

            if (rowStep != requiredRowStep || columnStep != requiredColStep)
                return false;

            if (!IsSwipeTowardTarget(row, col, target, rowStep, columnStep))
                return false;

            _stagePathLocked = true;
            return true;
        }

        /// <summary>
        /// Locks swipe to the taught direction from the stickman's current cell to targetCell.
        /// </summary>
        public bool TryClampSwipeToAuthoredPath(
            int stickmanRow,
            int stickmanCol,
            ref int rowStep,
            ref int columnStep,
            ref int requestedSteps)
        {
            if (!UsesGuidedPathLock)
                return true;

            if (!TryGetActivePath(out Vector2Int start, out Vector2Int target))
                return true;

            if (!IsOnAuthoredPathSegment(stickmanRow, stickmanCol, start, target))
                return false;

            if (!TryGetRequiredSwipe(start, target, out int requiredRowStep, out int requiredColStep, out _))
                return false;

            if (rowStep != requiredRowStep || columnStep != requiredColStep)
                return false;

            if (!IsSwipeTowardTarget(stickmanRow, stickmanCol, target, rowStep, columnStep))
                return false;

            if (requestedSteps < 1)
                return false;

            int remainingSteps = GetRemainingStepsTowardTarget(
                stickmanRow, stickmanCol, target, rowStep, columnStep);
            if (remainingSteps < 1)
                return false;

            rowStep = requiredRowStep;
            columnStep = requiredColStep;
            requestedSteps = remainingSteps;
            return true;
        }

        public bool CanCollectTutorialPlate(int row, int col)
        {
            if (!IsActive || !_stagePathLocked)
                return true;

            TutorialStage stage = CurrentStage;
            if (stage == null)
                return true;

            return stage.targetCell.x == row && stage.targetCell.y == col;
        }

        private static bool IsOnAuthoredPathSegment(int row, int col, Vector2Int start, Vector2Int target)
        {
            if (start.x == target.x && row == start.x)
            {
                int minCol = Mathf.Min(start.y, target.y);
                int maxCol = Mathf.Max(start.y, target.y);
                return col >= minCol && col <= maxCol;
            }

            if (start.y == target.y && col == start.y)
            {
                int minRow = Mathf.Min(start.x, target.x);
                int maxRow = Mathf.Max(start.x, target.x);
                return row >= minRow && row <= maxRow;
            }

            return start.x == target.x && start.y == target.y && row == start.x && col == start.y;
        }

        private static bool IsSwipeTowardTarget(
            int row,
            int col,
            Vector2Int target,
            int rowStep,
            int columnStep)
        {
            if (rowStep != 0)
                return Mathf.Sign(target.x - row) == Mathf.Sign(rowStep);

            if (columnStep != 0)
                return Mathf.Sign(target.y - col) == Mathf.Sign(columnStep);

            return false;
        }

        private static int GetRemainingStepsTowardTarget(
            int row,
            int col,
            Vector2Int target,
            int rowStep,
            int columnStep)
        {
            if (rowStep != 0)
                return Mathf.Abs(target.x - row);

            if (columnStep != 0)
                return Mathf.Abs(target.y - col);

            return 0;
        }

        private static bool TryGetRequiredSwipe(
            Vector2Int start,
            Vector2Int target,
            out int rowStep,
            out int columnStep,
            out int requiredSteps)
        {
            rowStep = 0;
            columnStep = 0;
            requiredSteps = 0;

            int rowDelta = target.x - start.x;
            int colDelta = target.y - start.y;
            if (rowDelta == 0 && colDelta == 0)
                return false;

            if (rowDelta != 0 && colDelta != 0)
                return false;

            if (rowDelta != 0)
            {
                rowStep = rowDelta > 0 ? 1 : -1;
                requiredSteps = Mathf.Abs(rowDelta);
            }
            else
            {
                columnStep = colDelta > 0 ? 1 : -1;
                requiredSteps = Mathf.Abs(colDelta);
            }

            return requiredSteps > 0;
        }

        public void NotifyCellSent(int row, int col)
        {
            if (!IsActive)
                return;
            if (!IsCellClickable(row, col))
                return;

            ShowStage(_stageIndex + 1, lockPath: false);
        }

        /// <summary>
        /// Call after the player completes the taught start→target action (e.g. picks the target plate).
        /// Advances to the next stage, or hides the hand when finished.
        /// </summary>
        public void NotifyTutorialActionCompleted()
        {
            if (!IsActive)
                return;

            ReleaseStagePathLock();
            ShowStage(_stageIndex + 1, lockPath: false);
        }

        /// <summary>
        /// Ends the current stage when it is configured to complete on hidden-table reveal.
        /// </summary>
        public void TryCompleteStageOnHiddenReveal()
        {
            if (!IsActive)
                return;

            TutorialStage stage = CurrentStage;
            if (stage == null || !stage.completeOnHiddenReveal)
                return;

            NotifyTutorialActionCompleted();
        }
    }
}
