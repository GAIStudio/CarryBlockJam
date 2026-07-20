using GAITemplate;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarryBlockJam
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CarryBlockJamSimpleBoard))]
    [RequireComponent(typeof(PuzzleGrid))]
    public class CarryBlockJamSwipeController : MonoBehaviour
    {
        [SerializeField] private CarryBlockJamSimpleBoard board;
        [SerializeField] private float swipeThresholdPixels = 40f;
        [SerializeField] private float moveDurationPerCell = 0.09f;
        [SerializeField] private float exitTravelDuration = 0.18f;
        [SerializeField] private float exitPlateDeliveryDuration = 0.07f;
        [SerializeField] private float gatePlateFlyDuration = 0.28f;
        [SerializeField] private float gatePlateFlyHeight = 0.8f;
        [SerializeField] private float tablePlateJumpDuration = 0.3f;
        [SerializeField] private float tablePlateJumpHeight = 0.65f;
        [SerializeField] private float tablePlateSettleDuration = 0.16f;
        [SerializeField] private float tablePlateAnimationSpeed = 1.4f;
        [SerializeField] private float pickupPlateBounceDuration = 0.42f;
        [SerializeField] private float pickupPlateBounceHeight = 0.8f;
        [SerializeField] private float pickupPlateStagger = 0.07f;
        [SerializeField] private float pickupPlateSettleDuration = 0.2f;
        [SerializeField] private float pickupPlateSettleScale = 0.22f;
        [SerializeField] private float pickupPlateFrontClearance = 0.65f;
        [SerializeField] private float pickupPlateAnimationSpeed = 1.6f;
        [SerializeField] private bool showSwipeHighlights;
        [SerializeField] private float highlightHeight = 0.35f;
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.84f, 1f, 0.9f);
        [SerializeField] private Vector3 carriedPlateBaseOffset = new Vector3(0f, 0.9f, 0.40f);
        [SerializeField] private float carriedPlateStackStep = 0.18f;
        [SerializeField] private Vector3 stickmanCarryOffset = new Vector3(0f, -0.7f, 0f);
        [SerializeField] private float failurePlateDropDuration = 0.35f;
        [SerializeField] private float failurePlateFlyHeight = 1.25f;
        [SerializeField] private float failurePlateSpreadStagger = 0.04f;
        [SerializeField] private float failureUiDelay = 1.4f;

        private Camera _gameplayCamera;
        private PuzzleGrid _grid;
        private Vector2 _swipeStartScreen;
        private int _swipeStartRow;
        private int _swipeStartColumn;
        private bool _trackingSwipe;
        private bool _isAnimating;
        private bool _successTriggered;
        private bool _failTriggered;
        private bool _failurePreparing;
        private CarryBlockJamBoardPiece _cylinder;
        private CarryBlockJamStickmanAnimator _stickmanAnimator;
        private bool _stickmanMoving;
        private readonly List<CarryBlockJamBoardPiece> _carriedPlates = new List<CarryBlockJamBoardPiece>();
        private Tween _plateCollectionTween;
        private Transform _highlightRoot;
        private readonly List<Transform> _highlightPool = new List<Transform>();

        private void Awake()
        {
            if (board == null)
                board = GetComponent<CarryBlockJamSimpleBoard>();

            _grid = GetComponent<PuzzleGrid>();
            EnsureHighlightRoot();
        }

        private void Start()
        {
            ResolveGameplayReferences();
        }

        private void Update()
        {
            if (_grid == null || !_grid.IsBuilt)
                return;

            HandleMouseInput();
            HandleTouchInput();
            UpdateSwipePreview();
        }

        private void HandleMouseInput()
        {
            if (Input.touchCount > 0)
                return;

            if (Input.GetMouseButtonDown(0))
                TryStartSwipe(Input.mousePosition);

            if (_trackingSwipe && Input.GetMouseButtonUp(0))
                FinishSwipe(Input.mousePosition);
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0)
                return;

            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryStartSwipe(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_trackingSwipe)
                        FinishSwipe(touch.position);
                    break;
            }
        }

        public void PlaceStickmanAtCell(int row, int column, bool force = false)
        {
            ResolveGameplayReferences();
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return;
            if (!_grid.IsInside(row, column))
                return;
            if (_cylinder.Row == row && _cylinder.Column == column)
                return;

            if (_grid.TryGetCell(row, column, out PuzzleCell cell) &&
                cell != null &&
                cell.Occupant != null &&
                cell.Occupant != _cylinder.gameObject)
            {
                if (!force)
                {
                    Debug.LogWarning(
                        $"[CarryBlockJamSwipeController] Cannot place stickman at occupied cell ({row},{column}).");
                    return;
                }

                // Tutorial snap: steal the cell slot so input is not permanently blocked.
                cell.Occupant = null;
            }

            MovePieceToCell(_cylinder, row, column, occupiesCell: true);
            RefreshStickmanAnimation(moving: false);
        }

        private void TryStartSwipe(Vector2 screenPosition)
        {
            if (_isAnimating || _failTriggered)
                return;

            if (_cylinder == null)
                ResolveGameplayReferences();

            if (_cylinder == null)
                return;

            // Confirm the gesture is over the board, but movement always originates
            // from the stickman's cell. Raycasting the finger onto the floor near a
            // tall stickman (especially on the top rows) often hits the cell behind
            // him, which made "swipe up into row 0" look like a zero-length move.
            if (!TryGetNearestGridCell(screenPosition, out _, out _))
                return;

            // Text-only tutorial tip: hide instruction on first press.
            TutorialManager.Instance?.NotifyPlayerInteracted();

            _swipeStartScreen = screenPosition;
            _swipeStartRow = _cylinder.Row;
            _swipeStartColumn = _cylinder.Column;
            _trackingSwipe = true;
            EnsureHighlightRoot();
            ShowHighlights(false);
        }

        private void FinishSwipe(Vector2 screenPosition)
        {
            _trackingSwipe = false;
            ShowHighlights(false);

            ResolveGameplayReferences();
            if (_cylinder == null)
                return;

            if (_failTriggered)
                return;

            if (!TryGetSwipeIntent(screenPosition, out int rowStep, out int columnStep, out int requestedSteps))
                return;

            if (TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                TutorialManager.Instance.IsStagePathLocked)
            {
                if (!TutorialManager.Instance.TryEngageStagePathLock(
                        _cylinder.Row,
                        _cylinder.Column,
                        rowStep,
                        columnStep))
                    return;

                if (!TutorialManager.Instance.TryClampSwipeToAuthoredPath(
                        _cylinder.Row,
                        _cylinder.Column,
                        ref rowStep,
                        ref columnStep,
                        ref requestedSteps))
                    return;

                TryExecuteTutorialTravelSwipe(requestedSteps);
                return;
            }

            if (HasCarriedPlates)
                ExecuteCarrySwipe(rowStep, columnStep, requestedSteps);
            else
                ExecuteTravelSwipe(rowStep, columnStep, requestedSteps);
        }

        /// <summary>
        /// Tutorial move: always travel the full remaining start→target path in one swipe.
        /// Contents of cells do not block or change the path. Stage completes on arrival.
        /// </summary>
        private bool TryExecuteTutorialTravelSwipe(int requestedSteps)
        {
            if (TutorialManager.Instance == null ||
                !TutorialManager.Instance.TryGetActivePath(out _, out Vector2Int target))
                return false;

            var path = new List<Vector2Int>();
            if (!TutorialManager.Instance.TryBuildAuthoredPathCells(
                    _cylinder.Row,
                    _cylinder.Column,
                    path))
                return false;

            if (path.Count == 0)
            {
                FinishTutorialStageAtTarget(target);
                return true;
            }

            if (path.Count != requestedSteps)
                return false;

            // Collect any pickable plates on the path up front so the stickman can walk the line.
            var pickupPieces = new List<CarryBlockJamBoardPiece>();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int step = path[i];
                if (!_grid.TryGetCell(step.x, step.y, out PuzzleCell cell) || cell?.Occupant == null)
                    continue;

                CarryBlockJamBoardPiece occupant =
                    cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (!CanPickUpPiece(occupant, step.x, step.y))
                    continue;

                List<CarryBlockJamBoardPiece> extracted = ExtractPickupPlates(occupant, step.x, step.y);
                if (extracted == null)
                    continue;

                for (int p = 0; p < extracted.Count; p++)
                {
                    if (extracted[p] != null)
                        pickupPieces.Add(extracted[p]);
                }
            }

            // While carrying, stop beside a matching table at the target (normal drop).
            bool stopBeforeTable =
                HasCarriedPlates &&
                path.Count > 0 &&
                path[path.Count - 1].x == target.x &&
                path[path.Count - 1].y == target.y &&
                TryResolveDropTargetAtCell(target.x, target.y, out _);
            if (stopBeforeTable)
                path.RemoveAt(path.Count - 1);

            void OnArrived()
            {
                if (pickupPieces.Count > 0)
                    AddPlatesToCarryStack(pickupPieces, notifyTutorialComplete: false);

                FinishTutorialStageAtTarget(target);
            }

            if (path.Count == 0)
            {
                OnArrived();
                return true;
            }

            AnimateTutorialCylinderTravel(path, OnArrived);
            return true;
        }

        /// <summary>
        /// Opportunistic pickup/drop/exit at the stage target, then always advance the stage.
        /// </summary>
        private void FinishTutorialStageAtTarget(Vector2Int target)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsActive)
                return;

            if (HasCarriedPlates)
            {
                if (TryResolveExitAtCell(target.x, target.y, out CarryBlockJamExit exit) &&
                    exit != null &&
                    exit.CanAccept(CarriedColor))
                {
                    SendCarriedPlatesToExit(exit, null);
                    // Stage advances from exit-delivery completion callback.
                    return;
                }

                if (TryResolveDropTargetAtCell(target.x, target.y, out CarryBlockJamBoardPiece dropBox))
                {
                    AnimateCarriedPlatesToBox(dropBox, () =>
                        TutorialManager.Instance.NotifyTutorialActionCompleted());
                    return;
                }
            }
            else if (_grid.TryGetCell(target.x, target.y, out PuzzleCell cell) &&
                     cell?.Occupant != null)
            {
                CarryBlockJamBoardPiece occupant =
                    cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (CanPickUpPiece(occupant, target.x, target.y))
                {
                    List<CarryBlockJamBoardPiece> leftover =
                        ExtractPickupPlates(occupant, target.x, target.y);
                    if (leftover != null && leftover.Count > 0)
                        AddPlatesToCarryStack(leftover, notifyTutorialComplete: false);
                }
            }

            TutorialManager.Instance.NotifyTutorialActionCompleted();
        }

        private void CompleteTutorialTargetArrival(int targetRow, int targetColumn)
        {
            FinishTutorialStageAtTarget(new Vector2Int(targetRow, targetColumn));
        }

        private bool TryResolveDropTargetAtCell(int row, int column, out CarryBlockJamBoardPiece targetBox)
        {
            targetBox = null;
            if (!HasCarriedPlates)
                return false;

            if (_grid.TryGetCell(row, column, out PuzzleCell cell) &&
                cell?.Occupant != null)
            {
                CarryBlockJamBoardPiece occupant =
                    cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (IsMatchingDropTarget(occupant))
                {
                    targetBox = occupant.Kind == CarryBlockJamPieceKind.Box
                        ? occupant
                        : GetStorageBox(occupant) ?? occupant;
                    return true;
                }
            }

            // Tutorial travel can place the stickman on the table cell, hiding the table occupant.
            CarryBlockJamBoardPiece[] pieces = GetComponentsInChildren<CarryBlockJamBoardPiece>(true);
            for (int i = 0; i < pieces.Length; i++)
            {
                CarryBlockJamBoardPiece piece = pieces[i];
                if (piece == null || piece.Row != row || piece.Column != column)
                    continue;
                if (piece.Kind == CarryBlockJamPieceKind.Box && piece.Color == CarriedColor)
                {
                    targetBox = piece;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// When delivering to a table, stop on the adjacent cell like normal carry swipes.
        /// </summary>
        private bool TrimTutorialPathBeforeTableDrop(List<Vector2Int> path, Vector2Int target)
        {
            if (!HasCarriedPlates || path == null || path.Count == 0)
                return false;

            if (TryResolveExitAtCell(target.x, target.y, out _))
                return false;

            if (!TryResolveDropTargetAtCell(target.x, target.y, out _))
                return false;

            Vector2Int lastStep = path[path.Count - 1];
            if (lastStep.x != target.x || lastStep.y != target.y)
                return false;

            path.RemoveAt(path.Count - 1);
            return true;
        }

        /// <summary>
        /// Same movement as <see cref="AnimateCylinderTravel"/> but never truncates before
        /// the last cell (needed so tutorial reaches targetCell when a plate sits on a table).
        /// </summary>
        private void AnimateTutorialCylinderTravel(List<Vector2Int> path, TweenCallback onComplete = null)
        {
            if (_cylinder == null || path == null || path.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            RefreshStickmanAnimation(moving: true);
            Haptic.HeavyTaptic();

            ClearStickmanOccupantFromCell(_cylinder.Row, _cylinder.Column);

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < path.Count; i++)
            {
                int targetRow = path[i].x;
                int targetColumn = path[i].y;
                Vector3 targetPosition = GetPieceLocalPosition(_cylinder, targetRow, targetColumn);

                sequence.AppendCallback(() => FaceStickmanToward(targetPosition));
                sequence.Append(_cylinder.transform.DOLocalMove(
                    targetPosition,
                    moveDurationPerCell).SetEase(Ease.Linear));

                int placeRow = targetRow;
                int placeColumn = targetColumn;
                sequence.AppendCallback(() => PlaceStickmanOnCell(placeRow, placeColumn));
            }

            sequence.OnComplete(() =>
            {
                _isAnimating = false;
                RefreshStickmanAnimation(moving: false);
                onComplete?.Invoke();
            });
        }

        private void PlaceStickmanOnCell(int row, int column)
        {
            if (_cylinder == null || _grid == null)
                return;

            ClearStickmanOccupantFromCell(_cylinder.Row, _cylinder.Column);
            _cylinder.PlaceOnGrid(_grid, GetPiecesRoot(), row, column);
            if (_grid.TryGetCell(row, column, out PuzzleCell cell) && cell != null)
                cell.Occupant = _cylinder.gameObject;
        }

        private void ClearStickmanOccupantFromCell(int row, int column)
        {
            if (_cylinder == null || _grid == null || !_grid.IsInside(row, column))
                return;

            if (_grid.TryGetCell(row, column, out PuzzleCell cell) &&
                cell != null &&
                cell.Occupant == _cylinder.gameObject)
                cell.Occupant = null;
        }

        /// <summary>
        /// Tutorial travel used to leave the stickman marked as occupant on every walked cell.
        /// Clear those ghosts so free movement is not invisibly blocked.
        /// </summary>
        public void ClearStaleStickmanOccupants()
        {
            ResolveGameplayReferences();
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return;

            for (int row = 0; row < _grid.Rows; row++)
            {
                for (int column = 0; column < _grid.Columns; column++)
                {
                    if (row == _cylinder.Row && column == _cylinder.Column)
                        continue;

                    ClearStickmanOccupantFromCell(row, column);
                }
            }

            if (_grid.TryGetCell(_cylinder.Row, _cylinder.Column, out PuzzleCell current) && current != null)
                current.Occupant = _cylinder.gameObject;
        }

        private void ExecuteTravelSwipe(int rowStep, int columnStep, int requestedSteps)
        {
            int row = _cylinder.Row;
            int column = _cylinder.Column;
            var path = new List<Vector2Int>();
            List<CarryBlockJamBoardPiece> pickupPieces = null;
            int pickupRow = -1;
            int pickupColumn = -1;

            for (int stepIndex = 0; stepIndex < requestedSteps; stepIndex++)
            {
                int nextRow = row + rowStep;
                int nextColumn = column + columnStep;
                if (!_grid.IsInside(nextRow, nextColumn) ||
                    !_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell nextCell) ||
                    nextCell == null)
                    break;

                CarryBlockJamBoardPiece nextPiece = nextCell.Occupant != null
                    ? nextCell.Occupant.GetComponent<CarryBlockJamBoardPiece>()
                    : null;
                if (nextPiece == _cylinder)
                    nextPiece = null;

                if (nextPiece == null)
                {
                    row = nextRow;
                    column = nextColumn;
                    path.Add(new Vector2Int(nextRow, nextColumn));
                    continue;
                }

                if (IsBoxCellBlocker(nextPiece))
                {
                    if (CanPickUpPiece(nextPiece, nextRow, nextColumn))
                    {
                        bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                        pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
                        pickupRow = nextRow;
                        pickupColumn = nextColumn;
                        if (!pickupFromPreviousCell)
                            path.Add(new Vector2Int(nextRow, nextColumn));

                        MoveConnectedCylinder(path, () =>
                        {
                            AddPlatesToCarryStack(pickupPieces, pickupRow, pickupColumn);
                        });
                        return;
                    }

                    break;
                }

                if (CanPickUpPiece(nextPiece, nextRow, nextColumn))
                {
                    bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                    pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
                    pickupRow = nextRow;
                    pickupColumn = nextColumn;
                    if (!pickupFromPreviousCell)
                        path.Add(new Vector2Int(nextRow, nextColumn));

                    MoveConnectedCylinder(path, () =>
                    {
                        AddPlatesToCarryStack(pickupPieces, pickupRow, pickupColumn);
                    });
                    return;
                }

                break;
            }

            if (path.Count > 0)
                AnimateCylinderTravel(path);
        }

        private void ExecuteCarrySwipe(int rowStep, int columnStep, int requestedSteps)
        {
            int currentRow = _cylinder.Row;
            int currentColumn = _cylinder.Column;
            var cylinderPath = new List<Vector2Int>();
            List<CarryBlockJamBoardPiece> pickupPieces = null;
            CarryBlockJamBoardPiece targetBox = null;
            int pickupRow = -1;
            int pickupColumn = -1;

            for (int stepIndex = 0; stepIndex < requestedSteps; stepIndex++)
            {
                int nextRow = currentRow + rowStep;
                int nextColumn = currentColumn + columnStep;
                if (!_grid.IsInside(nextRow, nextColumn) ||
                    !_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell nextCell) ||
                    nextCell == null)
                    break;

                CarryBlockJamBoardPiece nextPiece = nextCell.Occupant != null
                    ? nextCell.Occupant.GetComponent<CarryBlockJamBoardPiece>()
                    : null;
                if (nextPiece == _cylinder)
                    nextPiece = null;

                if (nextPiece == null)
                {
                    currentRow = nextRow;
                    currentColumn = nextColumn;
                    cylinderPath.Add(new Vector2Int(nextRow, nextColumn));
                    continue;
                }

                if (IsBoxCellBlocker(nextPiece))
                {
                    CarryBlockJamBoardPiece blockedStorageBox = GetStorageBox(nextPiece);
                    if (blockedStorageBox != null && blockedStorageBox.Color == CarriedColor)
                    {
                        targetBox = blockedStorageBox;
                    }
                    else if (CanPickUpPiece(nextPiece, nextRow, nextColumn) && nextPiece.Color == CarriedColor)
                    {
                        bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                        pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
                        pickupRow = nextRow;
                        pickupColumn = nextColumn;
                        if (!pickupFromPreviousCell)
                            cylinderPath.Add(new Vector2Int(nextRow, nextColumn));
                    }

                    break;
                }

                CarryBlockJamBoardPiece storageBox = GetStorageBox(nextPiece);
                if (storageBox != null && storageBox.Color == CarriedColor)
                {
                    targetBox = storageBox;
                }
                else if (CanPickUpPiece(nextPiece, nextRow, nextColumn) && nextPiece.Color == CarriedColor)
                {
                    bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                    pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
                    pickupRow = nextRow;
                    pickupColumn = nextColumn;
                    if (!pickupFromPreviousCell)
                        cylinderPath.Add(new Vector2Int(nextRow, nextColumn));
                }
                else if (IsMatchingDropTarget(nextPiece))
                {
                    targetBox = nextPiece;
                }

                break;
            }

            if (!HasCarriedPlates)
                return;

            if (pickupPieces != null && pickupPieces.Count > 0)
            {
                MoveConnectedCylinder(cylinderPath, () => AddPlatesToCarryStack(pickupPieces, pickupRow, pickupColumn));
                return;
            }

            if (targetBox != null)
            {
                MoveConnectedCylinder(cylinderPath, () => AnimateCarriedPlatesToBox(targetBox));
                return;
            }

            if (TryResolveExit(rowStep, columnStep, currentRow, currentColumn, out CarryBlockJamExit edgeExit) ||
                TryResolveExitAtCell(currentRow, currentColumn, out edgeExit))
            {
                SendCarriedPlatesToExit(edgeExit, cylinderPath);
                return;
            }

            MoveConnectedCylinder(cylinderPath);
        }

        private void SendCarriedPlatesToExit(CarryBlockJamExit exitComponent, List<Vector2Int> cylinderPath)
        {
            if (!HasCarriedPlates || exitComponent == null)
                return;

            TweenCallback onCylinderComplete = () => AnimateCarriedPlatesToExit(exitComponent);
            if (cylinderPath != null && cylinderPath.Count > 0)
                AnimateCylinderTravel(cylinderPath, onCylinderComplete);
            else
            {
                _isAnimating = true;
                onCylinderComplete.Invoke();
            }
        }

        private void MoveConnectedCylinder(List<Vector2Int> cylinderPath, TweenCallback onComplete = null)
        {
            List<Vector2Int> safePath = TrimPathBeforeBoxBlocker(cylinderPath);
            if (safePath == null || safePath.Count == 0)
            {
                onComplete?.Invoke();
                EvaluateCarriedPlateDeadlock();
                return;
            }

            AnimateCylinderTravel(safePath, () =>
            {
                onComplete?.Invoke();
                EvaluateCarriedPlateDeadlock();
            });
        }

        private List<CarryBlockJamBoardPiece> ExtractPickupPlates(CarryBlockJamBoardPiece piece, int row, int column)
        {
            if (piece == null)
                return null;

            // Capture the table before stack links are cleared.
            CarryBlockJamBoardPiece storageBox = GetStorageBox(piece);
            var pickupPieces = new List<CarryBlockJamBoardPiece>();
            CarryBlockJamBoardPiece current = GetPickupBasePiece(piece);
            CarryBlockJamBoardPiece firstRemaining = null;

            while (current != null)
            {
                CarryBlockJamBoardPiece nextAbove = current.StackedAbove;
                if (current.Kind == CarryBlockJamPieceKind.Plate)
                {
                    if (current.Color == piece.Color)
                    {
                        current.ClearStackLinks();
                        pickupPieces.Add(current);
                    }
                    else if (firstRemaining == null)
                    {
                        firstRemaining = current;
                    }
                }
                else if (current.Kind == CarryBlockJamPieceKind.Box && firstRemaining == null)
                {
                    firstRemaining = current;
                }

                current = nextAbove;
            }

            if (firstRemaining == null)
                firstRemaining = storageBox;

            if (_grid.TryGetCell(row, column, out PuzzleCell cell) && cell != null)
                cell.Occupant = firstRemaining != null ? firstRemaining.gameObject : null;

            return pickupPieces;
        }

        private void MovePieceToCell(CarryBlockJamBoardPiece piece, int row, int column, bool occupiesCell)
        {
            if (piece == null || board == null || _grid == null)
                return;

            if (piece.Row >= 0 && piece.Column >= 0 && _grid.IsInside(piece.Row, piece.Column))
                _grid.ClearOccupant(piece.Row, piece.Column);

            piece.PlaceOnGrid(_grid, GetPiecesRoot(), row, column);

            if (_grid.TryGetCell(row, column, out PuzzleCell cell) && cell != null)
                cell.Occupant = occupiesCell ? piece.gameObject : null;
        }

        private Transform GetPiecesRoot()
        {
            Transform root = board.transform.Find("RuntimePieces");
            return root != null ? root : board.transform;
        }

        private void AnimateCylinderTravel(List<Vector2Int> path, TweenCallback onComplete = null)
        {
            if (_cylinder == null || path == null || path.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            RefreshStickmanAnimation(moving: true);
            Haptic.HeavyTaptic();

            if (_cylinder.Row >= 0 && _cylinder.Column >= 0 && _grid.IsInside(_cylinder.Row, _cylinder.Column))
                _grid.ClearOccupant(_cylinder.Row, _cylinder.Column);

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int step = path[i];
                int targetRow = step.x;
                int targetColumn = step.y;
                if (IsBoxOwnedCell(targetRow, targetColumn))
                    break;

                Vector3 targetPosition = GetPieceLocalPosition(_cylinder, targetRow, targetColumn);
                sequence.AppendCallback(() => FaceStickmanToward(targetPosition));
                sequence.Append(_cylinder.transform.DOLocalMove(
                    targetPosition,
                    moveDurationPerCell).SetEase(Ease.Linear));
                sequence.AppendCallback(() =>
                {
                    if (!IsBoxOwnedCell(targetRow, targetColumn))
                        PlaceStickmanOnCell(targetRow, targetColumn);
                });
            }

            Vector2Int finalStep = path[path.Count - 1];
            sequence.OnComplete(() =>
            {
                if (!IsBoxOwnedCell(finalStep.x, finalStep.y))
                    PlaceStickmanOnCell(finalStep.x, finalStep.y);

                _isAnimating = false;
                RefreshStickmanAnimation(moving: false);
                onComplete?.Invoke();
            });
        }

        private List<Vector2Int> TrimPathBeforeBoxBlocker(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0 || _grid == null)
                return path;

            var safePath = new List<Vector2Int>(path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int step = path[i];
                if (IsBoxOwnedCell(step.x, step.y))
                    break;

                if (!_grid.TryGetCell(step.x, step.y, out PuzzleCell cell) || cell == null)
                    break;

                CarryBlockJamBoardPiece occupantPiece = cell.Occupant != null
                    ? cell.Occupant.GetComponent<CarryBlockJamBoardPiece>()
                    : null;
                if (occupantPiece == _cylinder)
                    occupantPiece = null;

                if (IsBoxCellBlocker(occupantPiece))
                    break;

                safePath.Add(step);
            }

            return safePath;
        }

        private bool IsBoxOwnedCell(int row, int column)
        {
            CarryBlockJamBoardPiece[] pieces = GetComponentsInChildren<CarryBlockJamBoardPiece>(true);
            for (int i = 0; i < pieces.Length; i++)
            {
                CarryBlockJamBoardPiece piece = pieces[i];
                if (piece == null || piece.Kind != CarryBlockJamPieceKind.Box)
                    continue;

                if (piece.Row == row && piece.Column == column)
                    return true;
            }

            return false;
        }

        private void AnimateCarriedPlatesToExit(CarryBlockJamExit exitComponent)
        {
            if (!HasCarriedPlates || exitComponent == null || !exitComponent.CanAccept(CarriedColor))
            {
                _isAnimating = false;
                return;
            }

            PieceColorType deliverColor = CarriedColor;
            int consumableCount = Mathf.Min(
                _carriedPlates.Count,
                exitComponent.GetAcceptablePlateCount(deliverColor));
            if (consumableCount <= 0)
            {
                _isAnimating = false;
                return;
            }

            _isAnimating = true;
            List<CarryBlockJamBoardPiece> plates = DetachCarriedPlates(consumableCount);
            RefreshStickmanAnimation(moving: false);
            Haptic.MediumTaptic();

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < plates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = plates[i];
                if (plate == null)
                    continue;

                CarryBlockJamBoardPiece arrivingPlate = plate;
                plate.transform.SetParent(GetPiecesRoot(), true);
                Vector3 targetPosition = exitComponent.transform.position + Vector3.up * (0.05f * i);
                float flyDuration = Mathf.Max(exitPlateDeliveryDuration, gatePlateFlyDuration);
                Vector3 spinTarget = plate.transform.eulerAngles + new Vector3(0f, 360f, 0f);
                sequence.Append(plate.transform.DOJump(
                    targetPosition,
                    Mathf.Max(0.01f, gatePlateFlyHeight),
                    1,
                    flyDuration).SetEase(Ease.InOutQuad));
                sequence.Join(plate.transform.DORotate(
                    spinTarget,
                    flyDuration,
                    RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
                sequence.Join(plate.transform.DOScale(
                    plate.transform.localScale * 0.35f,
                    flyDuration).SetEase(Ease.InQuad));
                sequence.AppendCallback(() =>
                {
                    exitComponent.ConsumeOne(deliverColor);
                    CarryBlockJamCurtainBox.NotifyPlatesDeliveredToExit(deliverColor, 1);
                    CarryBlockJamHiddenBox.NotifyPlateCollected(arrivingPlate);
                    CarryBlockJamFrozenBox.NotifyPlateCollected(arrivingPlate);
                    if (arrivingPlate != null)
                    {
                        arrivingPlate.gameObject.SetActive(false);
                        Destroy(arrivingPlate.gameObject);
                    }
                });
            }

            sequence.OnComplete(() =>
            {
                UpdateCarriedPlateVisuals();
                _isAnimating = false;
                MaybeNotifyTutorialExitDelivery();
                if (HasCarriedPlates)
                    EvaluateCarriedPlateDeadlock();
                else
                    TryTriggerSuccess(plates);
            });
        }

        private void MaybeNotifyTutorialExitDelivery()
        {
            if (TutorialManager.Instance == null ||
                !TutorialManager.Instance.IsActive ||
                _cylinder == null)
                return;

            if (!TutorialManager.Instance.IsTutorialTargetCell(_cylinder.Row, _cylinder.Column))
                return;

            TutorialManager.Instance.NotifyTutorialActionCompleted();
        }

        private void AnimateCarriedPlatesToBox(CarryBlockJamBoardPiece targetBox, TweenCallback onComplete = null)
        {
            if (!HasCarriedPlates || targetBox == null)
            {
                _isAnimating = false;
                onComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            List<CarryBlockJamBoardPiece> plates = DetachCarriedPlates();
            RefreshStickmanAnimation(moving: false);
            Haptic.MediumTaptic();
            AnimateNextPlateToBox(plates, 0, targetBox, onComplete);
        }

        private void AnimateNextPlateToBox(
            List<CarryBlockJamBoardPiece> plates,
            int index,
            CarryBlockJamBoardPiece targetBox,
            TweenCallback onComplete)
        {
            while (index < plates.Count && plates[index] == null)
                index++;

            if (index >= plates.Count)
            {
                _isAnimating = false;
                if (onComplete != null)
                    onComplete.Invoke();
                else
                    MaybeNotifyTutorialTableDrop(targetBox);
                return;
            }

            CarryBlockJamBoardPiece plate = plates[index];
            CarryBlockJamBoardPiece basePiece = GetTopStackPiece(targetBox);
            if (basePiece == null)
            {
                AnimateNextPlateToBox(plates, index + 1, targetBox, onComplete);
                return;
            }

            plate.ClearStackLinks();
            plate.transform.SetParent(GetPiecesRoot(), true);
            Vector3 stackWorldTarget = basePiece.transform.TransformPoint(
                basePiece.GetStackAttachLocalPosition(plate));
            float tableAnimationSpeed = Mathf.Max(0.01f, tablePlateAnimationSpeed);
            float jumpDuration = Mathf.Max(
                0.01f,
                Mathf.Max(exitTravelDuration, tablePlateJumpDuration) / tableAnimationSpeed);
            Vector3 spinTarget = plate.transform.eulerAngles + new Vector3(0f, 270f, 0f);

            Sequence landing = DOTween.Sequence();
            landing.Append(plate.transform.DOJump(
                stackWorldTarget,
                Mathf.Max(0.01f, tablePlateJumpHeight),
                1,
                jumpDuration).SetEase(Ease.OutQuad));
            landing.Join(plate.transform.DORotate(
                spinTarget,
                jumpDuration,
                RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            landing.AppendCallback(() =>
            {
                plate.StackOnPiece(basePiece);
                if (_grid.TryGetCell(targetBox.Row, targetBox.Column, out PuzzleCell cell) && cell != null)
                    cell.Occupant = plate.gameObject;
            });

            Vector3 settlePunch = plate.transform.localScale * 0.12f;
            landing.Append(plate.transform.DOPunchScale(
                settlePunch,
                Mathf.Max(0.01f, tablePlateSettleDuration / tableAnimationSpeed),
                6,
                0.5f));

            int nextIndex = index + 1;
            landing.OnComplete(() =>
                AnimateNextPlateToBox(plates, nextIndex, targetBox, onComplete));
        }

        private void MaybeNotifyTutorialTableDrop(CarryBlockJamBoardPiece targetBox)
        {
            if (TutorialManager.Instance == null ||
                !TutorialManager.Instance.IsActive ||
                targetBox == null)
                return;

            if (!TutorialManager.Instance.IsTutorialTargetCell(targetBox.Row, targetBox.Column))
                return;

            TutorialManager.Instance.NotifyTutorialActionCompleted();
        }

        private Vector3 GetPieceLocalPosition(CarryBlockJamBoardPiece piece, int row, int column)
        {
            // Stickman root stays on a single grid height. Walk/carry pose height
            // is compensated on the Visual child, not by hopping the root.
            return _grid.GetLocalPosition(row, column) + piece.GridOffset;
        }

        private void ResolveGameplayReferences()
        {
            if (_gameplayCamera == null)
                _gameplayCamera = Camera.main;

            if (_gameplayCamera == null)
                _gameplayCamera = FindObjectOfType<Camera>();

            CarryBlockJamBoardPiece[] pieces = GetComponentsInChildren<CarryBlockJamBoardPiece>(true);
            for (int i = 0; i < pieces.Length; i++)
            {
                CarryBlockJamBoardPiece piece = pieces[i];
                if (piece == null)
                    continue;

                if (piece.Kind == CarryBlockJamPieceKind.Cylinder)
                    _cylinder = piece;
            }

            EnsureStickmanAnimator();
        }

        private void EnsureStickmanAnimator()
        {
            if (_cylinder == null)
            {
                _stickmanAnimator = null;
                return;
            }

            if (_stickmanAnimator != null && _stickmanAnimator.transform.IsChildOf(_cylinder.transform))
            {
                SyncStickmanWalkHeightOffset();
                return;
            }

            RuntimeAnimatorController controller = null;
            CarryBlockJamRuntimePieceSpawner spawner = GetComponent<CarryBlockJamRuntimePieceSpawner>();
            if (spawner != null)
                controller = spawner.StickmanAnimatorController;

            _stickmanAnimator = CarryBlockJamStickmanAnimator.EnsureOnCylinder(_cylinder.transform, controller);
            SyncStickmanWalkHeightOffset();
            RefreshStickmanAnimation(moving: false);
        }

        private void RefreshStickmanAnimation(bool moving)
        {
            if (_stickmanAnimator == null)
                EnsureStickmanAnimator();

            _stickmanMoving = moving;
            SyncStickmanWalkHeightOffset();
            _stickmanAnimator?.SetState(moving, HasCarriedPlates);
        }

        private void SyncStickmanWalkHeightOffset()
        {
            if (_stickmanAnimator == null)
                return;

            _stickmanAnimator.SetAnimatedHeightOffset(ResolveStickmanWalkOffset());
        }

        private Vector3 ResolveStickmanWalkOffset()
        {
            if (board != null && board.PrefabSettings != null)
                return board.PrefabSettings.stickmanCarryOffset;

            return stickmanCarryOffset;
        }

        private Transform GetStickmanVisual()
        {
            if (_cylinder == null)
                return null;

            return _cylinder.transform.Find("Visual");
        }

        private void FaceStickmanToward(Vector3 localTarget)
        {
            if (_cylinder == null)
                return;

            Vector3 flatDelta = localTarget - _cylinder.transform.localPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < 0.0001f)
                return;

            Transform visual = GetStickmanVisual();
            Transform faceRoot = visual != null ? visual : _cylinder.transform;
            // LateUpdate on the animator driver owns Visual localPosition (height blend).
            faceRoot.localRotation = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
        }

        private void UpdateSwipePreview()
        {
            if (!showSwipeHighlights)
            {
                ShowHighlights(false);
                return;
            }

            if (_cylinder == null)
                ResolveGameplayReferences();

            if (!_trackingSwipe || _isAnimating || _cylinder == null)
            {
                ShowHighlights(false);
                return;
            }

            Vector2 currentScreenPosition = Input.touchCount > 0
                ? Input.GetTouch(0).position
                : (Vector2)Input.mousePosition;

            if (!TryGetSwipeIntent(currentScreenPosition, out int rowStep, out int columnStep, out int requestedSteps))
            {
                ShowHighlights(false);
                return;
            }

            if (TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                TutorialManager.Instance.IsStagePathLocked)
            {
                if (!TutorialManager.Instance.TryClampSwipeToAuthoredPath(
                        _cylinder.Row,
                        _cylinder.Column,
                        ref rowStep,
                        ref columnStep,
                        ref requestedSteps))
                {
                    ShowHighlights(false);
                    return;
                }

                var previewPath = new List<Vector2Int>();
                if (!TutorialManager.Instance.TryBuildAuthoredPathCells(
                        _cylinder.Row,
                        _cylinder.Column,
                        previewPath))
                {
                    ShowHighlights(false);
                    return;
                }

                if (TutorialManager.Instance.TryGetActivePath(out _, out Vector2Int previewTarget))
                    TrimTutorialPathBeforeTableDrop(previewPath, previewTarget);

                DrawHighlights(previewPath);
                return;
            }

            List<Vector2Int> freePreviewPath = BuildPreviewPath(rowStep, columnStep, requestedSteps);
            DrawHighlights(freePreviewPath);
        }

        private List<Vector2Int> BuildPreviewPath(int rowStep, int columnStep, int requestedSteps)
        {
            var path = new List<Vector2Int>();
            int row = _cylinder.Row;
            int column = _cylinder.Column;

            for (int stepIndex = 0; stepIndex < requestedSteps; stepIndex++)
            {
                int nextRow = row + rowStep;
                int nextColumn = column + columnStep;
                if (!_grid.IsInside(nextRow, nextColumn) ||
                    !_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell cell) ||
                    cell == null)
                    break;

                CarryBlockJamBoardPiece piece = cell.Occupant != null
                    ? cell.Occupant.GetComponent<CarryBlockJamBoardPiece>()
                    : null;
                if (piece == _cylinder)
                    piece = null;

                if (!HasCarriedPlates)
                {
                    if (piece == null)
                    {
                        row = nextRow;
                        column = nextColumn;
                        path.Add(new Vector2Int(nextRow, nextColumn));
                        continue;
                    }

                    if (IsBoxCellBlocker(piece))
                    {
                        // Stickman stops on the previous cell when picking up from a table,
                        // but still highlight the table so the swipe target is clear.
                        if (CanPickUpPiece(piece, nextRow, nextColumn))
                            path.Add(new Vector2Int(nextRow, nextColumn));
                        break;
                    }

                    if (CanPickUpPiece(piece, nextRow, nextColumn))
                    {
                        path.Add(new Vector2Int(nextRow, nextColumn));
                        break;
                    }
                }
                else
                {
                    if (piece != null)
                    {
                        if (IsBoxCellBlocker(piece))
                        {
                            CarryBlockJamBoardPiece blockedStorageBox = GetStorageBox(piece);
                            if (blockedStorageBox != null && blockedStorageBox.Color == CarriedColor)
                            {
                                path.Add(new Vector2Int(nextRow, nextColumn));
                                break;
                            }

                            if (CanPickUpPiece(piece, nextRow, nextColumn) && piece.Color == CarriedColor)
                                path.Add(new Vector2Int(nextRow, nextColumn));
                            break;
                        }

                        CarryBlockJamBoardPiece storageBox = GetStorageBox(piece);
                        if (storageBox != null && storageBox.Color == CarriedColor)
                        {
                            path.Add(new Vector2Int(nextRow, nextColumn));
                            break;
                        }

                        if (CanPickUpPiece(piece, nextRow, nextColumn) && piece.Color == CarriedColor)
                            path.Add(new Vector2Int(nextRow, nextColumn));
                        else if (IsMatchingDropTarget(piece))
                            path.Add(new Vector2Int(nextRow, nextColumn));
                        break;
                    }

                    row = nextRow;
                    column = nextColumn;
                    path.Add(new Vector2Int(nextRow, nextColumn));
                    continue;
                }

                break;
            }

            return path;
        }

        private bool TryGetSwipeIntent(Vector2 screenPosition, out int rowStep, out int columnStep, out int requestedSteps)
        {
            rowStep = 0;
            columnStep = 0;
            requestedSteps = 0;

            Vector2 screenDelta = screenPosition - _swipeStartScreen;
            if (screenDelta.magnitude < swipeThresholdPixels)
                return false;

            if (_gameplayCamera == null)
                ResolveGameplayReferences();

            if (_gameplayCamera == null || _grid == null)
                return false;

            GetGridAxisScreenVectors(
                _swipeStartRow,
                _swipeStartColumn,
                out Vector2 rowAxisScreen,
                out Vector2 columnAxisScreen);

            float rowAxisSqr = rowAxisScreen.sqrMagnitude;
            float columnAxisSqr = columnAxisScreen.sqrMagnitude;
            if (rowAxisSqr < 0.0001f && columnAxisSqr < 0.0001f)
                return false;

            float rowCells = rowAxisSqr > 0.0001f
                ? Vector2.Dot(screenDelta, rowAxisScreen) / rowAxisSqr
                : 0f;
            float columnCells = columnAxisSqr > 0.0001f
                ? Vector2.Dot(screenDelta, columnAxisScreen) / columnAxisSqr
                : 0f;

            if (Mathf.Abs(columnCells) > Mathf.Abs(rowCells))
            {
                if (Mathf.Abs(columnCells) < 0.35f)
                    return false;

                columnStep = columnCells > 0f ? 1 : -1;
                rowStep = 0;
                requestedSteps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(columnCells)));
            }
            else
            {
                if (Mathf.Abs(rowCells) < 0.35f)
                    return false;

                rowStep = rowCells > 0f ? 1 : -1;
                columnStep = 0;
                requestedSteps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(rowCells)));
            }

            // Prefer finger end-cell when it agrees with the projected direction
            // (so a short or long swipe stops where the finger lifts).
            // If the finger is off-grid / disagrees (common near bottom exits),
            // keep the projection-based step count instead of rejecting the swipe.
            if (TryGetNearestGridCell(screenPosition, out int endRow, out int endColumn))
            {
                if (rowStep != 0)
                {
                    int rowDelta = endRow - _swipeStartRow;
                    if (rowDelta * rowStep > 0)
                        requestedSteps = Mathf.Abs(rowDelta);
                }
                else if (columnStep != 0)
                {
                    int columnDelta = endColumn - _swipeStartColumn;
                    if (columnDelta * columnStep > 0)
                        requestedSteps = Mathf.Abs(columnDelta);
                }
            }

            if (rowStep != 0)
            {
                requestedSteps = Mathf.Min(
                    requestedSteps,
                    rowStep > 0
                        ? _grid.Rows - 1 - _swipeStartRow
                        : _swipeStartRow);
            }
            else
            {
                requestedSteps = Mathf.Min(
                    requestedSteps,
                    columnStep > 0
                        ? _grid.Columns - 1 - _swipeStartColumn
                        : _swipeStartColumn);
            }

            return requestedSteps > 0;
        }

        private void GetGridAxisScreenVectors(
            int row,
            int column,
            out Vector2 rowAxisScreen,
            out Vector2 columnAxisScreen)
        {
            Vector2 originScreen = WorldToScreenPoint(_grid.GetWorldPosition(row, column));

            int rowNeighbor = row < _grid.Rows - 1 ? row + 1 : row - 1;
            int rowDirection = rowNeighbor > row ? 1 : -1;
            Vector2 rowNeighborScreen = WorldToScreenPoint(_grid.GetWorldPosition(rowNeighbor, column));
            rowAxisScreen = (rowNeighborScreen - originScreen) * rowDirection;

            int columnNeighbor = column < _grid.Columns - 1 ? column + 1 : column - 1;
            int columnDirection = columnNeighbor > column ? 1 : -1;
            Vector2 columnNeighborScreen = WorldToScreenPoint(_grid.GetWorldPosition(row, columnNeighbor));
            columnAxisScreen = (columnNeighborScreen - originScreen) * columnDirection;
        }

        private Vector2 WorldToScreenPoint(Vector3 worldPosition)
        {
            Vector3 screen = _gameplayCamera.WorldToScreenPoint(worldPosition);
            return new Vector2(screen.x, screen.y);
        }

        private bool TryResolveExit(int rowStep, int columnStep, int row, int column, out CarryBlockJamExit exitComponent)
        {
            exitComponent = null;
            if (!HasCarriedPlates || board == null || (columnStep == 0 && rowStep == 0))
                return false;

            BoardBorderSide side = ResolveExitSide(rowStep, columnStep);
            if (!IsOnExitBoundary(side, row, column))
                return false;

            exitComponent = FindMatchingExit(side, row, column, CarriedColor);
            if (exitComponent == null)
                return false;

            return true;
        }

        private bool TryResolveExitAtCell(int row, int column, out CarryBlockJamExit exitComponent)
        {
            exitComponent = null;
            if (!HasCarriedPlates || board == null)
                return false;

            exitComponent = FindMatchingExit(row, column, CarriedColor);
            if (exitComponent == null)
                return false;

            return true;
        }

        private static BoardBorderSide ResolveExitSide(int rowStep, int columnStep)
        {
            if (columnStep < 0)
                return BoardBorderSide.Left;
            if (columnStep > 0)
                return BoardBorderSide.Right;
            if (rowStep < 0)
                return BoardBorderSide.Top;
            return BoardBorderSide.Bottom;
        }

        private bool IsOnExitBoundary(BoardBorderSide side, int row, int column)
        {
            return side switch
            {
                BoardBorderSide.Left => column == 0,
                BoardBorderSide.Right => column == _grid.Columns - 1,
                BoardBorderSide.Top => row == 0,
                BoardBorderSide.Bottom => row == _grid.Rows - 1,
                _ => false,
            };
        }

        private CarryBlockJamExit[] GetRuntimeExits()
        {
            if (board == null)
                return System.Array.Empty<CarryBlockJamExit>();

            return board.GetComponentsInChildren<CarryBlockJamExit>(true);
        }

        private CarryBlockJamExit FindMatchingExit(
            BoardBorderSide side,
            int row,
            int column,
            PieceColorType color)
        {
            CarryBlockJamExit[] exits = GetRuntimeExits();
            if (exits == null)
                return null;

            for (int i = 0; i < exits.Length; i++)
            {
                CarryBlockJamExit exit = exits[i];
                if (exit == null || exit.Side != side || !exit.CanAccept(color))
                    continue;

                if (IsCellOnExit(row, column, exit))
                    return exit;
            }

            return null;
        }

        private CarryBlockJamExit FindMatchingExit(int row, int column, PieceColorType color)
        {
            CarryBlockJamExit[] exits = GetRuntimeExits();
            if (exits == null)
                return null;

            for (int i = 0; i < exits.Length; i++)
            {
                CarryBlockJamExit exit = exits[i];
                if (exit == null || !exit.CanAccept(color))
                    continue;

                if (IsCellOnExit(row, column, exit))
                    return exit;
            }

            return null;
        }

        private bool IsCellOnExit(int row, int column, CarryBlockJamExit exit)
        {
            if (exit == null || _grid == null)
                return false;

            if (exit.Row >= 0 && exit.Column >= 0)
                return row == exit.Row && column == exit.Column;

            int length = Mathf.Max(1, exit.Length);
            return exit.Side switch
            {
                BoardBorderSide.Left => column == 0 &&
                                        row >= exit.StartIndex &&
                                        row < exit.StartIndex + length,
                BoardBorderSide.Right => column == _grid.Columns - 1 &&
                                         row >= exit.StartIndex &&
                                         row < exit.StartIndex + length,
                BoardBorderSide.Top => row == 0 &&
                                       column >= exit.StartIndex &&
                                       column < exit.StartIndex + length,
                BoardBorderSide.Bottom => row == _grid.Rows - 1 &&
                                          column >= exit.StartIndex &&
                                          column < exit.StartIndex + length,
                _ => false,
            };
        }

        private bool TryGetNearestGridCell(Vector2 screenPosition, out int row, out int column)
        {
            row = -1;
            column = -1;

            if (_gameplayCamera == null)
                ResolveGameplayReferences();

            if (_gameplayCamera == null)
                return false;

            Ray ray = _gameplayCamera.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(board.transform.up, board.transform.position);
            if (!plane.Raycast(ray, out float distance))
                return false;

            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 localPos = board.transform.InverseTransformPoint(worldPoint);

            column = Mathf.RoundToInt(localPos.x / _grid.GridSpacingX + (_grid.Columns - 1) * 0.5f);
            row = Mathf.RoundToInt((_grid.Rows - 1) * 0.5f - localPos.z / _grid.GridSpacingZ);

            row = Mathf.Clamp(row, 0, _grid.Rows - 1);
            column = Mathf.Clamp(column, 0, _grid.Columns - 1);
            return true;
        }

        private void EnsureHighlightRoot()
        {
            if (_highlightRoot != null)
                return;

            Transform existing = transform.Find("SwipeHighlights");
            if (existing != null)
            {
                _highlightRoot = existing;
                return;
            }

            var rootObject = new GameObject("SwipeHighlights");
            _highlightRoot = rootObject.transform;
            _highlightRoot.SetParent(transform, false);
        }

        private void DrawHighlights(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                ShowHighlights(false);
                return;
            }

            EnsureHighlightRoot();
            EnsureHighlightCount(path.Count);

            for (int i = 0; i < _highlightPool.Count; i++)
            {
                bool active = i < path.Count;
                Transform highlight = _highlightPool[i];
                if (highlight == null)
                    continue;

                highlight.gameObject.SetActive(active);
                if (!active)
                    continue;

                Vector2Int step = path[i];
                highlight.position = ResolveHighlightWorldPosition(step.x, step.y);
                UpdateHighlightVisual(highlight, path, i);
            }
        }

        private Vector3 ResolveHighlightWorldPosition(int row, int column)
        {
            Vector3 cellCenter = _grid.GetWorldPosition(row, column);
            float topY = cellCenter.y + highlightHeight;

            if (_grid.TryGetCell(row, column, out PuzzleCell cell) && cell?.Slot != null)
            {
                Renderer[] renderers = cell.Slot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || !renderer.enabled)
                        continue;

                    topY = Mathf.Max(topY, renderer.bounds.max.y + 0.05f);
                }
            }

            return new Vector3(cellCenter.x, topY, cellCenter.z);
        }

        private void EnsureHighlightCount(int count)
        {
            while (_highlightPool.Count < count)
            {
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                highlight.name = $"SwipeCell_{_highlightPool.Count}";
                highlight.transform.SetParent(_highlightRoot, false);
                highlight.transform.localScale = new Vector3(
                    Mathf.Min(board.CellScaleXYZ.x, _grid.GridSpacingX * 0.85f),
                    0.08f,
                    Mathf.Min(board.CellScaleXYZ.z, _grid.GridSpacingZ * 0.85f));

                Renderer renderer = highlight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null)
                        shader = Shader.Find("Standard");

                    Material material = new Material(shader);
                    material.color = highlightColor;
                    material.renderQueue = 3000;
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                Collider collider = highlight.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                _highlightPool.Add(highlight.transform);
            }
        }

        private void UpdateHighlightVisual(Transform highlight, List<Vector2Int> path, int index)
        {
            if (highlight == null || path == null || index < 0 || index >= path.Count)
                return;

            Vector2Int direction = Vector2Int.zero;
            if (path.Count > 1)
            {
                if (index < path.Count - 1)
                    direction = path[index + 1] - path[index];
                else
                    direction = path[index] - path[index - 1];
            }

            float widthX = Mathf.Min(board.CellScaleXYZ.x, _grid.GridSpacingX * 0.8f);
            float widthZ = Mathf.Min(board.CellScaleXYZ.z, _grid.GridSpacingZ * 0.8f);
            float stretchX = direction.y != 0 ? _grid.GridSpacingX * 1.02f : widthX;
            float stretchZ = direction.x != 0 ? _grid.GridSpacingZ * 1.02f : widthZ;
            highlight.localScale = new Vector3(stretchX, 0.08f, stretchZ);

            Renderer renderer = highlight.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                return;

            float flow = Mathf.Sin(Time.time * 10f - index * 0.55f) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(0.28f, highlightColor.a, flow);
            float brightness = Mathf.Lerp(0.82f, 1.18f, flow);
            Color animatedColor = new Color(
                Mathf.Clamp01(highlightColor.r * brightness),
                Mathf.Clamp01(highlightColor.g * brightness),
                Mathf.Clamp01(highlightColor.b * brightness),
                alpha);
            renderer.sharedMaterial.color = animatedColor;
        }

        private void ShowHighlights(bool visible)
        {
            for (int i = 0; i < _highlightPool.Count; i++)
                _highlightPool[i].gameObject.SetActive(visible);
        }

        private bool CanPickUpPiece(CarryBlockJamBoardPiece piece, int atRow = -1, int atColumn = -1)
        {
            if (piece == null || piece.Kind != CarryBlockJamPieceKind.Plate)
                return false;

            int checkRow = atRow >= 0 ? atRow : piece.Row;
            int checkColumn = atColumn >= 0 ? atColumn : piece.Column;
            if (TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                !TutorialManager.Instance.CanCollectTutorialPlate(checkRow, checkColumn))
                return false;

            return !HasCarriedPlates || piece.Color == CarriedColor;
        }

        private bool IsMatchingDropTarget(CarryBlockJamBoardPiece piece)
        {
            if (piece == null || !HasCarriedPlates)
                return false;

            if (piece.Kind == CarryBlockJamPieceKind.Box)
                return piece.Color == CarriedColor;

            CarryBlockJamBoardPiece storageBox = GetStorageBox(piece);
            return storageBox != null && storageBox.Color == CarriedColor;
        }

        private bool HasCarriedPlates => _carriedPlates.Count > 0;

        private PieceColorType CarriedColor =>
            HasCarriedPlates ? _carriedPlates[0].Color : PieceColorType.None;

        private Tween AddPlateToCarryStack(CarryBlockJamBoardPiece plate, int pickupIndex)
        {
            if (plate == null)
                return null;

            plate.ClearStackLinks();
            Transform attachRoot = GetCarryAttachRoot();
            plate.transform.SetParent(attachRoot, true);
            if (!_carriedPlates.Contains(plate))
                _carriedPlates.Add(plate);

            int stackIndex = _carriedPlates.IndexOf(plate);
            Vector3 targetPosition = GetCarriedPlateLocalPosition(stackIndex);
            float animationSpeed = Mathf.Max(0.01f, pickupPlateAnimationSpeed);
            float bounceDuration = Mathf.Max(0.01f, pickupPlateBounceDuration / animationSpeed);
            float bounceHeight = Mathf.Max(0.01f, pickupPlateBounceHeight);
            Vector3 startPosition = plate.transform.localPosition;
            Vector3 horizontalFromStickman = new Vector3(startPosition.x, 0f, startPosition.z);
            Vector3 outward = horizontalFromStickman.sqrMagnitude > 0.001f
                ? horizontalFromStickman.normalized * 0.2f
                : Vector3.right * (pickupIndex % 2 == 0 ? 0.2f : -0.2f);
            Vector3 liftPosition = startPosition + Vector3.up * bounceHeight + outward;
            Vector3 frontWaypoint = targetPosition +
                Vector3.forward * Mathf.Max(0f, pickupPlateFrontClearance) +
                Vector3.up * (bounceHeight * 0.45f);

            Sequence bounce = DOTween.Sequence();
            bounce.SetDelay(Mathf.Max(0f, pickupPlateStagger) * pickupIndex / animationSpeed);
            bounce.Append(plate.transform.DOLocalMove(
                liftPosition,
                bounceDuration * 0.35f).SetEase(Ease.OutQuad));
            bounce.Append(plate.transform.DOLocalPath(
                new[] { frontWaypoint, targetPosition },
                bounceDuration * 0.65f,
                PathType.CatmullRom,
                PathMode.Ignore).SetEase(Ease.InOutSine));
            bounce.Insert(0f, plate.transform.DOLocalRotate(
                Vector3.zero,
                bounceDuration,
                RotateMode.Fast).SetEase(Ease.OutQuad));
            float settleDuration = Mathf.Max(0.01f, pickupPlateSettleDuration / animationSpeed);
            bounce.Append(plate.transform.DOLocalJump(
                targetPosition,
                bounceHeight * 0.22f,
                1,
                settleDuration).SetEase(Ease.OutQuad));
            bounce.Join(plate.transform.DOPunchScale(
                plate.transform.localScale * Mathf.Max(0f, pickupPlateSettleScale),
                settleDuration,
                6,
                0.5f));
            return bounce;
        }

        private void AddPlatesToCarryStack(
            List<CarryBlockJamBoardPiece> plates,
            int pickupRow = -1,
            int pickupColumn = -1,
            bool notifyTutorialComplete = false)
        {
            if (plates == null || plates.Count == 0)
                return;

            if (_plateCollectionTween != null && _plateCollectionTween.IsActive())
                _plateCollectionTween.Complete();

            Sequence collection = DOTween.Sequence();
            for (int i = 0; i < plates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = plates[i];
                Tween bounce = AddPlateToCarryStack(plate, i);
                if (bounce != null)
                    collection.Join(bounce);
                CarryBlockJamHiddenBox.NotifyPlateCollected(plate);
                CarryBlockJamFrozenBox.NotifyPlateCollected(plate);
            }

            collection.OnComplete(() =>
            {
                if (!_failTriggered)
                    UpdateCarriedPlateVisuals();
                _plateCollectionTween = null;
            });
            _plateCollectionTween = collection;
            Haptic.LightTaptic();
            RefreshStickmanAnimation(moving: false);

            // Tutorial stages advance only when the start→target move finishes, never from pickup alone.
            if (notifyTutorialComplete &&
                TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                pickupRow >= 0 &&
                TutorialManager.Instance.IsTutorialTargetCell(pickupRow, pickupColumn))
                TutorialManager.Instance.NotifyTutorialActionCompleted();

            EvaluateCarriedPlateDeadlock();
        }

        private void UpdateCarriedPlateVisuals()
        {
            Transform attachRoot = GetCarryAttachRoot();
            for (int i = 0; i < _carriedPlates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = _carriedPlates[i];
                if (plate == null)
                    continue;

                plate.transform.SetParent(attachRoot, false);
                plate.transform.localPosition = GetCarriedPlateLocalPosition(i);
                plate.transform.localRotation = Quaternion.identity;
            }
        }

        private Vector3 GetCarriedPlateLocalPosition(int stackIndex)
        {
            // Hold plates in front of the torso; stack upward without clipping the head.
            Vector3 stackOffset = Vector3.up * (carriedPlateStackStep * stackIndex);
            // Slight forward bias on higher plates only.
            stackOffset += Vector3.forward * (0.02f * stackIndex);
            return carriedPlateBaseOffset + stackOffset;
        }

        private Transform GetCarryAttachRoot()
        {
            if (_cylinder == null)
                return transform;

            Transform visual = GetStickmanVisual();
            return visual != null ? visual : _cylinder.transform;
        }

        private List<CarryBlockJamBoardPiece> DetachCarriedPlates()
        {
            CompletePlateCollectionAnimation();
            var detached = new List<CarryBlockJamBoardPiece>(_carriedPlates);
            _carriedPlates.Clear();
            return detached;
        }

        private List<CarryBlockJamBoardPiece> DetachCarriedPlates(int count)
        {
            CompletePlateCollectionAnimation();
            int resolvedCount = Mathf.Clamp(count, 0, _carriedPlates.Count);
            var detached = new List<CarryBlockJamBoardPiece>(resolvedCount);
            for (int i = 0; i < resolvedCount; i++)
                detached.Add(_carriedPlates[i]);

            if (resolvedCount > 0)
                _carriedPlates.RemoveRange(0, resolvedCount);

            return detached;
        }

        private void CompletePlateCollectionAnimation()
        {
            if (_plateCollectionTween != null && _plateCollectionTween.IsActive())
                _plateCollectionTween.Complete();
            _plateCollectionTween = null;
        }

        private static CarryBlockJamBoardPiece GetTopStackPiece(CarryBlockJamBoardPiece basePiece)
        {
            CarryBlockJamBoardPiece current = basePiece;
            while (current != null && current.StackedAbove != null)
                current = current.StackedAbove;

            return current;
        }

        private static CarryBlockJamBoardPiece GetStorageBox(CarryBlockJamBoardPiece piece)
        {
            CarryBlockJamBoardPiece current = piece;
            while (current != null)
            {
                if (current.Kind == CarryBlockJamPieceKind.Box)
                    return current;

                current = current.StackedBelow;
            }

            return null;
        }

        private static bool ShouldPickupFromPreviousCell(CarryBlockJamBoardPiece piece)
        {
            if (piece == null)
                return false;

            CarryBlockJamBoardPiece storageBox = GetStorageBox(piece);
            return storageBox != null && storageBox != piece;
        }

        private static bool IsBoxCellBlocker(CarryBlockJamBoardPiece piece)
        {
            if (piece == null)
                return false;

            if (piece.Kind == CarryBlockJamPieceKind.Box)
                return true;

            return GetStorageBox(piece) != null;
        }

        private static CarryBlockJamBoardPiece GetPickupBasePiece(CarryBlockJamBoardPiece piece)
        {
            if (piece == null)
                return null;

            // Always start at the bottom of the stack so a table under plates stays
            // registered as the cell occupant after plates are picked up.
            CarryBlockJamBoardPiece current = piece;
            while (current.StackedBelow != null)
                current = current.StackedBelow;

            return current;
        }

        public void PrepareForFailure(Action completed)
        {
            _failTriggered = true;
            _trackingSwipe = false;
            ShowHighlights(false);
            if (_plateCollectionTween != null && _plateCollectionTween.IsActive())
                _plateCollectionTween.Kill();
            _plateCollectionTween = null;

            if (_failurePreparing)
                return;

            _failurePreparing = true;
            StartCoroutine(PlayFailureSequence(completed));
        }

        private IEnumerator PlayFailureSequence(Action completed)
        {
            // Let an in-flight move or delivery finish so its occupancy and plate
            // lists are not left half-updated.
            while (_isAnimating)
                yield return null;

            ResolveGameplayReferences();
            float sequenceStart = Time.realtimeSinceStartup;

            Tween plateDrop = DropCarriedPlatesForFailure();
            RefreshStickmanAnimation(moving: false);
            _stickmanAnimator?.PlayFailure();

            if (plateDrop != null && plateDrop.IsActive())
                yield return plateDrop.WaitForCompletion();

            float remainingDelay = Mathf.Max(
                0f,
                failureUiDelay - (Time.realtimeSinceStartup - sequenceStart));
            if (remainingDelay > 0f)
                yield return new WaitForSecondsRealtime(remainingDelay);

            completed?.Invoke();
        }

        private Tween DropCarriedPlatesForFailure()
        {
            if (!HasCarriedPlates)
                return null;

            List<CarryBlockJamBoardPiece> plates = DetachCarriedPlates();
            Transform piecesRoot = GetPiecesRoot();
            List<Vector2Int> dropCells = FindFailureDropCells(plates.Count);
            Vector3 visualFallbackCenter =
                _cylinder != null && _grid != null && _grid.IsInside(_cylinder.Row, _cylinder.Column)
                    ? _grid.GetWorldPosition(_cylinder.Row, _cylinder.Column)
                    : transform.position;

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            for (int i = 0; i < plates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = plates[i];
                if (plate == null)
                    continue;

                plate.ClearStackLinks();
                plate.transform.SetParent(piecesRoot, true);

                Vector3 target;
                if (i < dropCells.Count)
                {
                    Vector2Int dropCell = dropCells[i];
                    Vector3 gridTarget = _grid.GetWorldPosition(dropCell.x, dropCell.y);
                    target = gridTarget + board.transform.TransformVector(plate.GridOffset);
                }
                else
                {
                    float angle = plates.Count > 1 ? i * Mathf.PI * 2f / plates.Count : 0f;
                    target = visualFallbackCenter +
                        board.transform.TransformVector(plate.GridOffset) +
                        new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.65f;
                }

                Tween flyTween = plate.transform.DOJump(
                    target,
                    Mathf.Max(0.01f, failurePlateFlyHeight),
                    1,
                    Mathf.Max(0.01f, failurePlateDropDuration))
                    .SetDelay(Mathf.Max(0f, failurePlateSpreadStagger) * i)
                    .SetEase(Ease.OutQuad);
                sequence.Join(flyTween);
            }

            sequence.OnComplete(() =>
            {
                int placedCount = Mathf.Min(plates.Count, dropCells.Count);
                for (int i = 0; i < placedCount; i++)
                {
                    CarryBlockJamBoardPiece plate = plates[i];
                    if (plate == null)
                        continue;

                    Vector2Int dropCell = dropCells[i];
                    plate.PlaceOnGrid(_grid, piecesRoot, dropCell.x, dropCell.y);
                    if (_grid.TryGetCell(dropCell.x, dropCell.y, out PuzzleCell cell) && cell != null)
                        cell.Occupant = plate.gameObject;
                }
            });

            return sequence;
        }

        private List<Vector2Int> FindFailureDropCells(int requestedCount)
        {
            var result = new List<Vector2Int>(Mathf.Max(0, requestedCount));
            if (requestedCount <= 0)
                return result;
            if (_grid == null || _cylinder == null || !_grid.IsBuilt)
                return result;

            int startRow = _cylinder.Row;
            int startColumn = _cylinder.Column;
            if (!_grid.IsInside(startRow, startColumn))
                return result;

            int cellCount = _grid.Rows * _grid.Columns;
            var visited = new bool[cellCount];
            var queue = new Queue<Vector2Int>(cellCount);
            queue.Enqueue(new Vector2Int(startRow, startColumn));
            visited[startRow * _grid.Columns + startColumn] = true;

            int[] rowOffsets = { -1, 1, 0, 0 };
            int[] columnOffsets = { 0, 0, -1, 1 };
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nextRow = current.x + rowOffsets[i];
                    int nextColumn = current.y + columnOffsets[i];
                    if (!_grid.IsInside(nextRow, nextColumn))
                        continue;

                    int index = nextRow * _grid.Columns + nextColumn;
                    if (visited[index])
                        continue;
                    visited[index] = true;

                    if (!_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell cell) || cell == null)
                        continue;

                    bool isEmptyGridCell =
                        cell.Occupant == null &&
                        !IsBoxOwnedCell(nextRow, nextColumn) &&
                        !IsExitCell(nextRow, nextColumn);
                    if (isEmptyGridCell)
                    {
                        result.Add(new Vector2Int(nextRow, nextColumn));
                        if (result.Count >= requestedCount)
                            return result;
                    }

                    // Keep expanding through empty cells so plates spread across
                    // the nearest available part of the board.
                    if (cell.Occupant == null || cell.Occupant == _cylinder.gameObject)
                        queue.Enqueue(new Vector2Int(nextRow, nextColumn));
                }
            }

            return result;
        }

        private bool IsExitCell(int row, int column)
        {
            CarryBlockJamExit[] exits = GetRuntimeExits();
            for (int i = 0; i < exits.Length; i++)
            {
                if (exits[i] != null && IsCellOnExit(row, column, exits[i]))
                    return true;
            }

            return false;
        }

        private void TryTriggerSuccess(List<CarryBlockJamBoardPiece> ignoredPlates = null)
        {
            if (_successTriggered || _failTriggered)
                return;

            if (HasCarriedPlates)
                return;

            CarryBlockJamBoardPiece[] pieces = GetComponentsInChildren<CarryBlockJamBoardPiece>(true);
            for (int i = 0; i < pieces.Length; i++)
            {
                CarryBlockJamBoardPiece piece = pieces[i];
                if (piece == null || piece.Kind != CarryBlockJamPieceKind.Plate)
                    continue;

                if (ignoredPlates != null && ignoredPlates.Contains(piece))
                    continue;

                return;
            }

            if (LevelManager.instance == null)
                return;

            CarryBlockJamExit[] exits = GetRuntimeExits();
            for (int i = 0; i < exits.Length; i++)
            {
                if (exits[i] != null && !exits[i].IsCompleted)
                    return;
            }

            _successTriggered = true;
            Haptic.MediumTaptic();
            LevelManager.instance.Success();
        }

        /// <summary>
        /// While carrying a color, fail if that color cannot reach a matching table or matching exit.
        /// Same-color plates count as walkable because they can be picked up along the way.
        /// </summary>
        private void EvaluateCarriedPlateDeadlock()
        {
            if (_failTriggered || _successTriggered || !HasCarriedPlates)
                return;
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return;
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
                return;
            if (LevelManager.instance == null)
                return;

            if (HasReachableSinkForCarriedPlates())
                return;

            _failTriggered = true;
            LevelManager.instance.Fail();
        }

        private bool HasReachableSinkForCarriedPlates()
        {
            PieceColorType color = CarriedColor;
            if (color == PieceColorType.None)
                return false;

            int startRow = _cylinder.Row;
            int startColumn = _cylinder.Column;
            if (!_grid.IsInside(startRow, startColumn))
                return false;

            if (IsCarriedPlateSinkCell(startRow, startColumn, color))
                return true;

            int cellCount = _grid.Rows * _grid.Columns;
            var visited = new bool[cellCount];
            var queue = new Queue<Vector2Int>(cellCount);
            queue.Enqueue(new Vector2Int(startRow, startColumn));
            visited[startRow * _grid.Columns + startColumn] = true;

            int[] rowOffsets = { -1, 1, 0, 0 };
            int[] colOffsets = { 0, 0, -1, 1 };

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nextRow = cell.x + rowOffsets[i];
                    int nextColumn = cell.y + colOffsets[i];
                    if (!_grid.IsInside(nextRow, nextColumn))
                        continue;

                    int visitIndex = nextRow * _grid.Columns + nextColumn;
                    if (visited[visitIndex])
                        continue;
                    if (!IsWalkableWhileCarrying(nextRow, nextColumn, color))
                        continue;

                    if (IsCarriedPlateSinkCell(nextRow, nextColumn, color))
                        return true;

                    visited[visitIndex] = true;
                    queue.Enqueue(new Vector2Int(nextRow, nextColumn));
                }
            }

            return false;
        }

        private bool IsWalkableWhileCarrying(int row, int column, PieceColorType carriedColor)
        {
            if (IsBoxOwnedCell(row, column))
                return false;

            if (!_grid.TryGetCell(row, column, out PuzzleCell cell) || cell == null)
                return false;

            if (cell.Occupant == null || cell.Occupant == _cylinder.gameObject)
                return true;

            CarryBlockJamBoardPiece piece = cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
            if (piece == null || piece == _cylinder)
                return true;

            if (IsBoxCellBlocker(piece))
                return false;

            return piece.Kind == CarryBlockJamPieceKind.Plate && piece.Color == carriedColor;
        }

        private bool IsCarriedPlateSinkCell(int row, int column, PieceColorType carriedColor)
        {
            if (FindMatchingExit(row, column, carriedColor) != null)
                return true;

            return IsOrthogonallyAdjacentToMatchingTable(row, column, carriedColor);
        }

        private bool IsOrthogonallyAdjacentToMatchingTable(int row, int column, PieceColorType carriedColor)
        {
            int[] rowOffsets = { -1, 1, 0, 0 };
            int[] colOffsets = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nextRow = row + rowOffsets[i];
                int nextColumn = column + colOffsets[i];
                if (!_grid.IsInside(nextRow, nextColumn))
                    continue;
                if (!_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell cell) || cell == null)
                    continue;
                if (cell.Occupant == null)
                    continue;

                CarryBlockJamBoardPiece piece = cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (piece == null)
                    continue;

                CarryBlockJamBoardPiece storageBox = GetStorageBox(piece) ??
                    (piece.Kind == CarryBlockJamPieceKind.Box ? piece : null);
                if (storageBox != null && storageBox.Color == carriedColor)
                    return true;
            }

            return false;
        }
    }
}
