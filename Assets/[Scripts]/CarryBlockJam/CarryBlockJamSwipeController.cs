using GAITemplate;
using DG.Tweening;
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
        [SerializeField] private float highlightHeight = 0.35f;
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.84f, 1f, 0.9f);
        [SerializeField] private Vector3 carriedPlateBaseOffset = new Vector3(0f, 0.85f, 0.40f);
        [SerializeField] private float carriedPlateStackStep = 0.18f;
        [SerializeField] private Vector3 stickmanCarryOffset = new Vector3(0f, -0.7f, 0f);

        private Camera _gameplayCamera;
        private PuzzleGrid _grid;
        private Vector2 _swipeStartScreen;
        private int _swipeStartRow;
        private int _swipeStartColumn;
        private bool _trackingSwipe;
        private bool _isAnimating;
        private bool _successTriggered;
        private CarryBlockJamBoardPiece _cylinder;
        private CarryBlockJamStickmanAnimator _stickmanAnimator;
        private bool _stickmanMoving;
        private readonly List<CarryBlockJamBoardPiece> _carriedPlates = new List<CarryBlockJamBoardPiece>();
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
            if (_isAnimating)
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

            if (!TryGetSwipeIntent(screenPosition, out int rowStep, out int columnStep, out int requestedSteps))
                return;

            bool tutorialActive =
                TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
            bool guidedTutorial =
                tutorialActive && TutorialManager.Instance.UsesGuidedPathLock;

            if (guidedTutorial)
            {
                if (!TutorialManager.Instance.TryEngageStagePathLock(
                        _cylinder.Row,
                        _cylinder.Column,
                        rowStep,
                        columnStep))
                {
                    ExecuteNormalSwipe(rowStep, columnStep, requestedSteps, swipeUntilPlate: true);
                    return;
                }

                if (!TutorialManager.Instance.TryClampSwipeToAuthoredPath(
                        _cylinder.Row,
                        _cylinder.Column,
                        ref rowStep,
                        ref columnStep,
                        ref requestedSteps))
                {
                    TutorialManager.Instance.ReleaseStagePathLock();
                    ExecuteNormalSwipe(rowStep, columnStep, requestedSteps, swipeUntilPlate: true);
                    return;
                }

                if (!TryExecuteTutorialTravelSwipe(rowStep, columnStep, requestedSteps))
                {
                    TutorialManager.Instance.ReleaseStagePathLock();
                    ExecuteNormalSwipe(rowStep, columnStep, requestedSteps, swipeUntilPlate: true);
                }
                return;
            }

            // Hidden-reveal tutorial (or free roam): swipe continues until a plate / blocker.
            if (tutorialActive && !HasCarriedPlates)
                requestedSteps = Mathf.Max(requestedSteps, GetMaxStepsInDirection(rowStep, columnStep));

            ExecuteNormalSwipe(rowStep, columnStep, requestedSteps, swipeUntilPlate: false);
        }

        private void ExecuteNormalSwipe(int rowStep, int columnStep, int requestedSteps, bool swipeUntilPlate)
        {
            if (swipeUntilPlate && !HasCarriedPlates)
                requestedSteps = Mathf.Max(requestedSteps, GetMaxStepsInDirection(rowStep, columnStep));

            if (HasCarriedPlates)
                ExecuteCarrySwipe(rowStep, columnStep, requestedSteps);
            else
                ExecuteTravelSwipe(rowStep, columnStep, requestedSteps);
        }

        private int GetMaxStepsInDirection(int rowStep, int columnStep)
        {
            if (_grid == null || _cylinder == null)
                return 0;

            if (rowStep > 0)
                return Mathf.Max(0, _grid.Rows - 1 - _cylinder.Row);
            if (rowStep < 0)
                return Mathf.Max(0, _cylinder.Row);
            if (columnStep > 0)
                return Mathf.Max(0, _grid.Columns - 1 - _cylinder.Column);
            if (columnStep < 0)
                return Mathf.Max(0, _cylinder.Column);
            return 0;
        }

        /// <summary>
        /// Tutorial travel: current cell → targetCell along the authored segment (target inclusive).
        /// Works empty-handed (pickup) or while carrying plates.
        /// </summary>
        private bool TryExecuteTutorialTravelSwipe(int rowStep, int columnStep, int requestedSteps)
        {
            if (!TutorialManager.Instance.TryGetActivePath(out _, out Vector2Int target))
                return false;

            int fromRow = _cylinder.Row;
            int fromCol = _cylinder.Column;

            var path = new List<Vector2Int>();
            if (!TutorialManager.Instance.TryBuildAuthoredPathCells(fromRow, fromCol, path))
                return false;

            CarryBlockJamBoardPiece pickupSource = null;
            int pickupRow = target.x;
            int pickupColumn = target.y;
            List<CarryBlockJamBoardPiece> pickupPieces = null;

            if (!HasCarriedPlates &&
                _grid.TryGetCell(target.x, target.y, out PuzzleCell targetCell) &&
                targetCell != null &&
                targetCell.Occupant != null)
            {
                CarryBlockJamBoardPiece targetPiece =
                    targetCell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (CanPickUpPiece(targetPiece, target.x, target.y))
                    pickupSource = targetPiece;
                else if (targetPiece != null)
                    return false;
            }

            if (!IsTutorialTravelPathClear(path, target, pickupSource))
                return false;

            bool stopBeforeTableDrop = TrimTutorialPathBeforeTableDrop(path, target);

            if (!stopBeforeTableDrop && path.Count != requestedSteps)
                return false;
            if (stopBeforeTableDrop && path.Count != requestedSteps - 1)
                return false;

            if (pickupSource != null)
                pickupPieces = ExtractPickupPlates(pickupSource, pickupRow, pickupColumn);

            if (path.Count == 0)
            {
                if (pickupPieces != null)
                    AddPlatesToCarryStack(pickupPieces, pickupRow, pickupColumn);
                else if (HasCarriedPlates)
                    CompleteTutorialTargetArrival(target.x, target.y);

                if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
                    TutorialManager.Instance.ReleaseStagePathLock();
                return true;
            }

            AnimateTutorialCylinderTravel(path, () =>
            {
                if (pickupPieces != null)
                    AddPlatesToCarryStack(pickupPieces, pickupRow, pickupColumn);
                else
                    CompleteTutorialTargetArrival(target.x, target.y);

                if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
                    TutorialManager.Instance.ReleaseStagePathLock();
            });
            return true;
        }

        private bool IsTutorialTravelPathClear(
            List<Vector2Int> path,
            Vector2Int target,
            CarryBlockJamBoardPiece pickupSource)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int step = path[i];
                bool isTargetStep = step.x == target.x && step.y == target.y;
                if (!_grid.TryGetCell(step.x, step.y, out PuzzleCell cell) || cell == null)
                    return false;

                if (cell.Occupant == null)
                    continue;

                CarryBlockJamBoardPiece occupant = cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (occupant == null)
                    continue;

                if (HasCarriedPlates)
                {
                    if (isTargetStep && TryResolveExitAtCell(step.x, step.y, out _))
                        continue;
                    if (isTargetStep && IsMatchingDropTarget(occupant))
                        continue;
                    if (IsBoxCellBlocker(occupant))
                        return false;
                    return false;
                }

                if (isTargetStep && pickupSource != null && occupant == pickupSource)
                    continue;

                return false;
            }

            return true;
        }

        private void CompleteTutorialTargetArrival(int targetRow, int targetColumn)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsActive)
                return;

            if (HasCarriedPlates)
            {
                if (TryResolveExitAtCell(targetRow, targetColumn, out CarryBlockJamExit exit) &&
                    exit != null &&
                    exit.CanAccept(CarriedColor))
                {
                    SendCarriedPlatesToExit(exit, null);
                    TutorialManager.Instance.NotifyTutorialActionCompleted();
                    return;
                }

                if (TryResolveDropTargetAtCell(targetRow, targetColumn, out CarryBlockJamBoardPiece dropBox))
                {
                    AnimateCarriedPlatesToBox(dropBox);
                    return;
                }
            }

            TutorialManager.Instance.NotifyTutorialActionCompleted();
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
            Haptic.LightTaptic();

            if (_cylinder.Row >= 0 && _cylinder.Column >= 0 && _grid.IsInside(_cylinder.Row, _cylinder.Column))
                _grid.ClearOccupant(_cylinder.Row, _cylinder.Column);

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

                // Capture per-step indices for the callback (avoid loop-variable capture issues).
                int placeRow = targetRow;
                int placeColumn = targetColumn;
                sequence.AppendCallback(() =>
                {
                    _cylinder.PlaceOnGrid(_grid, GetPiecesRoot(), placeRow, placeColumn);
                    if (_grid.TryGetCell(placeRow, placeColumn, out PuzzleCell stepCell) && stepCell != null)
                        stepCell.Occupant = _cylinder.gameObject;
                });
            }

            sequence.OnComplete(() =>
            {
                _isAnimating = false;
                RefreshStickmanAnimation(moving: false);
                onComplete?.Invoke();
            });
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
                return;
            }

            AnimateCylinderTravel(safePath, onComplete);
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
            Haptic.LightTaptic();

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
                    {
                        _cylinder.PlaceOnGrid(_grid, GetPiecesRoot(), targetRow, targetColumn);
                    }
                });
            }

            Vector2Int finalStep = path[path.Count - 1];
            sequence.OnComplete(() =>
            {
                if (_grid.TryGetCell(finalStep.x, finalStep.y, out PuzzleCell cell) && cell != null)
                {
                    CarryBlockJamBoardPiece occupantPiece = cell.Occupant != null
                        ? cell.Occupant.GetComponent<CarryBlockJamBoardPiece>()
                        : null;

                    if (!IsBoxCellBlocker(occupantPiece))
                        cell.Occupant = _cylinder.gameObject;
                }

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
                sequence.Append(plate.transform.DOMove(targetPosition, exitPlateDeliveryDuration).SetEase(Ease.InQuad));
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
            CarryBlockJamBoardPiece currentTop = GetTopStackPiece(targetBox);
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < plates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = plates[i];
                CarryBlockJamBoardPiece basePiece = currentTop;
                if (plate == null || basePiece == null)
                    continue;

                plate.ClearStackLinks();
                plate.transform.SetParent(GetPiecesRoot(), true);
                Vector3 stackWorldTarget = basePiece.transform.TransformPoint(
                    basePiece.GetStackAttachLocalPosition(plate));
                sequence.Append(plate.transform.DOMove(stackWorldTarget, exitTravelDuration).SetEase(Ease.InQuad));
                sequence.AppendCallback(() =>
                {
                    plate.StackOnPiece(basePiece);
                    if (_grid.TryGetCell(targetBox.Row, targetBox.Column, out PuzzleCell cell) && cell != null)
                        cell.Occupant = plate.gameObject;
                });
                currentTop = plate;
            }

            sequence.OnComplete(() =>
            {
                _isAnimating = false;
                if (onComplete != null)
                    onComplete.Invoke();
                else
                    MaybeNotifyTutorialTableDrop(targetBox);
            });
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

            if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
            {
                bool guided = TutorialManager.Instance.UsesGuidedPathLock;
                if (!guided || !TutorialManager.Instance.IsStagePathLocked)
                {
                    int previewSteps = requestedSteps;
                    if (!HasCarriedPlates)
                        previewSteps = Mathf.Max(previewSteps, GetMaxStepsInDirection(rowStep, columnStep));
                    DrawHighlights(BuildPreviewPath(rowStep, columnStep, previewSteps));
                    return;
                }

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

        private void AddPlateToCarryStack(CarryBlockJamBoardPiece plate)
        {
            if (plate == null)
                return;

            plate.ClearStackLinks();
            plate.transform.SetParent(GetCarryAttachRoot(), false);
            if (!_carriedPlates.Contains(plate))
                _carriedPlates.Add(plate);
            UpdateCarriedPlateVisuals();
        }

        private void AddPlatesToCarryStack(List<CarryBlockJamBoardPiece> plates, int pickupRow = -1, int pickupColumn = -1)
        {
            if (plates == null || plates.Count == 0)
                return;

            bool completedTutorialTarget =
                TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                pickupRow >= 0 &&
                TutorialManager.Instance.IsTutorialTargetCell(pickupRow, pickupColumn);

            for (int i = 0; i < plates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = plates[i];
                AddPlateToCarryStack(plate);
                CarryBlockJamHiddenBox.NotifyPlateCollected(plate);
                CarryBlockJamFrozenBox.NotifyPlateCollected(plate);
            }

            Haptic.MediumTaptic();
            RefreshStickmanAnimation(moving: false);

            if (completedTutorialTarget)
                TutorialManager.Instance.NotifyTutorialActionCompleted();
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
                // Hold plates in front of the torso; stack upward without clipping the head.
                Vector3 stackOffset = Vector3.up * (carriedPlateStackStep * i);
                // Slight forward bias on higher plates only.
                stackOffset += Vector3.forward * (0.02f * i);
                plate.transform.localPosition = carriedPlateBaseOffset + stackOffset;
                plate.transform.localRotation = Quaternion.identity;
            }
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
            var detached = new List<CarryBlockJamBoardPiece>(_carriedPlates);
            _carriedPlates.Clear();
            return detached;
        }

        private List<CarryBlockJamBoardPiece> DetachCarriedPlates(int count)
        {
            int resolvedCount = Mathf.Clamp(count, 0, _carriedPlates.Count);
            var detached = new List<CarryBlockJamBoardPiece>(resolvedCount);
            for (int i = 0; i < resolvedCount; i++)
                detached.Add(_carriedPlates[i]);

            if (resolvedCount > 0)
                _carriedPlates.RemoveRange(0, resolvedCount);

            return detached;
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

        private void TryTriggerSuccess(List<CarryBlockJamBoardPiece> ignoredPlates = null)
        {
            if (_successTriggered)
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
    }
}
