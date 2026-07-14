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
        [SerializeField] private float moveDurationPerCell = 0.14f;
        [SerializeField] private float moveJumpPower = 0.18f;
        [SerializeField] private float exitTravelDuration = 0.18f;
        [SerializeField] private float highlightHeight = 0.35f;
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.84f, 1f, 0.9f);
        [SerializeField] private Vector3 carriedPlateBaseOffset = new Vector3(0f, 0.85f, 0.52f);
        [SerializeField] private float carriedPlateStackStep = 0.18f;
        [SerializeField] private Vector3 stickmanCarryOffset = new Vector3(0f, -0.75f, 0f);

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

        private void TryStartSwipe(Vector2 screenPosition)
        {
            if (_isAnimating)
                return;

            if (_cylinder == null)
                ResolveGameplayReferences();

            if (!TryGetNearestGridCell(screenPosition, out int row, out int column))
                return;

            _swipeStartScreen = screenPosition;
            _swipeStartRow = row;
            _swipeStartColumn = column;
            _trackingSwipe = true;
            EnsureHighlightRoot();
            ShowHighlights(false);
        }

        private void FinishSwipe(Vector2 screenPosition)
        {
            _trackingSwipe = false;
            ShowHighlights(false);

            if (!TryGetSwipeIntent(screenPosition, out int rowStep, out int columnStep, out int requestedSteps))
                return;

            ResolveGameplayReferences();
            if (_cylinder == null)
                return;

            if (HasCarriedPlates)
                ExecuteCarrySwipe(rowStep, columnStep, requestedSteps);
            else
                ExecuteTravelSwipe(rowStep, columnStep, requestedSteps);
        }

        private void ExecuteTravelSwipe(int rowStep, int columnStep, int requestedSteps)
        {
            int row = _cylinder.Row;
            int column = _cylinder.Column;
            var path = new List<Vector2Int>();
            List<CarryBlockJamBoardPiece> pickupPieces = null;

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
                    if (CanPickUpPiece(nextPiece))
                    {
                        bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                        pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
                        if (!pickupFromPreviousCell)
                            path.Add(new Vector2Int(nextRow, nextColumn));

                        MoveConnectedCylinder(path, () =>
                        {
                            AddPlatesToCarryStack(pickupPieces);
                        });
                        return;
                    }

                    break;
                }

                if (CanPickUpPiece(nextPiece))
                {
                    bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                    pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
                    if (!pickupFromPreviousCell)
                        path.Add(new Vector2Int(nextRow, nextColumn));

                    MoveConnectedCylinder(path, () =>
                    {
                        AddPlatesToCarryStack(pickupPieces);
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
                    else if (CanPickUpPiece(nextPiece) && nextPiece.Color == CarriedColor)
                    {
                        bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                        pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
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
                else if (CanPickUpPiece(nextPiece) && nextPiece.Color == CarriedColor)
                {
                    bool pickupFromPreviousCell = ShouldPickupFromPreviousCell(nextPiece);
                    pickupPieces = ExtractPickupPlates(nextPiece, nextRow, nextColumn);
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
                MoveConnectedCylinder(cylinderPath, () => AddPlatesToCarryStack(pickupPieces));
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
                sequence.Append(_cylinder.transform.DOLocalJump(
                    targetPosition,
                    moveJumpPower,
                    1,
                    moveDurationPerCell).SetEase(Ease.OutQuad));
                sequence.AppendCallback(() =>
                {
                    if (!IsBoxOwnedCell(targetRow, targetColumn))
                    {
                        _cylinder.PlaceOnGrid(_grid, GetPiecesRoot(), targetRow, targetColumn);
                        ApplyStickmanWalkHeight();
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

            _isAnimating = true;
            int consumedCount = exitComponent.Consume(CarriedColor, _carriedPlates.Count);
            if (consumedCount <= 0)
            {
                _isAnimating = false;
                return;
            }

            CarryBlockJamCurtainBox.NotifyPlatesDeliveredToExit(CarriedColor, consumedCount);

            List<CarryBlockJamBoardPiece> plates = DetachCarriedPlates(consumedCount);
            RefreshStickmanAnimation(moving: false);

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < plates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = plates[i];
                if (plate == null)
                    continue;

                plate.transform.SetParent(GetPiecesRoot(), true);
                Vector3 targetPosition = exitComponent.transform.position + Vector3.up * (0.05f * i);
                sequence.Append(plate.transform.DOMove(targetPosition, exitTravelDuration).SetEase(Ease.InQuad));
                sequence.AppendCallback(() =>
                {
                    CarryBlockJamHiddenBox.NotifyPlateCollected(plate);
                    CarryBlockJamFrozenBox.NotifyPlateCollected(plate);
                    plate.gameObject.SetActive(false);
                    Destroy(plate.gameObject);
                });
            }

            sequence.OnComplete(() =>
            {
                UpdateCarriedPlateVisuals();
                _isAnimating = false;
                TryTriggerSuccess(plates);
            });
        }

        private void AnimateCarriedPlatesToBox(CarryBlockJamBoardPiece targetBox)
        {
            if (!HasCarriedPlates || targetBox == null)
            {
                _isAnimating = false;
                return;
            }

            _isAnimating = true;
            List<CarryBlockJamBoardPiece> plates = DetachCarriedPlates();
            RefreshStickmanAnimation(moving: false);
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

            sequence.OnComplete(() => _isAnimating = false);
        }

        private Vector3 GetPieceLocalPosition(CarryBlockJamBoardPiece piece, int row, int column)
        {
            Vector3 position = _grid.GetLocalPosition(row, column) + piece.GridOffset;
            if (piece == _cylinder && ShouldApplyStickmanWalkOffset)
                position += ResolveStickmanWalkOffset();
            return position;
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
                return;

            RuntimeAnimatorController controller = null;
            CarryBlockJamRuntimePieceSpawner spawner = GetComponent<CarryBlockJamRuntimePieceSpawner>();
            if (spawner != null)
                controller = spawner.StickmanAnimatorController;

            _stickmanAnimator = CarryBlockJamStickmanAnimator.EnsureOnCylinder(_cylinder.transform, controller);
            RefreshStickmanAnimation(moving: false);
        }

        private void RefreshStickmanAnimation(bool moving)
        {
            if (_stickmanAnimator == null)
                EnsureStickmanAnimator();

            _stickmanMoving = moving;
            _stickmanAnimator?.SetState(moving, HasCarriedPlates);
            ApplyStickmanWalkHeight();
        }

        // StandartWalk, CarryWalking, and CarryingIdle only — never the empty start idle.
        private bool ShouldApplyStickmanWalkOffset => _stickmanMoving || HasCarriedPlates;

        private Vector3 ResolveStickmanWalkOffset()
        {
            if (board != null && board.PrefabSettings != null)
                return board.PrefabSettings.stickmanCarryOffset;

            return stickmanCarryOffset;
        }

        private void ApplyStickmanWalkHeight()
        {
            if (_cylinder == null || _grid == null)
                return;

            if (_cylinder.Row < 0 || _cylinder.Column < 0)
                return;

            Transform visual = _cylinder.transform.Find("Visual");
            if (visual != null)
                visual.localPosition = Vector3.zero;

            _cylinder.transform.localPosition = GetPieceLocalPosition(_cylinder, _cylinder.Row, _cylinder.Column);
        }

        private void FaceStickmanToward(Vector3 localTarget)
        {
            if (_cylinder == null)
                return;

            Vector3 flatDelta = localTarget - _cylinder.transform.localPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < 0.0001f)
                return;

            Transform visual = _cylinder.transform.Find("Visual");
            Transform faceRoot = visual != null ? visual : _cylinder.transform;
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

            List<Vector2Int> previewPath = BuildPreviewPath(rowStep, columnStep, requestedSteps);
            DrawHighlights(previewPath);
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
                        if (CanPickUpPiece(piece) && !ShouldPickupFromPreviousCell(piece))
                            path.Add(new Vector2Int(nextRow, nextColumn));
                        break;
                    }

                    if (CanPickUpPiece(piece))
                    {
                        if (!ShouldPickupFromPreviousCell(piece))
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
                                break;
                            if (CanPickUpPiece(piece) && piece.Color == CarriedColor)
                            {
                                if (!ShouldPickupFromPreviousCell(piece))
                                    path.Add(new Vector2Int(nextRow, nextColumn));
                            }
                            break;
                        }

                        CarryBlockJamBoardPiece storageBox = GetStorageBox(piece);
                        if (storageBox != null && storageBox.Color == CarriedColor)
                            break;
                        if (CanPickUpPiece(piece) && piece.Color == CarriedColor)
                        {
                            if (!ShouldPickupFromPreviousCell(piece))
                                path.Add(new Vector2Int(nextRow, nextColumn));
                        }
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

            if (!TryGetNearestGridCell(screenPosition, out int endRow, out int endColumn))
                return false;

            int rowDelta = endRow - _swipeStartRow;
            int columnDelta = endColumn - _swipeStartColumn;
            if (rowDelta == 0 && columnDelta == 0)
                return false;

            if (Mathf.Abs(columnDelta) > Mathf.Abs(rowDelta))
            {
                columnStep = columnDelta > 0 ? 1 : -1;
                requestedSteps = Mathf.Abs(columnDelta);
            }
            else
            {
                rowStep = rowDelta > 0 ? 1 : -1;
                requestedSteps = Mathf.Abs(rowDelta);
            }

            return requestedSteps > 0;
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

        private bool CanPickUpPiece(CarryBlockJamBoardPiece piece)
        {
            return piece != null &&
                   piece.Kind == CarryBlockJamPieceKind.Plate &&
                   (!HasCarriedPlates || piece.Color == CarriedColor);
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

        private void AddPlatesToCarryStack(List<CarryBlockJamBoardPiece> plates)
        {
            if (plates == null || plates.Count == 0)
                return;

            for (int i = 0; i < plates.Count; i++)
            {
                AddPlateToCarryStack(plates[i]);
                CarryBlockJamHiddenBox.NotifyPlateCollected(plates[i]);
                CarryBlockJamFrozenBox.NotifyPlateCollected(plates[i]);
            }

            RefreshStickmanAnimation(moving: false);
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

            Transform visual = _cylinder.transform.Find("Visual");
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
            LevelManager.instance.Success();
        }
    }
}
