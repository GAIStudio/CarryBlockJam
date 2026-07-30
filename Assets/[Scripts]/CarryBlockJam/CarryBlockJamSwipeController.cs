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
        [SerializeField] private float moveDurationPerCell = 0.1f;
        [SerializeField] private float exitTravelDuration = 0.18f;
        [SerializeField] private float exitPlateDeliveryDuration = 0.07f;
        [SerializeField] private float gatePlateFlyDuration = 0.2f;
        [SerializeField] private float gatePlateFlyHeight = 0.65f;
        [SerializeField] private float tablePlateJumpDuration = 0.3f;
        [SerializeField] private float tablePlateJumpHeight = 0.65f;
        [SerializeField] private float tablePlateSettleDuration = 0.16f;
        [SerializeField] private float tablePlateAnimationSpeed = 1.4f;
        [Tooltip("After delivering plates to a table, block picking them back up for this long so a fast second drag does not vacuum them immediately.")]
        [SerializeField] private float tableDropCollectCooldown = 0.55f;
        [SerializeField] private float pickupPlateBounceDuration = 0.18f;
        [SerializeField] private float pickupPlateBounceHeight = 0.55f;
        [SerializeField] private float pickupPlateStagger = 0.025f;
        [SerializeField] private float pickupPlateSettleDuration = 0.08f;
        [SerializeField] private float pickupPlateSettleScale = 0.16f;
        [SerializeField] private float pickupPlateFrontClearance = 0.45f;
        [SerializeField] private float pickupPlateAnimationSpeed = 2.8f;
        [SerializeField] private float dragCornerTransitionDuration = 0f;
        [SerializeField] private float dragTurnThresholdCells = 0.22f;
        [SerializeField] private float dragTurnProbeResetCells = 0.14f;
        [SerializeField] private float dragTurnDominance = 1.05f;
        [SerializeField] private float dragCellEngageThreshold = 0.08f;
        [SerializeField] private float dragCellCommitThreshold = 0.55f;
        [Tooltip("How far into the next cell CharTable must be before that cell is claimed on settle/turns. Higher = less border-sensitive.")]
        [SerializeField] private float dragCellCrossThreshold = 0.65f;
        [SerializeField, Min(0.2f)] private float dragFollowGain = 0.92f;
        [SerializeField, Min(1f)] private float dragFollowSpeed = 16f;
        [SerializeField] private float charTablePickupDuration = 0.42f;
        [SerializeField] private float charTablePickupOutsideDistance = 0.5f;
        [SerializeField] private float charTablePickupLift = 0.18f;
        [Tooltip("How small freestanding plates get at the soar apex (scale down then back up).")]
        [SerializeField] private float charTablePickupShrinkScale = 0.55f;
        [Tooltip("Peak height of the curvy jump when collecting freestanding ground plates.")]
        [SerializeField] private float charTablePickupArcHeight = 1.7f;
        [Tooltip("How far the arc peak stays away from CharTable for ground-plate pickup.")]
        [SerializeField] private float charTablePickupArcOutward = 1.05f;
        [Tooltip("Edge-on flip degrees at the soar apex (flat at start/end, vertical mid-flight).")]
        [SerializeField] private float charTablePickupFlipDegrees = 82f;
        [Tooltip("Duration when picking plates off a normal table onto CharTable.")]
        [SerializeField] private float charTableTablePickupDuration = 0.22f;
        [Tooltip("Arc height multiplier when dropping plates from CharTable onto a normal table.")]
        [SerializeField] private float charTableDropArcHeightMul = 1.85f;
        [Tooltip("Flight duration when dropping plates from CharTable onto a normal table.")]
        [SerializeField] private float charTableDropDuration = 0.07f;
        [SerializeField] private float charTablePickupShrinkDuration = 0.06f;
        [Tooltip("Launch gap between soaring plates so several stay visible in the air.")]
        [SerializeField] private float charTablePickupStagger = 0.085f;
        [SerializeField] private float highlightHeight = 0.35f;
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.84f, 1f, 0.9f);
        [SerializeField] private Vector3 carriedPlateBaseOffset = new Vector3(0f, 0.9f, 0.40f);
        [SerializeField] private Vector3 charTablePlateBaseOffset = new Vector3(0f, 0.7f, -0.08f);
        [SerializeField] private float carriedPlateStackStep = 0.18f;
        [SerializeField] private Vector3 stickmanCarryOffset = new Vector3(0f, -0.7f, 0f);
        [SerializeField] private float failurePlateDropDuration = 0.35f;
        [SerializeField] private float failurePlateFlyHeight = 1.25f;
        [SerializeField] private float failurePlateSpreadStagger = 0.04f;
        [SerializeField] private float failureUiDelay = 1.4f;
        [Header("CharTable Wind Trail")]
        [SerializeField] private bool enableCharTableTrail = true;
        [Tooltip("Defaults to Epic Toon FX SoapBubbleEmitter when left empty.")]
        [SerializeField] private GameObject charTableTrailVfxPrefab;
        [SerializeField] private float charTableTrailVfxScale = 1.15f;
        [SerializeField] private float charTableTrailMinEmission = 7f;
        [SerializeField] private float charTableTrailMaxEmission = 24f;
        [SerializeField] private float charTableTrailRateOverDistance = 8f;
        [SerializeField] private float charTableTrailMaxRateOverDistance = 16f;
        [SerializeField] private float charTableTrailExhaustSpeed = 2.2f;
        [SerializeField] private float charTableTrailMaxExhaustSpeed = 3.4f;
        [SerializeField] private float charTableTrailExhaustSize = 0.62f;
        [SerializeField] private float charTableTrailMaxExhaustSize = 0.85f;
        [SerializeField] private float charTableTrailMinLifetime = 0.16f;
        [SerializeField] private float charTableTrailMaxLifetime = 0.5f;
        [SerializeField] private float charTableTrailCellsForMaxHeavy = 8f;
        [SerializeField] private float charTableTrailHeight = 0.18f;
        [SerializeField] private float charTableTrailRearOffset = 0.55f;
        [SerializeField] private float charTableTrailAbsorbDuration = 0.12f;
        [Header("CharTable Idle Hints")]
        [SerializeField] private bool enableCharTableIdleHints = true;
        [SerializeField] private float charTableIdleShakeDelay = 3f;
        [SerializeField] private float charTableIdleShakeDuration = 0.45f;
        [SerializeField] private float charTableIdleShakeStrength = 0.055f;
        [SerializeField] private float charTableStartHintDuration = 1.4f;
        [Header("CarryBlockJam SFX (SoundManager names)")]
        [SerializeField] private string plateCollectSound = "SFX_UI_Fillup_Block_Box_1";
        [SerializeField] private string plateDeliverSound = "SFX_UI_Fillup_Block_AquaBright_3";

        private Camera _gameplayCamera;
        private PuzzleGrid _grid;
        private Vector2 _swipeStartScreen;
        private int _swipeStartRow;
        private int _swipeStartColumn;
        private Vector3 _dragStartBoardLocalPoint;
        private int _lockedDragAxis;
        private int _dragActiveAxis;
        private Vector3 _dragSegmentBoardLocalPoint;
        private Vector2Int _dragFollowCell;
        private Vector3 _dragTurnProbeBoardLocalPoint;
        private readonly List<Vector2Int> _dragRouteCorners =
            new List<Vector2Int>();
        private bool _dragCornerTransitionActive;
        private float _dragCornerTransitionElapsed;
        private Vector3 _dragCornerTransitionStart;
        private Vector3 _dragCornerTransitionTarget;
        private bool _trackingSwipe;
        private int _activeFingerId = -1;
        private bool _commitDraggedMovementInstantly;
        private bool _isAnimating;
        private bool _successTriggered;
        private bool _failTriggered;
        private bool _failurePreparing;
        private int _failureOriginRow = -1;
        private int _failureOriginColumn = -1;
        private CarryBlockJamBoardPiece _cylinder;
        private CarryBlockJamStickmanAnimator _stickmanAnimator;
        private bool _stickmanMoving;
        private readonly List<CarryBlockJamBoardPiece> _carriedPlates = new List<CarryBlockJamBoardPiece>();
        private readonly Dictionary<Vector2Int, float> _tableCollectCooldownUntil =
            new Dictionary<Vector2Int, float>();
        private PieceColorType _dragCollectColor = PieceColorType.None;
        private bool _swipeStartedWithCarriedPlates;
        /// <summary>
        /// Table cell we picked up from during this swipe — blocks putting those
        /// plates straight back onto the same table in the same gesture.
        /// </summary>
        private bool _hasTablePickupBlockDeliver;
        private Vector2Int _tablePickupBlockDeliverCell;
        private ParticleSystem _charTableTrailParticles;
        private float _trailSessionCells;
        private float _trailSegmentProgressReported;
        private Tween _charTableTrailAbsorbTween;
        private Vector3 _charTableTrailLastWorldPos;
        private bool _charTableTrailHasLastPos;
        private Vector3 _charTableTrailExhaustDir = Vector3.back;
        private int _charTableTrailConfigVersion;
        private const int CharTableTrailExhaustConfigVersion = 15;
        private static GameObject _cachedCharTableTrailVfxPrefab;
        private const string CharTableTrailVfxResourcePath = "particles/SoapBubbleEmitter";
        private const string CharTableTrailVfxEditorPath =
            "Assets/Packages/Particles/Epic Toon FX/Prefabs/Environment/Bubbles/SoapBubbleEmitter.prefab";
        private Tween _charTableIdleShakeTween;
        private ParticleSystem _charTableHintParticles;
        private bool _charTableStartHintPlayed;
        private float _lastPlayerInputTime = -1f;
        private Vector3 _charTableVisualRestLocalPosition;
        private bool _charTableVisualRestCaptured;
        private readonly List<Tween> _plateCollectionTweens = new List<Tween>();
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
            UpdateCharTableIdleHints();
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

        private bool TryGetTouchById(int fingerId, out Touch touch)
        {
            touch = default;
            int count = Input.touchCount;
            for (int i = 0; i < count; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId == fingerId)
                {
                    touch = t;
                    return true;
                }
            }
            return false;
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0)
            {
                _activeFingerId = -1;
                return;
            }

            if (_trackingSwipe && _activeFingerId >= 0)
            {
                if (TryGetTouchById(_activeFingerId, out Touch activeTouch))
                {
                    if (activeTouch.phase == TouchPhase.Ended)
                    {
                        FinishSwipe(activeTouch.position);
                        _activeFingerId = -1;
                    }
                    else if (activeTouch.phase == TouchPhase.Canceled)
                    {
                        CancelSwipe();
                        _activeFingerId = -1;
                    }
                }
                else
                {
                    CancelSwipe();
                    _activeFingerId = -1;
                }
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    TryStartSwipe(touch.position);
                    if (_trackingSwipe)
                    {
                        _activeFingerId = touch.fingerId;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// True once success/fail is reserved — CharTable must not be teleported for tutorial snaps.
        /// </summary>
        public bool IsEndLocked => _successTriggered || _failTriggered;

        public void PlaceStickmanAtCell(int row, int column, bool force = false)
        {
            ResolveGameplayReferences();
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return;
            // Keep CharTable where it is when end panels open (no snap back to stage/start).
            if (_successTriggered || _failTriggered)
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
            if (UsesCharTableVisual())
            {
                _charTableVisualRestCaptured = false;
                CaptureCharTableVisualRestPose();
            }
        }

        private void TryStartSwipe(Vector2 screenPosition)
        {
            if (_isAnimating || _failTriggered)
                return;

            if (_cylinder == null)
                ResolveGameplayReferences();

            if (_cylinder == null)
                return;

            // Movement starts only when the player selects the character/table visual.
            // Runtime FBX colliders are removed, so selection uses renderer bounds.
            if (!IsPointerOverCylinder(screenPosition))
                return;

            // Text-only tutorial tip: hide instruction on first press.
            TutorialManager.Instance?.NotifyPlayerInteracted();
            NotifyCharTablePlayerInput();

            _swipeStartScreen = screenPosition;
            _swipeStartRow = _cylinder.Row;
            _swipeStartColumn = _cylinder.Column;
            TryProjectPointerToBoardLocal(screenPosition, out _dragStartBoardLocalPoint);
            _lockedDragAxis = 0;
            _dragActiveAxis = 0;
            _dragSegmentBoardLocalPoint = _dragStartBoardLocalPoint;
            _dragTurnProbeBoardLocalPoint = _dragStartBoardLocalPoint;
            _dragRouteCorners.Clear();
            _dragRouteCorners.Add(
                new Vector2Int(_swipeStartRow, _swipeStartColumn));
            _dragFollowCell = new Vector2Int(_swipeStartRow, _swipeStartColumn);
            _dragCornerTransitionActive = false;
            _dragCornerTransitionElapsed = 0f;
            _dragCollectColor = HasCarriedPlates ? CarriedColor : PieceColorType.None;
            _swipeStartedWithCarriedPlates = HasCarriedPlates;
            _hasTablePickupBlockDeliver = false;
            _trailSessionCells = 0f;
            _trailSegmentProgressReported = 0f;
            _trackingSwipe = true;
            EnsureCharTableTrail();
            KillCharTableTrailAbsorbTween();
            _charTableTrailHasLastPos = false;
            if (_cylinder != null)
            {
                _charTableTrailLastWorldPos = _cylinder.transform.position;
                _charTableTrailHasLastPos = true;
            }
            SetCharTableTrailEmitting(false);
            RefreshCharTableTrailHeaviness();
            EnsureHighlightRoot();
            ShowHighlights(false);
            RefreshStickmanAnimation(moving: false);
        }

        private void FinishSwipe(Vector2 screenPosition)
        {
            _trackingSwipe = false;
            _activeFingerId = -1;
            ShowHighlights(false);

            ResolveGameplayReferences();
            if (_cylinder == null)
                return;

            if (_failTriggered)
            {
                SnapCylinderToLogicalCell();
                return;
            }

            bool tutorialPathLocked =
                TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                TutorialManager.Instance.IsStagePathLocked;
            if (!tutorialPathLocked)
            {
                FinishOrthogonalDrag(screenPosition);
                return;
            }

            bool hasIntent = TryGetSwipeIntent(
                screenPosition,
                out int rowStep,
                out int columnStep,
                out int requestedSteps);
            _lockedDragAxis = 0;
            if (!hasIntent)
            {
                SnapCylinderToLogicalCell();
                RefreshStickmanAnimation(moving: false);
                return;
            }

            _commitDraggedMovementInstantly = true;
            if (tutorialPathLocked)
            {
                if (!TutorialManager.Instance.TryEngageStagePathLock(
                        _cylinder.Row,
                        _cylinder.Column,
                        rowStep,
                        columnStep))
                {
                    CompleteUnconsumedDirectDrag();
                    RefreshStickmanAnimation(moving: false);
                    return;
                }

                if (!TutorialManager.Instance.TryClampSwipeToAuthoredPath(
                        _cylinder.Row,
                        _cylinder.Column,
                        ref rowStep,
                        ref columnStep,
                        ref requestedSteps))
                {
                    CompleteUnconsumedDirectDrag();
                    RefreshStickmanAnimation(moving: false);
                    return;
                }

                TryExecuteTutorialTravelSwipe(requestedSteps);
                CompleteUnconsumedDirectDrag();
                return;
            }

            if (HasCarriedPlates)
                ExecuteCarrySwipe(rowStep, columnStep, requestedSteps);
            else
                ExecuteTravelSwipe(rowStep, columnStep, requestedSteps);

            CompleteUnconsumedDirectDrag();
        }

        private void FinishOrthogonalDrag(Vector2 screenPosition)
        {
            if (_dragActiveAxis == 0)
            {
                if (!TryGetSwipeIntent(
                        screenPosition,
                        out _,
                        out _,
                        out _))
                {
                    ResetOrthogonalDrag();
                    SnapCylinderToLogicalCell();
                    RefreshStickmanAnimation(moving: false);
                    return;
                }

                _dragActiveAxis = _lockedDragAxis;
            }

            // Never travel on release — that was teleporting CharTable toward a
            // clamped finger/edge cell after circular flicks. Settle where the
            // visual already is; deliveries happen in place.
            SettleCharTableAtDragEnd(screenPosition);

            TryInteractMatchingTableOnRelease(screenPosition);
            TryDeliverCarriedPlatesIfPressingExit(screenPosition);

            SnapCylinderToLogicalCell();

            ResetOrthogonalDrag();
            CompleteUnconsumedDirectDrag();
            AbsorbCharTableTrail();
            RefreshStickmanAnimation(moving: false);
            if (HasCarriedPlates)
                EvaluateCarriedPlateDeadlock();
        }

        /// <summary>
        /// Pin logical + visual CharTable to the hysteresis follow cell — never
        /// Round() mid-border positions (that caused side-grid jumps on release).
        /// Matching exit cells are an exception: settle onto the gate front cell
        /// when CharTable is already visually there so delivery is not missed.
        /// </summary>
        private void SettleCharTableAtDragEnd(Vector2 screenPosition)
        {
            UpdateDragFollowCellFromVisual();

            Vector2Int cell = _dragFollowCell;
            if (TryResolveVisualMatchingExitCell(out Vector2Int exitCell))
            {
                cell = exitCell;
                _dragFollowCell = exitCell;
            }
            else if (!IsValidCharTableSettleCell(cell.x, cell.y))
            {
                if (!TryResolveSettleCellAtDragEnd(out cell))
                {
                    SnapCylinderToLogicalCell();
                    return;
                }
            }

            // Catch any freestanding plate CharTable ended on.
            TryCollectPlatesUnderCharTable();

            PlaceStickmanOnCell(cell.x, cell.y);
            SnapCylinderToLogicalCell();
        }

        /// <summary>
        /// Collect freestanding plates only from the cell CharTable is currently
        /// on (visual nearest + follow cell). Enables quick multi-pickup while
        /// dragging through several plate cells — never vacuums neighbors.
        /// </summary>
        private void TryCollectPlatesUnderCharTable()
        {
            if (_cylinder == null || _grid == null)
                return;

            if (TryGetDragVisualCellContinuous(
                    out float rowContinuous,
                    out float columnContinuous))
            {
                int visualRow = Mathf.Clamp(
                    Mathf.RoundToInt(rowContinuous),
                    0,
                    _grid.Rows - 1);
                int visualColumn = Mathf.Clamp(
                    Mathf.RoundToInt(columnContinuous),
                    0,
                    _grid.Columns - 1);
                if (!IsBoxOwnedCell(visualRow, visualColumn))
                    TryCollectAtCellDuringDrag(visualRow, visualColumn);
            }

            UpdateDragFollowCellFromVisual();
            if (!IsBoxOwnedCell(_dragFollowCell.x, _dragFollowCell.y))
                TryCollectAtCellDuringDrag(_dragFollowCell.x, _dragFollowCell.y);
        }

        /// <summary>
        /// True when CharTable's visual pose is clearly on a matching exit cell.
        /// </summary>
        private bool TryResolveVisualMatchingExitCell(out Vector2Int exitCell)
        {
            exitCell = default;
            if (!HasCarriedPlates ||
                !TryGetDragVisualCellContinuous(out float rowContinuous, out float columnContinuous))
                return false;

            int row = Mathf.Clamp(
                Mathf.RoundToInt(rowContinuous),
                0,
                _grid.Rows - 1);
            int column = Mathf.Clamp(
                Mathf.RoundToInt(columnContinuous),
                0,
                _grid.Columns - 1);

            // Must be near the cell center — not a borderline Round() into the gate.
            if (Mathf.Abs(rowContinuous - row) > 0.5f ||
                Mathf.Abs(columnContinuous - column) > 0.5f)
                return false;

            if (IsBoxOwnedCell(row, column))
                return false;

            if (!TryResolveExitAtCell(row, column, out CarryBlockJamExit exit) ||
                exit == null)
                return false;

            exitCell = new Vector2Int(row, column);
            return true;
        }

        private bool TryResolveSettleCellAtDragEnd(out Vector2Int cell)
        {
            cell = default;
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return false;

            if (_dragRouteCorners.Count > 0)
            {
                Vector2Int corner = _dragRouteCorners[_dragRouteCorners.Count - 1];
                if (IsValidCharTableSettleCell(corner.x, corner.y))
                {
                    cell = corner;
                    return true;
                }
            }

            if (IsValidCharTableSettleCell(_cylinder.Row, _cylinder.Column))
            {
                cell = new Vector2Int(_cylinder.Row, _cylinder.Column);
                return true;
            }

            return false;
        }

        private bool IsValidCharTableSettleCell(int row, int column)
        {
            return _grid != null &&
                   _grid.IsInside(row, column) &&
                   !IsBoxOwnedCell(row, column) &&
                   // Different-color / blocked plates are never valid settle cells.
                   !IsDragPathBlocker(row, column, GetRequiredCollectColor());
        }

        /// <summary>
        /// Cap path length to the finger's continuous axis progress — never to
        /// GetNearestGridCell, which clamps off-board touches onto edge cells.
        /// </summary>
        private int ClampDragStepsTowardFinger(
            Vector2 screenPosition,
            int fromRow,
            int fromColumn,
            int rowStep,
            int columnStep,
            int requestedSteps,
            bool allowOffBoardPress)
        {
            if (requestedSteps <= 0 || (rowStep == 0 && columnStep == 0))
                return 0;

            float along = MeasureFingerAlongAxis(
                screenPosition,
                fromRow,
                fromColumn,
                rowStep,
                columnStep);

            if (along > 0.05f)
            {
                int cap = Mathf.Max(1, Mathf.CeilToInt(along));
                return Mathf.Min(requestedSteps, cap);
            }

            if (allowOffBoardPress &&
                IsPressingOffBoardEdge(
                    screenPosition,
                    fromRow,
                    fromColumn,
                    rowStep,
                    columnStep))
                return 1;

            return 0;
        }

        private float MeasureFingerAlongAxis(
            Vector2 screenPosition,
            int fromRow,
            int fromColumn,
            int rowStep,
            int columnStep)
        {
            if ((rowStep == 0 && columnStep == 0) ||
                !TryProjectPointerToBoardLocal(
                    screenPosition,
                    out Vector3 pointerLocal) ||
                _grid == null)
                return 0f;

            Vector3 fromLocal = _grid.GetLocalPosition(fromRow, fromColumn);
            Vector3 delta = pointerLocal - fromLocal;
            return rowStep != 0
                ? (-delta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ)) * rowStep
                : (delta.x / Mathf.Max(0.0001f, _grid.GridSpacingX)) * columnStep;
        }

        private bool IsPressingOffBoardEdge(
            Vector2 screenPosition,
            int fromRow,
            int fromColumn,
            int rowStep,
            int columnStep)
        {
            int nextRow = fromRow + rowStep;
            int nextColumn = fromColumn + columnStep;
            if (_grid != null && _grid.IsInside(nextRow, nextColumn))
                return false;

            return MeasureFingerAlongAxis(
                       screenPosition,
                       fromRow,
                       fromColumn,
                       rowStep,
                       columnStep) >=
                   0.35f;
        }

        /// <summary>
        /// Gate delivery when CharTable is on the gate's front cell. Arriving on
        /// that cell is enough — no extra finger-past-rim check (that caused
        /// missed deliveries until leaving and returning).
        /// </summary>
        private bool TryDeliverCarriedPlatesIfPressingExit(Vector2 screenPosition)
        {
            if (!HasCarriedPlates || _cylinder == null || _grid == null)
                return false;

            if (TryResolveExitAtCell(
                    _cylinder.Row,
                    _cylinder.Column,
                    out CarryBlockJamExit exit) &&
                exit != null)
            {
                SendCarriedPlatesToExit(exit, null);
                return true;
            }

            // Fallback: visual/follow claimed the gate but settle lagged one cell.
            if (TryResolveVisualMatchingExitCell(out Vector2Int exitCell) &&
                TryResolveExitAtCell(
                    exitCell.x,
                    exitCell.y,
                    out exit) &&
                exit != null)
            {
                PlaceStickmanOnCell(exitCell.x, exitCell.y);
                SendCarriedPlatesToExit(exit, null);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Mid-drag gate magnet: when CharTable is carrying matching plates and
        /// reaches a gate front cell, stop the drag immediately and deliver —
        /// no finger release required. Does not change movement math; only
        /// reacts after CharTable has already arrived on that cell.
        /// </summary>
        private bool TryMagnetDeliverAtGateDuringDrag()
        {
            if (!_trackingSwipe ||
                _isAnimating ||
                !HasCarriedPlates ||
                _cylinder == null ||
                _grid == null)
                return false;

            int gateRow;
            int gateColumn;
            CarryBlockJamExit exit;

            if (TryResolveVisualMatchingExitCell(out Vector2Int exitCell) &&
                TryResolveExitAtCell(exitCell.x, exitCell.y, out exit) &&
                exit != null)
            {
                gateRow = exitCell.x;
                gateColumn = exitCell.y;
            }
            else
            {
                UpdateDragFollowCellFromVisual();
                gateRow = _dragFollowCell.x;
                gateColumn = _dragFollowCell.y;
                if (!TryResolveExitAtCell(gateRow, gateColumn, out exit) ||
                    exit == null)
                    return false;
            }

            if (!exit.CanAccept(CarriedColor) ||
                exit.GetAcceptablePlateCount(CarriedColor) <= 0)
                return false;

            // Stick to the gate cell and end the active drag — magnet catch.
            PlaceStickmanOnCell(gateRow, gateColumn);
            SnapCylinderToLogicalCell();
            _trackingSwipe = false;
            ShowHighlights(false);
            ResetOrthogonalDrag();
            CompleteUnconsumedDirectDrag();
            AbsorbCharTableTrail();
            RefreshStickmanAnimation(moving: false);
            SendCarriedPlatesToExit(exit, null);
            return true;
        }

        /// <summary>
        /// Matching tables: pick up plates when CharTable is on the approach cell
        /// and the finger is on/into the table; deliver carried plates the same way.
        /// Gate delivery stays separate (front exit cell only).
        /// </summary>
        private bool TryInteractMatchingTableOnRelease(Vector2 screenPosition)
        {
            if (_cylinder == null || _grid == null)
                return false;

            if (!TryResolveAdjacentTableForRelease(
                    screenPosition,
                    out int tableRow,
                    out int tableColumn,
                    out CarryBlockJamBoardPiece tableBox))
                return false;

            // Empty CharTable: pickup from this table only (never pickup+deliver).
            if (!HasCarriedPlates)
            {
                PieceColorType collectColor = GetRequiredCollectColor();
                if (HasCollectiblePlateAt(tableRow, tableColumn, collectColor) ||
                    (collectColor == PieceColorType.None &&
                     HasCollectiblePlateAt(tableRow, tableColumn, PieceColorType.None)))
                {
                    TryCollectAtCellDuringDrag(tableRow, tableColumn);
                    return true;
                }

                return false;
            }

            // Already carrying (started loaded, or collected freestanding mid-drag).
            // Only block if these plates were just taken from THIS table.
            if (IsTableDeliverBlockedByRecentPickup(tableRow, tableColumn))
                return false;

            if (!CanDeliverCarriedPlatesToCell(
                    tableRow,
                    tableColumn,
                    _cylinder.Row,
                    _cylinder.Column,
                    pathCellsTraveled: 0))
                return false;

            if (tableBox == null &&
                !TryResolveDropTargetAtCell(tableRow, tableColumn, out tableBox))
                return false;

            if (tableBox == null || !CanDeliverToTable(tableBox))
                return false;

            AnimateCarriedPlatesToBox(tableBox);
            return true;
        }

        /// <summary>
        /// Resolve a matching/interactable table that CharTable is beside, with
        /// the finger on that table cell or pressing into it.
        /// </summary>
        private bool TryResolveAdjacentTableForRelease(
            Vector2 screenPosition,
            out int tableRow,
            out int tableColumn,
            out CarryBlockJamBoardPiece tableBox)
        {
            tableRow = -1;
            tableColumn = -1;
            tableBox = null;

            int fromRow = _cylinder.Row;
            int fromColumn = _cylinder.Column;

            // Finger on a table cell next to CharTable.
            if (TryGetNearestGridCell(
                    screenPosition,
                    out int fingerRow,
                    out int fingerColumn) &&
                IsOrthogonallyAdjacent(
                    fromRow,
                    fromColumn,
                    fingerRow,
                    fingerColumn) &&
                IsBoxOwnedCell(fingerRow, fingerColumn))
            {
                tableRow = fingerRow;
                tableColumn = fingerColumn;
                TryResolveDropTargetAtCell(fingerRow, fingerColumn, out tableBox);
                if (tableBox == null)
                    tableBox = FindBoxAtCell(fingerRow, fingerColumn);
                return tableBox != null ||
                       HasCollectiblePlateAt(
                           fingerRow,
                           fingerColumn,
                           GetRequiredCollectColor());
            }

            // Finger pressing into the neighbor cell that is a table.
            if (!TryProjectPointerToBoardLocal(
                    screenPosition,
                    out Vector3 pointerLocal))
                return false;

            Vector3 fromLocal = _grid.GetLocalPosition(fromRow, fromColumn);
            Vector3 delta = pointerLocal - fromLocal;
            float rowCells =
                -delta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ);
            float columnCells =
                delta.x / Mathf.Max(0.0001f, _grid.GridSpacingX);

            int rowStep = 0;
            int columnStep = 0;
            float along;
            if (Mathf.Abs(rowCells) >= Mathf.Abs(columnCells))
            {
                if (Mathf.Abs(rowCells) < 0.35f)
                    return false;
                rowStep = rowCells > 0f ? 1 : -1;
                along = Mathf.Abs(rowCells);
            }
            else
            {
                if (Mathf.Abs(columnCells) < 0.35f)
                    return false;
                columnStep = columnCells > 0f ? 1 : -1;
                along = Mathf.Abs(columnCells);
            }

            if (along < 0.45f)
                return false;

            tableRow = fromRow + rowStep;
            tableColumn = fromColumn + columnStep;
            if (!IsBoxOwnedCell(tableRow, tableColumn))
                return false;

            TryResolveDropTargetAtCell(tableRow, tableColumn, out tableBox);
            if (tableBox == null)
                tableBox = FindBoxAtCell(tableRow, tableColumn);
            return tableBox != null ||
                   HasCollectiblePlateAt(
                       tableRow,
                       tableColumn,
                       GetRequiredCollectColor());
        }

        private static bool IsOrthogonallyAdjacent(
            int fromRow,
            int fromColumn,
            int toRow,
            int toColumn)
        {
            return Mathf.Abs(fromRow - toRow) + Mathf.Abs(fromColumn - toColumn) == 1;
        }

        private bool CanDeliverCarriedPlatesToCell(
            int tableRow,
            int tableColumn,
            int currentRow,
            int currentColumn,
            int pathCellsTraveled)
        {
            if (!HasCarriedPlates)
                return false;

            // Don't put plates back on the same table we just emptied this swipe.
            if (IsTableDeliverBlockedByRecentPickup(tableRow, tableColumn))
                return false;

            if (_swipeStartedWithCarriedPlates)
                return true;

            // Collected freestanding plates earlier in this drag — allow drop after travel.
            return pathCellsTraveled > 0 ||
                   currentRow != _swipeStartRow ||
                   currentColumn != _swipeStartColumn ||
                   HasLeftSwipeStartDuringDrag();
        }

        private bool HasLeftSwipeStartDuringDrag()
        {
            if (_dragRouteCorners.Count > 1)
                return true;

            if (_dragFollowCell.x != _swipeStartRow ||
                _dragFollowCell.y != _swipeStartColumn)
                return true;

            if (TryResolveDragVisualCell(out Vector2Int visualCell) &&
                (visualCell.x != _swipeStartRow ||
                 visualCell.y != _swipeStartColumn))
                return true;

            return false;
        }

        private void ResetOrthogonalDrag()
        {
            _lockedDragAxis = 0;
            _dragActiveAxis = 0;
            _dragRouteCorners.Clear();
            _dragCornerTransitionActive = false;
            _dragCornerTransitionElapsed = 0f;
            if (!HasCarriedPlates)
                _dragCollectColor = PieceColorType.None;
        }

        private void ExecuteDirectDragSegment(
            int rowStep,
            int columnStep,
            int requestedSteps)
        {
            if (requestedSteps <= 0)
                return;

            _commitDraggedMovementInstantly = true;
            if (HasCarriedPlates)
                ExecuteCarrySwipe(rowStep, columnStep, requestedSteps);
            else
                ExecuteTravelSwipe(rowStep, columnStep, requestedSteps);
            CompleteUnconsumedDirectDrag();
        }

        private void CancelSwipe()
        {
            _trackingSwipe = false;
            _activeFingerId = -1;
            ResetOrthogonalDrag();
            if (!HasCarriedPlates)
                _dragCollectColor = PieceColorType.None;
            ShowHighlights(false);
            AbsorbCharTableTrail();
            SnapCylinderToLogicalCell();
            RefreshStickmanAnimation(moving: false);
            NotifyCharTablePlayerInput();
        }

        private void CompleteUnconsumedDirectDrag()
        {
            if (!_commitDraggedMovementInstantly)
                return;

            _commitDraggedMovementInstantly = false;
            SnapCylinderToLogicalCell();
        }

        private void SnapCylinderToLogicalCell(bool immediate = false)
        {
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return;

            if (!_grid.IsInside(_cylinder.Row, _cylinder.Column))
                return;

            Vector3 targetPosition = GetPieceLocalPosition(_cylinder, _cylinder.Row, _cylinder.Column);
            if (immediate)
            {
                _cylinder.transform.DOKill();
                _cylinder.transform.localPosition = targetPosition;
            }
            else
            {
                float dist = Vector3.Distance(_cylinder.transform.localPosition, targetPosition);
                if (dist > 0.01f)
                {
                    _cylinder.transform.DOKill();
                    _cylinder.transform.DOLocalMove(targetPosition, 0.08f).SetEase(Ease.OutQuad);
                }
                else
                {
                    _cylinder.transform.localPosition = targetPosition;
                }
            }
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
                TryCollectPlatesFromOccupant(
                    occupant,
                    step.x,
                    step.y,
                    pickupPieces);
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
                {
                    bool fromTable = false;
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (IsBoxOwnedCell(path[i].x, path[i].y))
                        {
                            fromTable = true;
                            break;
                        }
                    }

                    AddPlatesToCarryStack(
                        pickupPieces,
                        notifyTutorialComplete: false,
                        fromTable: fromTable);
                }

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
                        AddPlatesToCarryStack(
                            leftover,
                            notifyTutorialComplete: false,
                            fromTable: IsBoxOwnedCell(target.x, target.y));
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
                if (piece.Kind == CarryBlockJamPieceKind.Box && CanDeliverToTable(piece))
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
            if (TryCompleteDraggedMovementInstantly(
                    path,
                    allowBoxOwnedDestination: true,
                    onComplete: onComplete))
                return;

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

            // Never claim a table cell — logical or visual — so stacked plates stay findable.
            if (IsBoxOwnedCell(row, column))
                return;

            ClearStickmanOccupantFromCell(_cylinder.Row, _cylinder.Column);
            _cylinder.PlaceOnGrid(_grid, GetPiecesRoot(), row, column);

            // Never overwrite a freestanding plate Occupant. Fast settle used to
            // claim the cell, hide the plate from Occupant-based path checks, and
            // let CharTable travel through that plate on the next move.
            if (FindFreestandingPlateAtCell(row, column) != null)
                return;

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
            {
                cell.Occupant = null;
                RestoreStackOccupantAtCell(row, column);
            }
        }

        private void RestoreStackOccupantAtCell(int row, int column)
        {
            if (_grid == null ||
                !_grid.TryGetCell(row, column, out PuzzleCell cell) ||
                cell == null ||
                cell.Occupant != null)
                return;

            CarryBlockJamBoardPiece plate = FindFreestandingPlateAtCell(row, column);
            if (plate != null)
            {
                CarryBlockJamBoardPiece plateTop = GetTopStackPiece(plate);
                cell.Occupant = plateTop != null ? plateTop.gameObject : plate.gameObject;
                return;
            }

            CarryBlockJamBoardPiece box = FindBoxAtCell(row, column);
            if (box == null)
                return;

            CarryBlockJamBoardPiece top = GetTopStackPiece(box);
            cell.Occupant = top != null ? top.gameObject : box.gameObject;
        }

        private CarryBlockJamBoardPiece FindBoxAtCell(int row, int column)
        {
            CarryBlockJamBoardPiece[] pieces = GetComponentsInChildren<CarryBlockJamBoardPiece>(true);
            for (int i = 0; i < pieces.Length; i++)
            {
                CarryBlockJamBoardPiece piece = pieces[i];
                if (piece == null || piece.Kind != CarryBlockJamPieceKind.Box)
                    continue;

                if (piece.Row == row && piece.Column == column)
                    return piece;
            }

            return null;
        }

        /// <summary>
        /// Freestanding plate still registered on this cell by Row/Column — even if
        /// Occupant was overwritten by CharTable. Skips plates already in the carry
        /// stack (including mid-soar pickups).
        /// </summary>
        private CarryBlockJamBoardPiece FindFreestandingPlateAtCell(int row, int column)
        {
            if (_grid == null || !_grid.IsInside(row, column) || IsBoxOwnedCell(row, column))
                return null;

            CarryBlockJamBoardPiece[] pieces = GetComponentsInChildren<CarryBlockJamBoardPiece>(true);
            CarryBlockJamBoardPiece found = null;
            for (int i = 0; i < pieces.Length; i++)
            {
                CarryBlockJamBoardPiece piece = pieces[i];
                if (piece == null || piece.Kind != CarryBlockJamPieceKind.Plate)
                    continue;
                if (piece.Row != row || piece.Column != column)
                    continue;
                if (_carriedPlates != null && _carriedPlates.Contains(piece))
                    continue;

                found = piece;
                break;
            }

            if (found == null)
                return null;

            CarryBlockJamBoardPiece basePiece = GetPickupBasePiece(found);
            return basePiece != null ? basePiece : found;
        }

        private CarryBlockJamBoardPiece GetCellBoardPiece(int row, int column)
        {
            if (_grid == null ||
                !_grid.TryGetCell(row, column, out PuzzleCell cell) ||
                cell == null)
                return null;

            CarryBlockJamBoardPiece piece = null;
            if (cell.Occupant != null)
                piece = cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();

            if (piece == _cylinder)
                piece = null;

            if (piece != null)
                return piece;

            // Occupant can be missing/stolen while the plate is still on the cell.
            CarryBlockJamBoardPiece plate = FindFreestandingPlateAtCell(row, column);
            if (plate != null)
                return plate;

            return FindBoxAtCell(row, column);
        }

        private bool TryResolveCollectiblePlate(
            CarryBlockJamBoardPiece occupant,
            int row,
            int column,
            PieceColorType requiredColor,
            out CarryBlockJamBoardPiece plate)
        {
            plate = null;
            CarryBlockJamBoardPiece root = occupant ?? FindBoxAtCell(row, column);
            if (root == null)
                return false;

            CarryBlockJamBoardPiece current = GetPickupBasePiece(root);
            while (current != null)
            {
                if (IsCollectiblePlate(current, row, column, requiredColor))
                {
                    plate = current;
                    return true;
                }

                current = current.StackedAbove;
            }

            return false;
        }

        private bool HasCollectiblePlateAt(int row, int column, PieceColorType requiredColor)
        {
            if (IsTableCollectOnCooldown(row, column))
                return false;

            return TryResolveCollectiblePlate(
                GetCellBoardPiece(row, column),
                row,
                column,
                requiredColor,
                out _);
        }

        private void ArmTableCollectCooldown(int row, int column)
        {
            if (_grid == null || !_grid.IsInside(row, column))
                return;

            float delay = Mathf.Max(0.05f, tableDropCollectCooldown);
            Vector2Int key = new Vector2Int(row, column);
            float until = Time.time + delay;
            if (_tableCollectCooldownUntil.TryGetValue(key, out float existing))
                until = Mathf.Max(until, existing);
            _tableCollectCooldownUntil[key] = until;
        }

        private void MarkTablePickupBlocksDeliver(int row, int column)
        {
            if (_grid == null || !_grid.IsInside(row, column))
                return;

            _hasTablePickupBlockDeliver = true;
            _tablePickupBlockDeliverCell = new Vector2Int(row, column);
        }

        private bool IsTableDeliverBlockedByRecentPickup(int row, int column)
        {
            return _hasTablePickupBlockDeliver &&
                   _tablePickupBlockDeliverCell.x == row &&
                   _tablePickupBlockDeliverCell.y == column;
        }

        private bool IsTableCollectOnCooldown(int row, int column)
        {
            Vector2Int key = new Vector2Int(row, column);
            if (!_tableCollectCooldownUntil.TryGetValue(key, out float until))
                return false;

            if (Time.time >= until)
            {
                _tableCollectCooldownUntil.Remove(key);
                return false;
            }

            return true;
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
            var pickupPieces = new List<CarryBlockJamBoardPiece>();

            for (int stepIndex = 0; stepIndex < requestedSteps; stepIndex++)
            {
                int nextRow = row + rowStep;
                int nextColumn = column + columnStep;
                if (!_grid.IsInside(nextRow, nextColumn) ||
                    !_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell nextCell) ||
                    nextCell == null)
                    break;

                CarryBlockJamBoardPiece nextPiece = GetCellBoardPiece(nextRow, nextColumn);

                if (nextPiece == null)
                {
                    row = nextRow;
                    column = nextColumn;
                    path.Add(new Vector2Int(nextRow, nextColumn));
                    continue;
                }

                if (TryCollectPlatesFromOccupant(nextPiece, nextRow, nextColumn, pickupPieces))
                {
                    // Freestanding plates are walkable; tables stay blocked like other obstacles.
                    if (IsBoxOwnedCell(nextRow, nextColumn) || IsBoxCellBlocker(nextPiece))
                    {
                        if (IsBoxOwnedCell(nextRow, nextColumn))
                            MarkTablePickupBlocksDeliver(nextRow, nextColumn);
                        break;
                    }

                    row = nextRow;
                    column = nextColumn;
                    path.Add(new Vector2Int(nextRow, nextColumn));
                    continue;
                }

                // Table ahead with nothing collectible — stay on previous cell.
                if (IsBoxOwnedCell(nextRow, nextColumn) || IsBoxCellBlocker(nextPiece))
                    break;

                break;
            }

            if (pickupPieces.Count > 0)
            {
                MoveConnectedCylinder(path, () =>
                {
                    AddPlatesToCarryStack(
                        pickupPieces,
                        fromTable: _hasTablePickupBlockDeliver);
                });
                return;
            }

            if (path.Count > 0)
                AnimateCylinderTravel(path);
        }

        private void ExecuteCarrySwipe(int rowStep, int columnStep, int requestedSteps)
        {
            int currentRow = _cylinder.Row;
            int currentColumn = _cylinder.Column;
            var cylinderPath = new List<Vector2Int>();
            var pickupPieces = new List<CarryBlockJamBoardPiece>();
            CarryBlockJamBoardPiece targetBox = null;
            bool pressedOffBoard = false;

            for (int stepIndex = 0; stepIndex < requestedSteps; stepIndex++)
            {
                int nextRow = currentRow + rowStep;
                int nextColumn = currentColumn + columnStep;
                if (!_grid.IsInside(nextRow, nextColumn) ||
                    !_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell nextCell) ||
                    nextCell == null)
                {
                    pressedOffBoard = !_grid.IsInside(nextRow, nextColumn);
                    break;
                }

                CarryBlockJamBoardPiece nextPiece = GetCellBoardPiece(nextRow, nextColumn);

                if (nextPiece == null)
                {
                    currentRow = nextRow;
                    currentColumn = nextColumn;
                    cylinderPath.Add(new Vector2Int(nextRow, nextColumn));
                    continue;
                }

                if (TryCollectPlatesFromOccupant(nextPiece, nextRow, nextColumn, pickupPieces))
                {
                    // Freestanding plates are walkable; tables stay blocked like other obstacles.
                    if (IsBoxOwnedCell(nextRow, nextColumn) || IsBoxCellBlocker(nextPiece))
                    {
                        if (IsBoxOwnedCell(nextRow, nextColumn))
                            MarkTablePickupBlocksDeliver(nextRow, nextColumn);
                        break;
                    }

                    currentRow = nextRow;
                    currentColumn = nextColumn;
                    cylinderPath.Add(new Vector2Int(nextRow, nextColumn));
                    continue;
                }

                if (IsBoxOwnedCell(nextRow, nextColumn) || IsBoxCellBlocker(nextPiece))
                {
                    // Drop onto matching table when this swipe reached it while
                    // carrying. Require travel (or starting already loaded) so a
                    // pickup-from-table swipe does not put plates straight back.
                    if (CanDeliverCarriedPlatesToCell(
                            nextRow,
                            nextColumn,
                            currentRow,
                            currentColumn,
                            cylinderPath.Count))
                    {
                        CarryBlockJamBoardPiece dropBox = GetStorageBox(nextPiece) ??
                            (nextPiece != null && nextPiece.Kind == CarryBlockJamPieceKind.Box
                                ? nextPiece
                                : FindBoxAtCell(nextRow, nextColumn));
                        if (dropBox != null && CanDeliverToTable(dropBox))
                            targetBox = dropBox;
                    }

                    break;
                }

                CarryBlockJamBoardPiece storageBox = GetStorageBox(nextPiece);
                if (storageBox != null &&
                    CanDeliverCarriedPlatesToCell(
                        nextRow,
                        nextColumn,
                        currentRow,
                        currentColumn,
                        cylinderPath.Count) &&
                    CanDeliverToTable(storageBox))
                {
                    targetBox = storageBox;
                    break;
                }

                if (IsMatchingDropTarget(nextPiece) &&
                    CanDeliverCarriedPlatesToCell(
                        nextRow,
                        nextColumn,
                        currentRow,
                        currentColumn,
                        cylinderPath.Count))
                {
                    targetBox = nextPiece;
                    break;
                }

                break;
            }

            // Gate delivery only when this swipe presses off the board toward the
            // exit — not merely from passing/stopping on an edge exit cell during
            // fast circular movement along the rim.
            bool reachedExit = false;
            CarryBlockJamExit edgeExit = null;
            if (pressedOffBoard && HasCarriedPlates)
            {
                reachedExit = TryResolveExit(
                    rowStep,
                    columnStep,
                    currentRow,
                    currentColumn,
                    out edgeExit);
            }

            if (pickupPieces.Count > 0 || targetBox != null || (reachedExit && HasCarriedPlates))
            {
                MoveConnectedCylinder(cylinderPath, () =>
                {
                    if (pickupPieces.Count > 0)
                        AddPlatesToCarryStack(
                            pickupPieces,
                            fromTable: _hasTablePickupBlockDeliver);

                    if (targetBox != null && HasCarriedPlates)
                    {
                        AnimateCarriedPlatesToBox(targetBox);
                        return;
                    }

                    if (reachedExit && edgeExit != null && HasCarriedPlates)
                        AnimateCarriedPlatesToExit(edgeExit);
                });
                return;
            }

            if (!HasCarriedPlates)
                return;

            if (reachedExit && edgeExit != null)
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
            if (TryCompleteDraggedMovementInstantly(
                    path,
                    allowBoxOwnedDestination: false,
                    onComplete: onComplete))
                return;

            if (_cylinder == null || path == null || path.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            RefreshStickmanAnimation(moving: true);
            Haptic.HeavyTaptic();
            EnsureCharTableTrail();
            KillCharTableTrailAbsorbTween();
            _charTableTrailHasLastPos = false;
            if (_cylinder != null)
            {
                _charTableTrailLastWorldPos = _cylinder.transform.position;
                _charTableTrailHasLastPos = true;
            }
            SetCharTableTrailEmitting(true);

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
                    moveDurationPerCell).SetEase(Ease.Linear)
                    .OnUpdate(() =>
                    {
                        _trailSessionCells += Time.deltaTime / Mathf.Max(0.01f, moveDurationPerCell);
                        RefreshCharTableTrailHeaviness();
                    }));
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
                AbsorbCharTableTrail();
                onComplete?.Invoke();
            });
        }

        private bool TryCompleteDraggedMovementInstantly(
            List<Vector2Int> path,
            bool allowBoxOwnedDestination,
            TweenCallback onComplete)
        {
            if (!_commitDraggedMovementInstantly)
                return false;

            _commitDraggedMovementInstantly = false;
            if (_cylinder == null || path == null || path.Count == 0)
            {
                SnapCylinderToLogicalCell();
                RefreshStickmanAnimation(moving: false);
                onComplete?.Invoke();
                return true;
            }

            int finalIndex = path.Count - 1;
            if (!allowBoxOwnedDestination)
            {
                while (finalIndex >= 0 &&
                       IsBoxOwnedCell(path[finalIndex].x, path[finalIndex].y))
                    finalIndex--;
            }

            if (finalIndex < 0)
            {
                SnapCylinderToLogicalCell();
                RefreshStickmanAnimation(moving: false);
                onComplete?.Invoke();
                return true;
            }

            Vector2Int destination = path[finalIndex];
            Haptic.HeavyTaptic();
            PlaceStickmanOnCell(destination.x, destination.y);
            RefreshStickmanAnimation(moving: false);
            onComplete?.Invoke();
            return true;
        }

        private List<Vector2Int> TrimPathBeforeBoxBlocker(List<Vector2Int> path)
        {
            return TrimPathBeforeMovementBlocker(path);
        }

        private List<Vector2Int> TrimPathBeforeMovementBlocker(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0 || _grid == null)
                return path;

            PieceColorType requiredColor = GetRequiredCollectColor();
            var safePath = new List<Vector2Int>(path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int step = path[i];
                if (IsDragPathBlocker(step.x, step.y, requiredColor))
                    break;

                if (TryResolveCollectiblePlate(
                        GetCellBoardPiece(step.x, step.y),
                        step.x,
                        step.y,
                        requiredColor,
                        out CarryBlockJamBoardPiece collectPlate) &&
                    requiredColor == PieceColorType.None &&
                    collectPlate != null)
                {
                    requiredColor = collectPlate.Color;
                }

                safePath.Add(step);
            }

            return safePath;
        }

        /// <summary>
        /// Movement blockers while dragging: every table, and any plate that is not
        /// collectible for the active drag color. Matching freestanding plates are
        /// walkable and are collected by the swept drag pickup.
        /// </summary>
        private bool IsDragPathBlocker(int row, int column, PieceColorType requiredColor)
        {
            if (_grid == null || !_grid.IsInside(row, column))
                return true;

            if (IsBoxOwnedCell(row, column))
                return true;

            CarryBlockJamBoardPiece piece = GetCellBoardPiece(row, column);
            if (piece == null || piece == _cylinder)
                return false;

            if (IsBoxCellBlocker(piece))
                return true;

            // Matching / empty-carry collectible plates are enterable for pickup.
            if (HasCollectiblePlateAt(row, column, requiredColor))
                return false;

            // Different-color plates, hidden/frozen/curtained plates, and other
            // occupants always block the path.
            return true;
        }

        /// <summary>
        /// True when the cell's board occupant is a freestanding plate stack (not a table).
        /// </summary>
        private bool IsFreestandingPlateCell(int row, int column)
        {
            // Row/Column lookup remains reliable even if CharTable temporarily
            // owns the PuzzleCell occupant during a very fast drag.
            return FindFreestandingPlateAtCell(row, column) != null;
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
            if (!HasCarriedPlates)
                ClearCharTableTrail(resetHeaviness: true);

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
                    Haptic.MediumTaptic();
                    PlayCarrySfx(plateDeliverSound);
                    // VFX is owned by the gate exit — never the plate/CharTable pose.
                    exitComponent.ConsumeOne(deliverColor);
                    CarryBlockJamHiddenBox.NotifyPlateCollected(arrivingPlate);
                    CarryBlockJamHiddenPlate.NotifyPlateCollected(arrivingPlate);
                    CarryBlockJamFrozenBox.NotifyPlateCollected(arrivingPlate);
                    if (arrivingPlate != null)
                    {
                        arrivingPlate.gameObject.SetActive(false);
                        Destroy(arrivingPlate.gameObject);
                    }
                });
            }

            // A plate that is parented to CharTable but belongs to neither the
            // logical carried stack nor this delivery is a stale pickup visual.
            // Remove it before the gate animation so an old pile cannot remain
            // attached after its logical plates have already been delivered.
            RemoveStaleCarryPlateVisuals(plates);

            sequence.OnComplete(() =>
            {
                UpdateCarriedPlateVisuals();
                _isAnimating = false;
                if (HasCarriedPlates)
                {
                    MaybeNotifyTutorialExitDelivery();
                    EvaluateCarriedPlateDeadlock();
                    return;
                }

                // Win before tutorial advance — otherwise the next stage snaps CharTable to start.
                TryTriggerSuccess(plates);
                if (_successTriggered)
                {
                    TutorialManager.Instance?.CompleteAndHide();
                    return;
                }

                MaybeNotifyTutorialExitDelivery();
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
            // CharTable stack index 0 is the bottom — deliver top plate first.
            plates.Reverse();
            // Lock collect as soon as delivery starts so a fast overlapping drag
            // cannot pick plates back up mid-animation.
            ArmTableCollectCooldown(targetBox.Row, targetBox.Column);
            RefreshStickmanAnimation(moving: false);
            ClearCharTableTrail(resetHeaviness: true);
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
                if (targetBox != null)
                    ArmTableCollectCooldown(targetBox.Row, targetBox.Column);
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
            // CharTable visual is Y-scaled; worldPositionStays keeps that squash on
            // the plate and makes table stacks look overlapped. Restore full scale.
            plate.transform.localScale = Vector3.one;
            Vector3 stackWorldTarget = basePiece.transform.TransformPoint(
                basePiece.GetStackAttachLocalPosition(plate));
            float tableAnimationSpeed = Mathf.Max(0.01f, tablePlateAnimationSpeed);
            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            bool usesCharTable = spawner != null && spawner.UsesCharTableCylinderVisual;
            if (usesCharTable)
            {
                CarryBlockJamPrefabSettings settings =
                    board != null ? board.PrefabSettings : null;
                float charTableSpeed = settings != null
                    ? settings.charTableDropAnimationSpeed
                    : 3f;
                tableAnimationSpeed *= Mathf.Max(0.01f, charTableSpeed);
            }

            Sequence landing = DOTween.Sequence();
            if (usesCharTable)
            {
                // Tall curvy soar from CharTable onto the table stack (top plate first).
                CarryBlockJamBoardPiece stackBase = basePiece;
                landing.Append(BuildTableTransferSoarTween(
                    plate,
                    index,
                    () => stackBase != null
                        ? stackBase.transform.TransformPoint(
                            stackBase.GetStackAttachLocalPosition(plate))
                        : stackWorldTarget,
                    () => stackBase != null
                        ? stackBase.transform.rotation
                        : Quaternion.identity,
                    charTableDropArcHeightMul,
                    charTableDropDuration));
            }
            else
            {
                float jumpDuration = Mathf.Max(
                    0.01f,
                    Mathf.Max(exitTravelDuration, tablePlateJumpDuration) / tableAnimationSpeed);
                Vector3 spinTarget = plate.transform.eulerAngles + new Vector3(0f, 270f, 0f);
                landing.Append(plate.transform.DOJump(
                    stackWorldTarget,
                    Mathf.Max(0.01f, tablePlateJumpHeight),
                    1,
                    jumpDuration).SetEase(Ease.OutQuad));
                landing.Join(plate.transform.DORotate(
                    spinTarget,
                    jumpDuration,
                    RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            }

            landing.AppendCallback(() =>
            {
                Haptic.MediumTaptic();
                PlayCarrySfx(plateDeliverSound);
                plate.StackOnPiece(basePiece);
                if (_grid.TryGetCell(targetBox.Row, targetBox.Column, out PuzzleCell cell) && cell != null)
                    cell.Occupant = plate.gameObject;
                // Brief lockout so an immediate second drag cannot vacuum plates
                // back onto CharTable before the drop reads as intentional.
                ArmTableCollectCooldown(targetBox.Row, targetBox.Column);
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
            if (UsesCharTableVisual())
                CaptureCharTableVisualRestPose();
        }

        private void EnsureStickmanAnimator()
        {
            if (_cylinder == null)
            {
                _stickmanAnimator = null;
                return;
            }

            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            if (spawner != null && spawner.UsesCharTableCylinderVisual)
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
            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            bool usesCarryPose = HasCarriedPlates ||
                                 (spawner != null && spawner.UsesCharTableCylinderVisual);
            _stickmanAnimator?.SetState(moving, usesCarryPose);
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

            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            if (spawner != null && spawner.UsesCharTableCylinderVisual)
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

            ShowHighlights(false);
            Vector2 currentScreenPosition;
            if (Input.touchCount > 0)
            {
                if (_activeFingerId >= 0 && TryGetTouchById(_activeFingerId, out Touch activeTouch))
                    currentScreenPosition = activeTouch.position;
                else
                    currentScreenPosition = Input.GetTouch(0).position;
            }
            else
            {
                currentScreenPosition = Input.mousePosition;
            }

            bool tutorialPathLocked =
                TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                TutorialManager.Instance.IsStagePathLocked;
            if (!tutorialPathLocked)
            {
                UpdateOrthogonalDrag(currentScreenPosition);
                return;
            }

            if (!TryGetSwipeIntent(currentScreenPosition, out int rowStep, out int columnStep, out int requestedSteps))
            {
                SnapCylinderToLogicalCell();
                RefreshStickmanAnimation(moving: false);
                return;
            }

            if (tutorialPathLocked)
            {
                if (!TutorialManager.Instance.TryClampSwipeToAuthoredPath(
                        _cylinder.Row,
                        _cylinder.Column,
                        ref rowStep,
                        ref columnStep,
                        ref requestedSteps))
                {
                    SnapCylinderToLogicalCell();
                    RefreshStickmanAnimation(moving: false);
                    return;
                }

                var previewPath = new List<Vector2Int>();
                if (!TutorialManager.Instance.TryBuildAuthoredPathCells(
                        _cylinder.Row,
                        _cylinder.Column,
                        previewPath))
                {
                    SnapCylinderToLogicalCell();
                    RefreshStickmanAnimation(moving: false);
                    return;
                }

                if (TutorialManager.Instance.TryGetActivePath(out _, out Vector2Int previewTarget))
                    TrimTutorialPathBeforeTableDrop(previewPath, previewTarget);

                if (previewPath.Count > requestedSteps)
                    previewPath.RemoveRange(requestedSteps, previewPath.Count - requestedSteps);

                UpdateDraggedCylinderPosition(
                    currentScreenPosition,
                    rowStep,
                    columnStep,
                    previewPath);
                return;
            }
        }

        private void UpdateOrthogonalDrag(Vector2 screenPosition)
        {
            if (_dragActiveAxis == 0)
            {
                if (!TryGetSwipeIntent(
                        screenPosition,
                        out _,
                        out _,
                        out _))
                {
                    // Keep the current pose — snapping here feels like a border hitch.
                    RefreshStickmanAnimation(moving: false);
                    return;
                }

                _dragActiveAxis = _lockedDragAxis;
                _dragSegmentBoardLocalPoint = _dragStartBoardLocalPoint;
            }

            Vector2Int segmentStart = _dragRouteCorners.Count > 0
                ? _dragRouteCorners[_dragRouteCorners.Count - 1]
                : new Vector2Int(_swipeStartRow, _swipeStartColumn);

            if (TryResolveDragVisualCell(out Vector2Int visualCell))
            {
                // Keep the segment pinned to where CharTable actually is so wall
                // hits don't leave the drag anchored on an earlier cell.
                if (visualCell != segmentStart &&
                    (IsActiveAxisBlockedFrom(segmentStart, screenPosition) ||
                     IsActiveAxisBlockedFrom(visualCell, screenPosition)))
                {
                    ReanchorDragSegmentToCell(visualCell);
                    segmentStart = visualCell;
                }
            }

            bool blockedOnActiveAxis =
                IsActiveAxisBlockedFrom(segmentStart, screenPosition);

            TryCommitOrthogonalTurn(
                screenPosition,
                segmentStart,
                blockedOnActiveAxis);
            segmentStart = _dragRouteCorners.Count > 0
                ? _dragRouteCorners[_dragRouteCorners.Count - 1]
                : new Vector2Int(_swipeStartRow, _swipeStartColumn);

            if (!TryGetActiveSegmentIntent(
                    screenPosition,
                    segmentStart.x,
                    segmentStart.y,
                    out int rowStep,
                    out int columnStep,
                    out int requestedSteps))
            {
                // Dead axis (wall/blocker): switch to the finger's sideways axis
                // without requiring a release + new drag.
                if (TrySwitchAxisToFingerDirection(
                        screenPosition,
                        segmentStart,
                        forceWhenBlocked: true))
                {
                    segmentStart = _dragRouteCorners.Count > 0
                        ? _dragRouteCorners[_dragRouteCorners.Count - 1]
                        : segmentStart;
                    if (!TryGetActiveSegmentIntent(
                            screenPosition,
                            segmentStart.x,
                            segmentStart.y,
                            out rowStep,
                            out columnStep,
                            out requestedSteps))
                    {
                        RefreshStickmanAnimation(moving: false);
                        return;
                    }
                }
                else
                {
                    RefreshStickmanAnimation(moving: false);
                    return;
                }
            }

            // Circular flicks can project a huge axis delta from a stale segment
            // anchor — never build/follow a path past the finger cell.
            requestedSteps = ClampDragStepsTowardFinger(
                screenPosition,
                segmentStart.x,
                segmentStart.y,
                rowStep,
                columnStep,
                requestedSteps,
                allowOffBoardPress: false);

            if (requestedSteps <= 0)
            {
                // Path length can be 0 while pressed into a table neighbor — still
                // pick up table stacks on that cell (not freestanding plates).
                TryCollectNeighborInDragDirection(
                    screenPosition,
                    segmentStart.x,
                    segmentStart.y,
                    rowStep,
                    columnStep);
                TryCollectPlatesUnderCharTable();
                TryMagnetDeliverAtGateDuringDrag();
                RefreshStickmanAnimation(moving: false);
                return;
            }

            if (!TryProjectPointerToBoardLocal(
                    screenPosition,
                    out Vector3 pointerLocal))
            {
                RefreshStickmanAnimation(moving: false);
                return;
            }

            Vector3 pointerDelta = pointerLocal - _dragSegmentBoardLocalPoint;
            float draggedCells = rowStep != 0
                ? -pointerDelta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ)
                : pointerDelta.x / Mathf.Max(0.0001f, _grid.GridSpacingX);
            float directedProgress = Mathf.Max(
                0f,
                draggedCells * (rowStep != 0 ? rowStep : columnStep));
            float fingerAlong = MeasureFingerAlongAxis(
                screenPosition,
                segmentStart.x,
                segmentStart.y,
                rowStep,
                columnStep);
            if (fingerAlong >= 0f)
                directedProgress = Mathf.Min(directedProgress, fingerAlong + 0.35f);
            float followProgress = directedProgress;

            // Collect uses the unscaled finger progress so pickup is not delayed
            // behind followGain / visual lag. Tables only here — freestanding
            // plates collect from the cell CharTable is on (below).
            TryCollectAlongDragSegment(
                segmentStart.x,
                segmentStart.y,
                rowStep,
                columnStep,
                directedProgress);
            TryCollectNeighborInDragDirection(
                screenPosition,
                segmentStart.x,
                segmentStart.y,
                rowStep,
                columnStep);

            List<Vector2Int> previewPath = BuildPreviewPath(
                segmentStart.x,
                segmentStart.y,
                rowStep,
                columnStep,
                requestedSteps);
            List<Vector2Int> movablePath = TrimPathBeforeMovementBlocker(previewPath);

            ApplyDraggedCylinderPosition(
                followProgress,
                rowStep,
                columnStep,
                movablePath,
                segmentStart.x,
                segmentStart.y);

            // Quick on-cell freestanding pickup as CharTable overlaps each plate cell.
            TryCollectPlatesUnderCharTable();
            if (TryMagnetDeliverAtGateDuringDrag())
                return;

            // Reanchor from hysteresis follow cell — never Round() at borders.
            Vector2Int stopCell = IsValidCharTableSettleCell(
                    _dragFollowCell.x,
                    _dragFollowCell.y)
                ? _dragFollowCell
                : (movablePath.Count > 0
                    ? movablePath[movablePath.Count - 1]
                    : segmentStart);
            bool pressedPastEnd = followProgress > movablePath.Count + 0.05f;
            if (movablePath.Count == 0 ||
                (pressedPastEnd && IsActiveAxisBlockedFrom(stopCell, screenPosition)))
            {
                ReanchorDragSegmentToCell(stopCell);
                if (TryProjectPointerToBoardLocal(screenPosition, out Vector3 blockedPointer))
                    _dragTurnProbeBoardLocalPoint = blockedPointer;
            }
        }

        /// <summary>
        /// When the locked axis has no legal steps (wall/blocker), adopt the
        /// finger's stronger perpendicular axis so dragging can continue.
        /// </summary>
        private bool TrySwitchAxisToFingerDirection(
            Vector2 screenPosition,
            Vector2Int segmentStart,
            bool forceWhenBlocked)
        {
            if (_dragActiveAxis == 0 ||
                !TryProjectPointerToBoardLocal(
                    screenPosition,
                    out Vector3 pointerLocal))
                return false;

            Vector3 delta = pointerLocal - _dragSegmentBoardLocalPoint;
            float rowCells =
                -delta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ);
            float columnCells =
                delta.x / Mathf.Max(0.0001f, _grid.GridSpacingX);
            float rowAbs = Mathf.Abs(rowCells);
            float columnAbs = Mathf.Abs(columnCells);
            float activeAbs = _dragActiveAxis == 1 ? rowAbs : columnAbs;
            float perpAbs = _dragActiveAxis == 1 ? columnAbs : rowAbs;
            float switchThreshold = forceWhenBlocked ? 0.08f : 0.16f;
            if (perpAbs < switchThreshold)
                return false;
            if (!forceWhenBlocked && perpAbs < activeAbs * Mathf.Max(1f, dragTurnDominance))
                return false;

            // Prefer switching when the current axis is blocked or clearly weaker.
            int nextRow = segmentStart.x + (_dragActiveAxis == 1
                ? (rowCells >= 0f ? 1 : -1)
                : 0);
            int nextColumn = segmentStart.y + (_dragActiveAxis == 2
                ? (columnCells >= 0f ? 1 : -1)
                : 0);
            bool activeBlocked =
                _dragActiveAxis == 1
                    ? !_grid.IsInside(nextRow, segmentStart.y) ||
                      IsDragPathBlocker(
                          nextRow,
                          segmentStart.y,
                          GetRequiredCollectColor())
                    : !_grid.IsInside(segmentStart.x, nextColumn) ||
                      IsDragPathBlocker(
                          segmentStart.x,
                          nextColumn,
                          GetRequiredCollectColor());
            if (!forceWhenBlocked && !activeBlocked && perpAbs < activeAbs * 1.15f)
                return false;

            int nextAxis = _dragActiveAxis == 1 ? 2 : 1;
            int stepSign = nextAxis == 1
                ? (rowCells >= 0f ? 1 : -1)
                : (columnCells >= 0f ? 1 : -1);
            int probeRow = segmentStart.x + (nextAxis == 1 ? stepSign : 0);
            int probeColumn = segmentStart.y + (nextAxis == 2 ? stepSign : 0);
            if (_grid.IsInside(probeRow, probeColumn) &&
                IsDragPathBlocker(probeRow, probeColumn, GetRequiredCollectColor()))
            {
                // Side direction also blocked — still switch so reverse on that
                // axis can be measured from this cell.
            }

            ReanchorDragSegmentToCell(segmentStart);
            _dragActiveAxis = nextAxis;
            _lockedDragAxis = nextAxis;
            _dragTurnProbeBoardLocalPoint = pointerLocal;
            _trailSegmentProgressReported = 0f;
            return true;
        }

        private bool IsActiveAxisBlockedFrom(Vector2Int segmentStart, Vector2 screenPosition)
        {
            if (_grid == null || _dragActiveAxis == 0)
                return false;
            if (!TryProjectPointerToBoardLocal(screenPosition, out Vector3 pointerLocal))
                return false;

            Vector3 pointerDelta = pointerLocal - _dragSegmentBoardLocalPoint;
            float cells = _dragActiveAxis == 1
                ? -pointerDelta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ)
                : pointerDelta.x / Mathf.Max(0.0001f, _grid.GridSpacingX);
            if (Mathf.Abs(cells) < Mathf.Max(0.05f, dragCellEngageThreshold))
                return false;

            int rowStep = 0;
            int columnStep = 0;
            if (_dragActiveAxis == 1)
                rowStep = cells > 0f ? 1 : -1;
            else
                columnStep = cells > 0f ? 1 : -1;

            int nextRow = segmentStart.x + rowStep;
            int nextColumn = segmentStart.y + columnStep;
            if (!_grid.IsInside(nextRow, nextColumn))
                return true;

            return IsDragPathBlocker(nextRow, nextColumn, GetRequiredCollectColor());
        }

        private bool TryResolveDragVisualCell(out Vector2Int cell)
        {
            cell = default;
            if (!TryGetDragVisualCellContinuous(out float rowContinuous, out float columnContinuous))
                return false;

            int row = Mathf.Clamp(
                Mathf.RoundToInt(rowContinuous),
                0,
                _grid.Rows - 1);
            int column = Mathf.Clamp(
                Mathf.RoundToInt(columnContinuous),
                0,
                _grid.Columns - 1);
            if (!_grid.TryGetCell(row, column, out PuzzleCell puzzleCell) ||
                puzzleCell == null)
                return false;

            // Never treat a table cell as CharTable's visual cell — that caused
            // settle/snap fights and edge-looking overlaps on fast drags.
            if (IsBoxOwnedCell(row, column))
                return false;

            // Never claim a different-color plate cell from Round() — that hid the
            // plate behind CharTable Occupant and allowed pathing through it.
            PieceColorType requiredColor = GetRequiredCollectColor();
            if (IsDragPathBlocker(row, column, requiredColor))
                return false;

            // A rounded visual cell may be several cells ahead after a long frame.
            // Validate every intermediate cell so Round() cannot leapfrog a plate.
            if (!IsAxisReachableWithoutPlateCross(
                    _dragFollowCell.x,
                    _dragFollowCell.y,
                    row,
                    column,
                    requiredColor))
                return false;

            cell = new Vector2Int(row, column);
            return true;
        }

        private bool IsAxisReachableWithoutPlateCross(
            int fromRow,
            int fromColumn,
            int toRow,
            int toColumn,
            PieceColorType requiredColor)
        {
            if (fromRow == toRow && fromColumn == toColumn)
                return true;

            int rowDelta = toRow - fromRow;
            int columnDelta = toColumn - fromColumn;
            if (rowDelta != 0 && columnDelta != 0)
                return false;

            int rowStep = rowDelta == 0 ? 0 : (rowDelta > 0 ? 1 : -1);
            int columnStep = columnDelta == 0 ? 0 : (columnDelta > 0 ? 1 : -1);
            int steps = Mathf.Max(Mathf.Abs(rowDelta), Mathf.Abs(columnDelta));
            int row = fromRow;
            int column = fromColumn;

            for (int i = 0; i < steps; i++)
            {
                int nextRow = row + rowStep;
                int nextColumn = column + columnStep;
                if (!_grid.IsInside(nextRow, nextColumn) ||
                    IsDragPathBlocker(nextRow, nextColumn, requiredColor))
                    return false;

                if (requiredColor == PieceColorType.None &&
                    TryResolveCollectiblePlate(
                        GetCellBoardPiece(nextRow, nextColumn),
                        nextRow,
                        nextColumn,
                        requiredColor,
                        out CarryBlockJamBoardPiece collectPlate) &&
                    collectPlate != null)
                {
                    requiredColor = collectPlate.Color;
                }

                row = nextRow;
                column = nextColumn;
            }

            return true;
        }

        private bool TryGetDragVisualCellContinuous(
            out float rowContinuous,
            out float columnContinuous)
        {
            rowContinuous = 0f;
            columnContinuous = 0f;
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return false;

            Vector3 gridLocalPosition =
                _cylinder.transform.localPosition - _cylinder.GridOffset;
            columnContinuous =
                gridLocalPosition.x / _grid.GridSpacingX +
                (_grid.Columns - 1) * 0.5f;
            rowContinuous =
                (_grid.Rows - 1) * 0.5f -
                gridLocalPosition.z / _grid.GridSpacingZ;
            return true;
        }

        /// <summary>
        /// Advance through visual cells one at a time and validate each one. This
        /// preserves the existing hysteresis threshold while preventing long-frame
        /// jumps from skipping a floor plate.
        /// </summary>
        private void UpdateDragFollowCellFromVisual()
        {
            if (!TryGetDragVisualCellContinuous(
                    out float rowContinuous,
                    out float columnContinuous))
                return;

            int row = Mathf.Clamp(_dragFollowCell.x, 0, _grid.Rows - 1);
            int column = Mathf.Clamp(_dragFollowCell.y, 0, _grid.Columns - 1);
            float cross = Mathf.Clamp(dragCellCrossThreshold, 0.5f, 0.9f);
            PieceColorType requiredColor = GetRequiredCollectColor();

            while (true)
            {
                int nextRow = row;
                int nextColumn = column;
                if (rowContinuous >= row + cross)
                    nextRow++;
                else if (rowContinuous <= row - cross)
                    nextRow--;
                else if (columnContinuous >= column + cross)
                    nextColumn++;
                else if (columnContinuous <= column - cross)
                    nextColumn--;
                else
                    break;

                if (!_grid.IsInside(nextRow, nextColumn) ||
                    IsBoxOwnedCell(nextRow, nextColumn) ||
                    IsDragPathBlocker(nextRow, nextColumn, requiredColor))
                    break;

                row = nextRow;
                column = nextColumn;

                if (requiredColor == PieceColorType.None &&
                    TryResolveCollectiblePlate(
                        GetCellBoardPiece(row, column),
                        row,
                        column,
                        requiredColor,
                        out CarryBlockJamBoardPiece collectPlate) &&
                    collectPlate != null)
                {
                    requiredColor = collectPlate.Color;
                }
            }

            row = Mathf.Clamp(row, 0, _grid.Rows - 1);
            column = Mathf.Clamp(column, 0, _grid.Columns - 1);

            _dragFollowCell = new Vector2Int(row, column);
        }

        private int StableCellIndex(float continuous, int current)
        {
            float cross = Mathf.Clamp(dragCellCrossThreshold, 0.5f, 0.9f);
            while (continuous >= current + cross)
                current++;
            while (continuous <= current - cross)
                current--;
            return current;
        }

        private void ReanchorDragSegmentToCell(Vector2Int cell)
        {
            if (_grid == null || !_grid.IsInside(cell.x, cell.y))
                return;

            if (_dragRouteCorners.Count == 0)
                _dragRouteCorners.Add(cell);
            else if (_dragRouteCorners[_dragRouteCorners.Count - 1] != cell)
                _dragRouteCorners.Add(cell);

            if (!IsBoxOwnedCell(cell.x, cell.y))
                _dragFollowCell = cell;

            _dragSegmentBoardLocalPoint = _grid.GetLocalPosition(cell.x, cell.y);
            _trailSegmentProgressReported = 0f;
        }

        private bool UpdateDragCornerTransition()
        {
            // Kept for compatibility; corner settles are applied immediately now.
            if (!_dragCornerTransitionActive || _cylinder == null)
                return false;

            _cylinder.transform.localPosition = _dragCornerTransitionTarget;
            _dragCornerTransitionActive = false;
            _dragCornerTransitionElapsed = 0f;
            _trailSegmentProgressReported = 0f;
            return false;
        }

        private bool TryCommitOrthogonalTurn(
            Vector2 screenPosition,
            Vector2Int segmentStart)
        {
            return TryCommitOrthogonalTurn(
                screenPosition,
                segmentStart,
                blockedOnActiveAxis: false);
        }

        private bool TryCommitOrthogonalTurn(
            Vector2 screenPosition,
            Vector2Int segmentStart,
            bool blockedOnActiveAxis)
        {
            if (!TryProjectPointerToBoardLocal(
                    screenPosition,
                    out Vector3 pointerLocal))
                return false;

            Vector3 probeDelta =
                pointerLocal - _dragTurnProbeBoardLocalPoint;
            float probeRowCells =
                -probeDelta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ);
            float probeColumnCells =
                probeDelta.x / Mathf.Max(0.0001f, _grid.GridSpacingX);
            float recentActiveMovement = Mathf.Abs(
                _dragActiveAxis == 1
                    ? probeRowCells
                    : probeColumnCells);
            float recentPerpendicularMovement = Mathf.Abs(
                _dragActiveAxis == 1
                    ? probeColumnCells
                    : probeRowCells);
            float dominance = Mathf.Max(1f, dragTurnDominance);
            float turnThreshold = blockedOnActiveAxis
                ? Mathf.Max(0.06f, dragTurnThresholdCells * 0.55f)
                : Mathf.Max(0.08f, dragTurnThresholdCells);

            // Straight drag: keep resetting the probe so tiny wobble cannot
            // build into a turn. When pressed into a wall/blocker, do NOT reset
            // on active-axis noise or side moves are never detected.
            if (!blockedOnActiveAxis &&
                recentActiveMovement >= Mathf.Max(
                    0.05f,
                    dragTurnProbeResetCells) &&
                recentActiveMovement * dominance >=
                recentPerpendicularMovement)
            {
                _dragTurnProbeBoardLocalPoint = pointerLocal;
                return false;
            }

            if (recentPerpendicularMovement < turnThreshold ||
                (!blockedOnActiveAxis &&
                 recentPerpendicularMovement + 0.0001f <
                 recentActiveMovement * dominance))
                return false;

            // When blocked, turn on the current cell immediately (no need to
            // advance along the dead axis first).
            Vector2Int corner = segmentStart;
            if (!blockedOnActiveAxis)
            {
                Vector3 segmentDelta =
                    pointerLocal - _dragSegmentBoardLocalPoint;
                float alongCells = _dragActiveAxis == 1
                    ? -segmentDelta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ)
                    : segmentDelta.x / Mathf.Max(0.0001f, _grid.GridSpacingX);

                int cornerSteps = CountCornerDragSteps(alongCells);
                int direction = alongCells >= 0f ? 1 : -1;
                int cornerRow = segmentStart.x;
                int cornerColumn = segmentStart.y;
                if (cornerSteps > 0)
                {
                    // A fast turn can request a corner several cells away in one
                    // frame. Validate every intermediate cell; checking only the
                    // final corner allowed Level 13's yellow plates to be jumped.
                    cornerSteps = ClampCornerStepsBeforeMovementBlocker(
                        segmentStart,
                        _dragActiveAxis,
                        direction,
                        cornerSteps);

                    if (_dragActiveAxis == 1)
                        cornerRow = segmentStart.x + direction * cornerSteps;
                    else
                        cornerColumn =
                            segmentStart.y + direction * cornerSteps;
                }

                corner = new Vector2Int(
                    Mathf.Clamp(cornerRow, 0, _grid.Rows - 1),
                    Mathf.Clamp(cornerColumn, 0, _grid.Columns - 1));
                if (!_grid.TryGetCell(
                        corner.x,
                        corner.y,
                        out PuzzleCell cornerCell) ||
                    cornerCell == null ||
                    IsDragPathBlocker(
                        corner.x,
                        corner.y,
                        GetRequiredCollectColor()))
                {
                    corner = segmentStart;
                    if (!_grid.TryGetCell(
                            corner.x,
                            corner.y,
                            out cornerCell) ||
                        cornerCell == null)
                        return false;
                }
            }
            else if (!_grid.TryGetCell(
                         corner.x,
                         corner.y,
                         out PuzzleCell blockedCornerCell) ||
                     blockedCornerCell == null)
            {
                return false;
            }

            Vector2Int previousCorner =
                _dragRouteCorners[_dragRouteCorners.Count - 1];
            if (corner != previousCorner)
                _dragRouteCorners.Add(corner);

            int nextAxis = _dragActiveAxis == 1 ? 2 : 1;
            Vector3 cornerBoardLocal = _grid.GetLocalPosition(corner.x, corner.y);
            _dragActiveAxis = nextAxis;
            _lockedDragAxis = _dragActiveAxis;
            _dragSegmentBoardLocalPoint = cornerBoardLocal;
            _dragTurnProbeBoardLocalPoint = pointerLocal;
            _trailSegmentProgressReported = 0f;
            _dragCornerTransitionActive = false;
            _dragCornerTransitionElapsed = 0f;
            _dragCornerTransitionStart =
                _cylinder.transform.localPosition;
            _dragCornerTransitionTarget =
                GetPieceLocalPosition(_cylinder, corner.x, corner.y);
            RefreshStickmanAnimation(moving: true);
            return true;
        }

        private int ClampCornerStepsBeforeMovementBlocker(
            Vector2Int segmentStart,
            int axis,
            int direction,
            int requestedSteps)
        {
            if (_grid == null || requestedSteps <= 0 || direction == 0)
                return 0;

            PieceColorType requiredColor = GetRequiredCollectColor();
            int safeSteps = 0;
            int row = segmentStart.x;
            int column = segmentStart.y;

            for (int distance = 1; distance <= requestedSteps; distance++)
            {
                if (axis == 1)
                    row += direction;
                else
                    column += direction;

                if (!_grid.IsInside(row, column) ||
                    !_grid.TryGetCell(row, column, out PuzzleCell cell) ||
                    cell == null ||
                    IsDragPathBlocker(row, column, requiredColor))
                    break;

                safeSteps = distance;
                if (requiredColor == PieceColorType.None &&
                    TryResolveCollectiblePlate(
                        GetCellBoardPiece(row, column),
                        row,
                        column,
                        requiredColor,
                        out CarryBlockJamBoardPiece collectPlate) &&
                    collectPlate != null)
                {
                    requiredColor = collectPlate.Color;
                }
            }

            return safeSteps;
        }

        private bool TryGetActiveSegmentIntent(
            Vector2 screenPosition,
            int startRow,
            int startColumn,
            out int rowStep,
            out int columnStep,
            out int requestedSteps)
        {
            return TryGetActiveSegmentIntent(
                screenPosition,
                startRow,
                startColumn,
                commitOnRelease: false,
                out rowStep,
                out columnStep,
                out requestedSteps);
        }

        private bool TryGetActiveSegmentIntent(
            Vector2 screenPosition,
            int startRow,
            int startColumn,
            bool commitOnRelease,
            out int rowStep,
            out int columnStep,
            out int requestedSteps)
        {
            rowStep = 0;
            columnStep = 0;
            requestedSteps = 0;
            if (_dragActiveAxis == 0 ||
                !TryProjectPointerToBoardLocal(
                    screenPosition,
                    out Vector3 pointerLocal))
                return false;

            Vector3 pointerDelta =
                pointerLocal - _dragSegmentBoardLocalPoint;
            float cells;
            float engage = Mathf.Max(0.05f, dragCellEngageThreshold);
            if (_dragActiveAxis == 1)
            {
                cells = -pointerDelta.z /
                    Mathf.Max(0.0001f, _grid.GridSpacingZ);
                if (Mathf.Abs(cells) < engage)
                    return false;

                rowStep = cells > 0f ? 1 : -1;
                requestedSteps = commitOnRelease
                    ? Mathf.Max(1, CountCommittedDragSteps(cells))
                    : CountFollowDragSteps(cells);
                requestedSteps = Mathf.Min(
                    requestedSteps,
                    rowStep > 0
                        ? _grid.Rows - 1 - startRow
                        : startRow);
            }
            else
            {
                cells = pointerDelta.x /
                    Mathf.Max(0.0001f, _grid.GridSpacingX);
                if (Mathf.Abs(cells) < engage)
                    return false;

                columnStep = cells > 0f ? 1 : -1;
                requestedSteps = commitOnRelease
                    ? Mathf.Max(1, CountCommittedDragSteps(cells))
                    : CountFollowDragSteps(cells);
                requestedSteps = Mathf.Min(
                    requestedSteps,
                    columnStep > 0
                        ? _grid.Columns - 1 - startColumn
                        : startColumn);
            }

            return requestedSteps > 0;
        }

        /// <summary>
        /// Path length while dragging — open cells for smooth follow, without
        /// the old Floor(abs)+1 edge overshoot. Cell claiming on release uses
        /// dragCellCrossThreshold hysteresis separately.
        /// </summary>
        private int CountFollowDragSteps(float signedCells)
        {
            float abs = Mathf.Abs(signedCells);
            float engage = Mathf.Max(0.05f, dragCellEngageThreshold);
            if (abs < engage)
                return 0;

            return Mathf.Max(1, Mathf.CeilToInt(abs));
        }

        /// <summary>
        /// Corner cell along a segment. Floor-biased so a 1-cell 2x2 edge
        /// cannot round forward into a 2nd extra cell.
        /// </summary>
        private static int CountCornerDragSteps(float signedCells)
        {
            float abs = Mathf.Abs(signedCells);
            if (abs < 0.35f)
                return 0;

            // Commit around 45% into the next cell — easy for tight turns,
            // still blocks Round(1.6)->2 overshoot.
            return Mathf.Max(0, Mathf.FloorToInt(abs + 0.55f));
        }

        /// <summary>
        /// Final cell count on release.
        /// </summary>
        private int CountCommittedDragSteps(float signedCells)
        {
            float abs = Mathf.Abs(signedCells);
            if (abs < 0.0001f)
                return 0;

            float commit = Mathf.Clamp(dragCellCommitThreshold, 0.5f, 0.95f);
            int steps = Mathf.FloorToInt(abs + (1f - commit));
            return Mathf.Max(0, steps);
        }

        private void UpdateDraggedCylinderPosition(
            Vector2 screenPosition,
            int rowStep,
            int columnStep,
            List<Vector2Int> path)
        {
            UpdateDraggedCylinderPosition(
                screenPosition,
                rowStep,
                columnStep,
                path,
                _swipeStartRow,
                _swipeStartColumn,
                _dragStartBoardLocalPoint);
        }

        private void UpdateDraggedCylinderPosition(
            Vector2 screenPosition,
            int rowStep,
            int columnStep,
            List<Vector2Int> path,
            int startRow,
            int startColumn,
            Vector3 pointerOrigin)
        {
            if (_cylinder == null ||
                !TryProjectPointerToBoardLocal(screenPosition, out Vector3 pointerLocal))
            {
                if (_cylinder != null &&
                    _grid != null &&
                    _grid.IsInside(startRow, startColumn))
                {
                    Vector3 targetPos = GetPieceLocalPosition(_cylinder, startRow, startColumn);
                    float speed = Mathf.Max(1f, dragFollowSpeed);
                    float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
                    _cylinder.transform.localPosition = Vector3.Lerp(
                        _cylinder.transform.localPosition,
                        targetPos,
                        t);
                }
                RefreshStickmanAnimation(moving: false);
                return;
            }

            Vector3 pointerDelta = pointerLocal - pointerOrigin;
            float draggedCells = rowStep != 0
                ? -pointerDelta.z / Mathf.Max(0.0001f, _grid.GridSpacingZ)
                : pointerDelta.x / Mathf.Max(0.0001f, _grid.GridSpacingX);
            float directedProgress = Mathf.Max(
                0f,
                draggedCells * (rowStep != 0 ? rowStep : columnStep));

            TryCollectAlongDragSegment(
                startRow,
                startColumn,
                rowStep,
                columnStep,
                directedProgress);

            ApplyDraggedCylinderPosition(
                directedProgress,
                rowStep,
                columnStep,
                TrimPathBeforeMovementBlocker(path),
                startRow,
                startColumn);

            TryCollectPlatesUnderCharTable();
            TryMagnetDeliverAtGateDuringDrag();
        }

        private void ApplyDraggedCylinderPosition(
            float directedProgress,
            int rowStep,
            int columnStep,
            List<Vector2Int> path,
            int startRow,
            int startColumn)
        {
            if (_cylinder == null)
                return;

            if (path == null || path.Count == 0)
            {
                if (_grid != null && _grid.IsInside(startRow, startColumn))
                {
                    Vector3 targetPos = GetPieceLocalPosition(_cylinder, startRow, startColumn);
                    float speed = Mathf.Max(1f, dragFollowSpeed);
                    float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
                    _cylinder.transform.localPosition = Vector3.Lerp(
                        _cylinder.transform.localPosition,
                        targetPos,
                        t);
                }

                RefreshStickmanAnimation(moving: false);
                SetCharTableTrailEmitting(false);
                UpdateDragFollowCellFromVisual();
                return;
            }

            // Never advance past the last walkable cell — tables and
            // non-matching plates stay solid even on very fast finger movement.
            float maxProgress = path.Count;
            PieceColorType requiredColor = GetRequiredCollectColor();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int step = path[i];
                if (IsBoxOwnedCell(step.x, step.y) ||
                    IsDragPathBlocker(step.x, step.y, requiredColor))
                {
                    maxProgress = i;
                    break;
                }

                if (TryResolveCollectiblePlate(
                        GetCellBoardPiece(step.x, step.y),
                        step.x,
                        step.y,
                        requiredColor,
                        out CarryBlockJamBoardPiece collectPlate) &&
                    requiredColor == PieceColorType.None &&
                    collectPlate != null)
                {
                    requiredColor = collectPlate.Color;
                }

            }

            // Final swept safety check independent of PuzzleCell.Occupant and the
            // preview path. A very large pointer delta can span many cells in one
            // frame, so inspect every crossed Row/Column registration directly.
            float plateLimitedProgress = ComputePlateLimitedDragProgress(
                startRow,
                startColumn,
                rowStep,
                columnStep,
                directedProgress,
                GetRequiredCollectColor());
            float clampedProgress = Mathf.Min(
                directedProgress,
                Mathf.Min(maxProgress, plateLimitedProgress));
            Vector3 targetPosition = EvaluateDragPathPosition(
                startRow,
                startColumn,
                path,
                clampedProgress);

            float followSpeed = Mathf.Max(1f, dragFollowSpeed);
            float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

            Vector3 currentPos = _cylinder.transform.localPosition;
            Vector3 nextPos;
            if (rowStep != 0)
            {
                // Lock X strictly to target line for sharp 90-degree orthogonal turns
                float newZ = Mathf.Lerp(currentPos.z, targetPosition.z, blend);
                nextPos = new Vector3(targetPosition.x, targetPosition.y, newZ);
            }
            else if (columnStep != 0)
            {
                // Lock Z strictly to target line for sharp 90-degree orthogonal turns
                float newX = Mathf.Lerp(currentPos.x, targetPosition.x, blend);
                nextPos = new Vector3(newX, targetPosition.y, targetPosition.z);
            }
            else
            {
                nextPos = Vector3.Lerp(currentPos, targetPosition, blend);
            }

            _cylinder.transform.localPosition = nextPos;
            CollectFreestandingPlatesThroughDragProgress(
                startRow,
                startColumn,
                rowStep,
                columnStep,
                GetDirectedProgressToPosition(
                    startRow,
                    startColumn,
                    rowStep,
                    columnStep,
                    nextPos));
            FaceStickmanToward(targetPosition);
            RefreshStickmanAnimation(moving: clampedProgress > 0.01f);
            UpdateCharTableTrailDuringDrag(clampedProgress);
            UpdateDragFollowCellFromVisual();
        }

        private float ComputePlateLimitedDragProgress(
            int startRow,
            int startColumn,
            int rowStep,
            int columnStep,
            float directedProgress,
            PieceColorType requiredColor)
        {
            if (_grid == null ||
                directedProgress <= 0f ||
                (rowStep == 0 && columnStep == 0))
                return Mathf.Max(0f, directedProgress);

            int cellsToScan = Mathf.CeilToInt(directedProgress);
            int row = startRow;
            int column = startColumn;

            for (int distance = 1; distance <= cellsToScan; distance++)
            {
                row += rowStep;
                column += columnStep;
                if (!_grid.IsInside(row, column))
                    return distance - 1;

                CarryBlockJamBoardPiece plate =
                    FindFreestandingPlateAtCell(row, column);
                if (plate == null)
                    continue;

                // Matching plates are collected during the visual sweep and do not
                // limit movement. Only a different/non-collectible plate blocks.
                bool canCollect = HasCollectiblePlateAt(row, column, requiredColor);
                if (!canCollect)
                    return distance - 1;

                if (requiredColor == PieceColorType.None)
                    requiredColor = plate.Color;
            }

            return directedProgress;
        }

        private float GetDirectedProgressToPosition(
            int startRow,
            int startColumn,
            int rowStep,
            int columnStep,
            Vector3 localPosition)
        {
            Vector3 startPosition =
                GetPieceLocalPosition(_cylinder, startRow, startColumn);
            if (rowStep != 0)
            {
                float rowCells = -(localPosition.z - startPosition.z) /
                    Mathf.Max(0.0001f, _grid.GridSpacingZ);
                return Mathf.Max(0f, rowCells * rowStep);
            }

            float columnCells = (localPosition.x - startPosition.x) /
                Mathf.Max(0.0001f, _grid.GridSpacingX);
            return Mathf.Max(0f, columnCells * columnStep);
        }

        private void CollectFreestandingPlatesThroughDragProgress(
            int startRow,
            int startColumn,
            int rowStep,
            int columnStep,
            float reachedProgress)
        {
            if (_grid == null ||
                reachedProgress < 1f ||
                (rowStep == 0 && columnStep == 0))
                return;

            PieceColorType requiredColor = GetRequiredCollectColor();
            int reachedCells = Mathf.FloorToInt(reachedProgress + 0.0001f);
            int row = startRow;
            int column = startColumn;
            for (int distance = 1; distance <= reachedCells; distance++)
            {
                row += rowStep;
                column += columnStep;
                if (!_grid.IsInside(row, column))
                    break;

                CarryBlockJamBoardPiece plate =
                    FindFreestandingPlateAtCell(row, column);
                if (plate == null)
                    continue;
                if (!HasCollectiblePlateAt(row, column, requiredColor))
                    break;

                TryCollectAtCellDuringDrag(row, column);
                requiredColor = GetRequiredCollectColor();
            }
        }

        private Vector3 EvaluateDragPathPosition(
            int startRow,
            int startColumn,
            List<Vector2Int> path,
            float progress)
        {
            Vector3 previous = GetPieceLocalPosition(_cylinder, startRow, startColumn);
            if (path == null || path.Count == 0 || progress <= 0f)
                return previous;

            float remaining = progress;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 next = GetPieceLocalPosition(_cylinder, path[i].x, path[i].y);
                if (remaining <= 1f)
                    return Vector3.LerpUnclamped(previous, next, remaining);

                remaining -= 1f;
                previous = next;
            }

            return previous;
        }

        private List<Vector2Int> BuildPreviewPath(int rowStep, int columnStep, int requestedSteps)
        {
            return BuildPreviewPath(
                _cylinder.Row,
                _cylinder.Column,
                rowStep,
                columnStep,
                requestedSteps);
        }

        private List<Vector2Int> BuildPreviewPath(
            int startRow,
            int startColumn,
            int rowStep,
            int columnStep,
            int requestedSteps)
        {
            var path = new List<Vector2Int>();
            int row = startRow;
            int column = startColumn;
            PieceColorType pathCollectColor = GetRequiredCollectColor();

            for (int stepIndex = 0; stepIndex < requestedSteps; stepIndex++)
            {
                int nextRow = row + rowStep;
                int nextColumn = column + columnStep;
                if (!_grid.IsInside(nextRow, nextColumn) ||
                    !_grid.TryGetCell(nextRow, nextColumn, out PuzzleCell cell) ||
                    cell == null)
                    break;

                // Tables and different-color plates stop the path. Matching
                // freestanding plates stay walkable and are collected in the
                // swept visual traversal.
                if (IsDragPathBlocker(nextRow, nextColumn, pathCollectColor))
                    break;

                CarryBlockJamBoardPiece piece = GetCellBoardPiece(nextRow, nextColumn);
                if (piece != null &&
                    TryResolveCollectiblePlate(
                        piece,
                        nextRow,
                        nextColumn,
                        pathCollectColor,
                        out CarryBlockJamBoardPiece collectPlate) &&
                    pathCollectColor == PieceColorType.None)
                {
                    pathCollectColor = collectPlate.Color;
                }

                row = nextRow;
                column = nextColumn;
                path.Add(new Vector2Int(nextRow, nextColumn));
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

            if (_lockedDragAxis == 0)
                _lockedDragAxis = Mathf.Abs(columnCells) > Mathf.Abs(rowCells) ? 2 : 1;

            float engage = Mathf.Max(0.05f, dragCellEngageThreshold);
            if (_lockedDragAxis == 2)
            {
                if (Mathf.Abs(columnCells) < engage)
                    return false;

                columnStep = columnCells > 0f ? 1 : -1;
                rowStep = 0;
                requestedSteps = CountFollowDragSteps(columnCells);
            }
            else
            {
                if (Mathf.Abs(rowCells) < engage)
                    return false;

                rowStep = rowCells > 0f ? 1 : -1;
                columnStep = 0;
                requestedSteps = CountFollowDragSteps(rowCells);
            }

            // Prefer finger end-cell when it agrees with the projected direction,
            // but never allow it to jump ahead of the projected drag distance
            // (tilted camera + fat finger otherwise overshoots into 3x3 loops).
            if (TryGetNearestGridCell(screenPosition, out int endRow, out int endColumn))
            {
                if (rowStep != 0)
                {
                    int rowDelta = endRow - _swipeStartRow;
                    if (rowDelta * rowStep > 0)
                        requestedSteps = Mathf.Min(requestedSteps, Mathf.Abs(rowDelta));
                }
                else if (columnStep != 0)
                {
                    int columnDelta = endColumn - _swipeStartColumn;
                    if (columnDelta * columnStep > 0)
                        requestedSteps = Mathf.Min(requestedSteps, Mathf.Abs(columnDelta));
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

        private bool IsPointerOverCylinder(Vector2 screenPosition)
        {
            if (_cylinder == null)
                return false;

            if (_gameplayCamera == null)
                ResolveGameplayReferences();

            if (_gameplayCamera == null)
                return false;

            // Priority 1: Grid cell hit test (allows tapping/dragging directly on the character's cell)
            if (TryGetNearestGridCell(screenPosition, out int cellRow, out int cellColumn))
            {
                if (cellRow == _cylinder.Row && cellColumn == _cylinder.Column)
                    return true;
            }

            Renderer[] renderers = _cylinder.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds worldBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
                return false;

            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            bool hasVisibleCorner = false;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(
                            extents,
                            new Vector3(x, y, z));
                        Vector3 projected = _gameplayCamera.WorldToScreenPoint(corner);
                        if (projected.z <= 0f)
                            continue;

                        hasVisibleCorner = true;
                        minX = Mathf.Min(minX, projected.x);
                        minY = Mathf.Min(minY, projected.y);
                        maxX = Mathf.Max(maxX, projected.x);
                        maxY = Mathf.Max(maxY, projected.y);
                    }
                }
            }

            float dpiScale = Screen.dpi > 0f ? (Screen.dpi / 160f) : (Screen.width / 1080f);
            float selectionPaddingPixels = Mathf.Clamp(36f * dpiScale, 24f, 120f);

            return hasVisibleCorner &&
                   screenPosition.x >= minX - selectionPaddingPixels &&
                   screenPosition.x <= maxX + selectionPaddingPixels &&
                   screenPosition.y >= minY - selectionPaddingPixels &&
                   screenPosition.y <= maxY + selectionPaddingPixels;
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

            if (!TryProjectPointerToBoardLocal(screenPosition, out Vector3 localPos))
                return false;

            column = Mathf.RoundToInt(localPos.x / _grid.GridSpacingX + (_grid.Columns - 1) * 0.5f);
            row = Mathf.RoundToInt((_grid.Rows - 1) * 0.5f - localPos.z / _grid.GridSpacingZ);

            row = Mathf.Clamp(row, 0, _grid.Rows - 1);
            column = Mathf.Clamp(column, 0, _grid.Columns - 1);
            return true;
        }

        private bool TryProjectPointerToBoardLocal(
            Vector2 screenPosition,
            out Vector3 localPosition)
        {
            localPosition = default;
            if (_gameplayCamera == null)
                ResolveGameplayReferences();

            if (_gameplayCamera == null || board == null)
                return false;

            Ray ray = _gameplayCamera.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(board.transform.up, board.transform.position);
            if (!plane.Raycast(ray, out float distance))
                return false;

            Vector3 worldPoint = ray.GetPoint(distance);
            localPosition = board.transform.InverseTransformPoint(worldPoint);
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
            return IsCollectiblePlate(piece, atRow, atColumn, GetRequiredCollectColor());
        }

        private PieceColorType GetRequiredCollectColor()
        {
            if (HasCarriedPlates)
            {
                _dragCollectColor = CarriedColor;
                return CarriedColor;
            }

            return _dragCollectColor;
        }

        private bool IsCollectiblePlate(
            CarryBlockJamBoardPiece piece,
            int atRow,
            int atColumn,
            PieceColorType requiredColor)
        {
            if (piece == null || piece.Kind != CarryBlockJamPieceKind.Plate)
                return false;

            if (piece.IsFrozen || piece.IsColorHidden || piece.IsCurtained)
                return false;

            int checkRow = atRow >= 0 ? atRow : piece.Row;
            int checkColumn = atColumn >= 0 ? atColumn : piece.Column;
            if (TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                !TutorialManager.Instance.CanCollectTutorialPlate(checkRow, checkColumn))
                return false;

            if (requiredColor != PieceColorType.None && piece.Color != requiredColor)
                return false;

            return true;
        }

        private bool TryCollectPlatesFromOccupant(
            CarryBlockJamBoardPiece occupant,
            int row,
            int column,
            List<CarryBlockJamBoardPiece> pickupPieces)
        {
            if (pickupPieces == null)
                return false;

            if (!TryResolveCollectiblePlate(
                    occupant,
                    row,
                    column,
                    GetRequiredCollectColor(),
                    out CarryBlockJamBoardPiece plate))
                return false;

            if (_dragCollectColor == PieceColorType.None)
                _dragCollectColor = plate.Color;

            List<CarryBlockJamBoardPiece> extracted = ExtractPickupPlates(plate, row, column);
            if (extracted == null || extracted.Count == 0)
                return false;

            for (int i = 0; i < extracted.Count; i++)
            {
                if (extracted[i] != null)
                    pickupPieces.Add(extracted[i]);
            }

            return true;
        }

        private void TryCollectAtCellDuringDrag(int row, int column)
        {
            if (IsTableCollectOnCooldown(row, column))
                return;

            CarryBlockJamBoardPiece occupant = GetCellBoardPiece(row, column);
            if (occupant == null)
                return;

            var pickupPieces = new List<CarryBlockJamBoardPiece>();
            if (!TryCollectPlatesFromOccupant(occupant, row, column, pickupPieces))
                return;

            if (pickupPieces.Count > 0)
            {
                if (IsBoxOwnedCell(row, column))
                    MarkTablePickupBlocksDeliver(row, column);
                AddPlatesToCarryStack(
                    pickupPieces,
                    pickupRow: row,
                    pickupColumn: column,
                    fromTable: IsBoxOwnedCell(row, column));
            }
        }

        private void TryCollectAlongDragSegment(
            int startRow,
            int startColumn,
            int rowStep,
            int columnStep,
            float directedProgress)
        {
            // Table stacks only (CharTable cannot enter box cells).
            // Freestanding plates collect via TryCollectPlatesUnderCharTable when
            // CharTable overlaps their cell — supports quick multi-pickup.
            if ((rowStep == 0 && columnStep == 0) || _grid == null)
                return;

            // Only the orthogonal neighbor of CharTable's current cell — never
            // vacuum a table from 2+ cells ahead while still traveling.
            int fromRow = _dragFollowCell.x;
            int fromColumn = _dragFollowCell.y;
            if (!_grid.IsInside(fromRow, fromColumn))
            {
                fromRow = startRow;
                fromColumn = startColumn;
            }

            int tableRow = fromRow + rowStep;
            int tableColumn = fromColumn + columnStep;
            if (!_grid.IsInside(tableRow, tableColumn) ||
                !IsBoxOwnedCell(tableRow, tableColumn))
                return;

            // Finger must press from the approach cell into the table (not merely
            // arrive one cell away).
            int stepsToApproach =
                Mathf.Abs(fromRow - startRow) + Mathf.Abs(fromColumn - startColumn);
            const float tablePressThreshold = 0.35f;
            if (directedProgress < stepsToApproach + tablePressThreshold)
                return;

            PieceColorType requiredColor = GetRequiredCollectColor();
            if (!HasCollectiblePlateAt(tableRow, tableColumn, requiredColor))
                return;

            TryCollectAtCellDuringDrag(tableRow, tableColumn);
        }

        /// <summary>
        /// Pick up table stacks only when CharTable is on the approach cell and
        /// the drag presses into that table neighbor. Freestanding plates are not
        /// collected here.
        /// </summary>
        private void TryCollectNeighborInDragDirection(
            Vector2 screenPosition,
            int fromRow,
            int fromColumn,
            int rowStep,
            int columnStep)
        {
            if (rowStep == 0 && columnStep == 0)
                return;

            // Prefer CharTable's claimed cell so a long segment cannot pull from afar.
            if (_grid != null &&
                _grid.IsInside(_dragFollowCell.x, _dragFollowCell.y))
            {
                fromRow = _dragFollowCell.x;
                fromColumn = _dragFollowCell.y;
            }

            float along = MeasureFingerAlongAxis(
                screenPosition,
                fromRow,
                fromColumn,
                rowStep,
                columnStep);
            // Require a real press into the table — being beside it is not enough.
            if (along < 0.35f)
                return;

            int targetRow = fromRow + rowStep;
            int targetColumn = fromColumn + columnStep;
            if (!IsBoxOwnedCell(targetRow, targetColumn))
                return;

            if (!HasCollectiblePlateAt(
                    targetRow,
                    targetColumn,
                    GetRequiredCollectColor()))
                return;

            TryCollectAtCellDuringDrag(targetRow, targetColumn);
        }

        private bool IsMatchingDropTarget(CarryBlockJamBoardPiece piece)
        {
            if (piece == null || !HasCarriedPlates)
                return false;

            if (piece.Kind == CarryBlockJamPieceKind.Box)
                return CanDeliverToTable(piece);

            CarryBlockJamBoardPiece storageBox = GetStorageBox(piece);
            return CanDeliverToTable(storageBox);
        }

        /// <summary>
        /// Empty tables: any color unless color-accept locked.
        /// Occupied tables: only the stack color (and color-accept lock must still match).
        /// </summary>
        private bool CanDeliverToTable(CarryBlockJamBoardPiece tableBox)
        {
            if (tableBox == null || tableBox.Kind != CarryBlockJamPieceKind.Box)
                return false;

            if (tableBox.IsColorHidden || tableBox.IsFrozen || tableBox.IsCurtained)
                return false;

            if (!HasCarriedPlates)
                return false;

            CarryBlockJamColorAcceptTable colorAccept =
                tableBox.GetComponent<CarryBlockJamColorAcceptTable>();
            if (colorAccept != null && !colorAccept.AcceptsColor(CarriedColor))
                return false;

            PieceColorType stackColor = GetTableStackPlateColor(tableBox);
            if (stackColor == PieceColorType.None)
                return true;

            return stackColor == CarriedColor;
        }

        /// <summary>
        /// Color of plates already stacked on a table, or None when the table is empty.
        /// </summary>
        private static PieceColorType GetTableStackPlateColor(CarryBlockJamBoardPiece tableBox)
        {
            if (tableBox == null)
                return PieceColorType.None;

            CarryBlockJamBoardPiece current = tableBox.StackedAbove;
            while (current != null)
            {
                if (current.Kind == CarryBlockJamPieceKind.Plate)
                {
                    PieceColorType color = current.TrueColor;
                    if (color != PieceColorType.None)
                        return color;
                }

                current = current.StackedAbove;
            }

            return PieceColorType.None;
        }

        private bool HasCarriedPlates => _carriedPlates.Count > 0;

        private PieceColorType CarriedColor =>
            HasCarriedPlates ? _carriedPlates[0].Color : PieceColorType.None;

        private Tween AddPlateToCarryStack(
            CarryBlockJamBoardPiece plate,
            int pickupIndex,
            bool fromTable)
        {
            if (plate == null)
                return null;

            // Fast overlap checks can report the same floor plate more than once
            // before its first soar finishes. Never start competing tweens for a
            // plate that already belongs to the logical carry stack.
            if (_carriedPlates.Contains(plate))
                return null;

            plate.ClearStackLinks();
            Transform attachRoot = GetCarryAttachRoot();
            _carriedPlates.Add(plate);

            int stackIndex = _carriedPlates.IndexOf(plate);
            Vector3 targetLocal = GetCarriedPlateLocalPosition(stackIndex);
            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            if (spawner != null && spawner.UsesCharTableCylinderVisual)
            {
                // Stay on the board root during flight so CharTable drag/settle
                // cannot haul plates into the gap cell between table and CharTable.
                plate.transform.SetParent(GetPiecesRoot(), true);
                return AnimatePlateOntoCharTable(
                    plate,
                    pickupIndex,
                    targetLocal,
                    fromTable);
            }

            plate.transform.SetParent(attachRoot, true);
            Vector3 startPosition = plate.transform.localPosition;
            float animationSpeed = Mathf.Max(0.01f, pickupPlateAnimationSpeed);
            float bounceDuration = Mathf.Max(0.01f, pickupPlateBounceDuration / animationSpeed);
            float bounceHeight = Mathf.Max(0.01f, pickupPlateBounceHeight);
            Vector3 horizontalFromStickman = new Vector3(startPosition.x, 0f, startPosition.z);
            Vector3 outward = horizontalFromStickman.sqrMagnitude > 0.001f
                ? horizontalFromStickman.normalized * 0.2f
                : Vector3.right * (pickupIndex % 2 == 0 ? 0.2f : -0.2f);
            Vector3 liftPosition = startPosition + Vector3.up * bounceHeight + outward;
            Vector3 frontWaypoint = targetLocal +
                Vector3.forward * Mathf.Max(0f, pickupPlateFrontClearance) +
                Vector3.up * (bounceHeight * 0.45f);

            Sequence bounce = DOTween.Sequence();
            bounce.SetDelay(Mathf.Max(0f, pickupPlateStagger) * pickupIndex / animationSpeed);
            bounce.Append(plate.transform.DOLocalMove(
                liftPosition,
                bounceDuration * 0.35f).SetEase(Ease.OutQuad));
            bounce.Append(plate.transform.DOLocalPath(
                new[] { frontWaypoint, targetLocal },
                bounceDuration * 0.65f,
                PathType.CatmullRom,
                PathMode.Ignore).SetEase(Ease.InOutSine));
            bounce.Insert(0f, plate.transform.DOLocalRotate(
                Vector3.zero,
                bounceDuration,
                RotateMode.Fast).SetEase(Ease.OutQuad));
            float settleDuration = Mathf.Max(0.01f, pickupPlateSettleDuration / animationSpeed);
            bounce.Append(plate.transform.DOLocalJump(
                targetLocal,
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

        /// <summary>
        /// Shared curvy cubic soar for table ↔ CharTable transfers (tall arc, mid
        /// scale dip, light edge-on flip). End pose is sampled live so moving
        /// stack/CharTable targets stay locked during flight.
        /// </summary>
        private Tween BuildTableTransferSoarTween(
            CarryBlockJamBoardPiece plate,
            int index,
            System.Func<Vector3> getEndWorld,
            System.Func<Quaternion> getEndRotation,
            float arcHeightMul = 1f,
            float durationOverride = -1f)
        {
            float duration = durationOverride > 0f
                ? Mathf.Max(0.05f, durationOverride)
                : Mathf.Max(0.08f, charTableTablePickupDuration);
            float heightJitter = 0.08f * ((index % 3) - 1);
            float outwardJitter = 0.06f * ((index % 4) - 1.5f);
            float arcHeight =
                Mathf.Max(1.4f, charTablePickupArcHeight * 1.56f + heightJitter) *
                Mathf.Max(0.5f, arcHeightMul);
            float arcOutward = Mathf.Max(0.18f, charTablePickupArcOutward * 0.38f + outwardJitter);
            float midBlend = 0.28f + 0.03f * (index % 3);
            float mid2Blend = 0.68f;
            float sideBow = 0.12f + 0.05f * (index % 3);
            float shrinkMul = Mathf.Clamp(charTablePickupShrinkScale + 0.1f, 0.5f, 0.95f);
            float flipDegrees = Mathf.Clamp(charTablePickupFlipDegrees * 0.45f, 0f, 55f);
            float sideSign = index % 2 == 0 ? 1f : -1f;

            Vector3 restScale = plate.transform.localScale;
            if (restScale == Vector3.zero)
                restScale = Vector3.one;
            Vector3 shrunkScale = restScale * shrinkMul;
            Vector3 startWorld = plate.transform.position;
            Quaternion startRotation = plate.transform.rotation;
            float shrinkDuration = Mathf.Max(0.02f, charTablePickupShrinkDuration * 0.55f);

            Sequence placement = DOTween.Sequence();
            placement.Append(
                plate.transform.DOScale(shrunkScale, shrinkDuration).SetEase(Ease.OutBack));

            float flight = 0f;
            Tween pathTween = DOTween.To(
                    () => flight,
                    value => flight = value,
                    1f,
                    duration)
                .SetEase(Ease.InOutSine)
                .OnUpdate(() =>
                {
                    if (plate == null || getEndWorld == null || getEndRotation == null)
                        return;

                    Vector3 endWorld = getEndWorld();
                    Quaternion endRotation = getEndRotation();
                    Vector3 mid = Vector3.Lerp(startWorld, endWorld, midBlend);
                    mid.y = Mathf.Max(startWorld.y, endWorld.y) + arcHeight;

                    Vector3 mid2 = Vector3.Lerp(startWorld, endWorld, mid2Blend);
                    mid2.y = Mathf.Max(startWorld.y, endWorld.y) + arcHeight * 0.62f;

                    Vector3 away = startWorld - endWorld;
                    away.y = 0f;
                    Vector3 lateral = Vector3.right;
                    if (away.sqrMagnitude > 0.0001f)
                    {
                        away.Normalize();
                        mid += away * arcOutward;
                        mid2 += away * (arcOutward * 0.35f);
                        Vector3 side = Vector3.Cross(Vector3.up, away);
                        if (side.sqrMagnitude > 0.0001f)
                        {
                            side.Normalize();
                            lateral = side;
                            mid += side * (sideBow * sideSign);
                            mid2 += side * (sideBow * -0.55f * sideSign);
                        }
                    }
                    else
                    {
                        mid += Vector3.right * (0.25f * sideSign);
                        mid2 += Vector3.left * (0.15f * sideSign);
                    }

                    plate.transform.position = EvaluateCubicBezier(
                        startWorld, mid, mid2, endWorld, flight);

                    float apex = Mathf.Sin(flight * Mathf.PI);
                    plate.transform.localScale = Vector3.Lerp(
                        Vector3.Lerp(shrunkScale, restScale, flight),
                        shrunkScale,
                        apex * 0.55f);

                    Quaternion baseRotation = Quaternion.Slerp(startRotation, endRotation, flight);
                    if (flipDegrees > 0.01f)
                    {
                        Quaternion soarTilt = Quaternion.AngleAxis(flipDegrees * apex, lateral);
                        plate.transform.rotation = soarTilt * baseRotation;
                    }
                    else
                    {
                        plate.transform.rotation = baseRotation;
                    }
                });

            placement.Append(pathTween);
            placement.OnComplete(() =>
            {
                if (plate == null)
                    return;

                if (getEndWorld != null)
                    plate.transform.position = getEndWorld();
                if (getEndRotation != null)
                    plate.transform.rotation = getEndRotation();
                plate.transform.localScale = restScale;
            });
            return placement;
        }

        private Tween AnimatePlateOntoCharTable(
            CarryBlockJamBoardPiece plate,
            int pickupIndex,
            Vector3 targetLocal,
            bool fromTable)
        {
            // Ground uses the same tall soar as CharTable → table delivery, with a
            // longer flight so the arc reads clearly.
            float arcMul = fromTable ? 1f : charTableDropArcHeightMul;
            float duration = -1f;
            if (!fromTable)
            {
                CarryBlockJamPrefabSettings settings =
                    board != null ? board.PrefabSettings : null;
                duration = settings != null
                    ? settings.charTablePickupDuration
                    : charTablePickupDuration;
                duration = Mathf.Max(0.28f, duration);
            }

            return BuildTableTransferSoarTween(
                    plate,
                    pickupIndex,
                    () =>
                    {
                        Transform root = GetCarryAttachRoot();
                        return root != null
                            ? root.TransformPoint(targetLocal)
                            : plate.transform.position;
                    },
                    () =>
                    {
                        Transform root = GetCarryAttachRoot();
                        return root != null ? root.rotation : Quaternion.identity;
                    },
                    arcMul,
                    duration)
                .OnComplete(() =>
                {
                    if (plate == null)
                        return;

                    Transform root = GetCarryAttachRoot();
                    plate.transform.SetParent(root, false);
                    plate.transform.localPosition = targetLocal;
                    plate.transform.localRotation = Quaternion.identity;
                });
        }

        private static Vector3 EvaluateCubicBezier(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            return (uu * u * a) + (3f * uu * t * b) + (3f * u * tt * c) + (tt * t * d);
        }

        private void AddPlatesToCarryStack(
            List<CarryBlockJamBoardPiece> plates,
            int pickupRow = -1,
            int pickupColumn = -1,
            bool notifyTutorialComplete = false,
            bool fromTable = false)
        {
            if (plates == null || plates.Count == 0)
                return;

            // Heal any visual left behind by an older interrupted pickup before
            // adding the next logical batch.
            RemoveStaleCarryPlateVisuals();

            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            bool usesCharTable = spawner != null && spawner.UsesCharTableCylinderVisual;

            // DOTween sequences are immutable after they start. A fresh sequence
            // per pickup batch prevents rapid pickups from being inserted into a
            // locked sequence and leaving their plates floating in world space.
            Sequence collection = DOTween.Sequence();
            _plateCollectionTweens.Add(collection);
            collection.OnComplete(() => _plateCollectionTweens.Remove(collection));

            // Launch gap shorter than flight so the next plate is airborne before
            // the previous one lands — visible trail with spacing.
            float shrink = Mathf.Max(0.02f, charTablePickupShrinkDuration * 0.55f);
            float flight = Mathf.Max(0.08f, charTableTablePickupDuration);
            float launchGap = Mathf.Clamp(
                charTablePickupStagger,
                0.05f,
                (shrink + flight) * 0.5f);

            bool hasCollectionTween = false;
            for (int i = 0; i < plates.Count; i++)
            {
                CarryBlockJamBoardPiece plate = plates[i];
                Tween bounce = AddPlateToCarryStack(plate, i, fromTable);
                if (bounce != null)
                {
                    hasCollectionTween = true;
                    bounce.OnStart(() =>
                    {
                        Haptic.LightTaptic();
                        PlayCarrySfx(plateCollectSound);
                    });

                    if (usesCharTable)
                        collection.Insert(launchGap * i, bounce);
                    else
                        collection.Join(bounce);
                }
                else
                {
                    Haptic.LightTaptic();
                    PlayCarrySfx(plateCollectSound);
                }

                CarryBlockJamHiddenBox.NotifyPlateCollected(plate);
                CarryBlockJamHiddenPlate.NotifyPlateCollected(plate);
                CarryBlockJamFrozenBox.NotifyPlateCollected(plate);
                if (!fromTable)
                    CarryBlockJamFrozenPlate.NotifyPlateCollected(plate);
            }

            if (!hasCollectionTween)
            {
                collection.Kill();
                _plateCollectionTweens.Remove(collection);
            }
            EnsureCharTableTrail();
            RefreshCharTableTrailHeaviness();
            if (!_trackingSwipe)
                RefreshStickmanAnimation(moving: false);

            if (notifyTutorialComplete &&
                TutorialManager.Instance != null &&
                TutorialManager.Instance.IsActive &&
                pickupRow >= 0 &&
                TutorialManager.Instance.IsTutorialTargetCell(pickupRow, pickupColumn))
                TutorialManager.Instance.NotifyTutorialActionCompleted();

            if (!_trackingSwipe)
                EvaluateCarriedPlateDeadlock();
        }

        private static void PlayCarrySfx(string soundName)
        {
            if (string.IsNullOrEmpty(soundName) || SoundManager.instance == null)
                return;

            SoundManager.instance.PlayOneShot(soundName);
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

        private void RemoveStaleCarryPlateVisuals(
            ICollection<CarryBlockJamBoardPiece> platesInTransfer = null)
        {
            Transform attachRoot = GetCarryAttachRoot();
            if (attachRoot == null)
                return;

            CarryBlockJamBoardPiece[] attachedPieces =
                attachRoot.GetComponentsInChildren<CarryBlockJamBoardPiece>(true);
            for (int i = 0; i < attachedPieces.Length; i++)
            {
                CarryBlockJamBoardPiece plate = attachedPieces[i];
                if (plate == null ||
                    plate.Kind != CarryBlockJamPieceKind.Plate ||
                    _carriedPlates.Contains(plate) ||
                    (platesInTransfer != null && platesInTransfer.Contains(plate)))
                    continue;

                plate.transform.DOKill(false);
                plate.gameObject.SetActive(false);
                Destroy(plate.gameObject);
            }
        }

        private Vector3 GetCarriedPlateLocalPosition(int stackIndex)
        {
            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            bool usesCharTable = spawner != null && spawner.UsesCharTableCylinderVisual;
            CarryBlockJamPrefabSettings settings =
                board != null ? board.PrefabSettings : null;
            Vector3 baseOffset = usesCharTable
                ? settings != null
                    ? settings.charTablePlateOffset
                    : charTablePlateBaseOffset
                : carriedPlateBaseOffset;
            float stackStep = usesCharTable && settings != null
                ? settings.charTablePlateStackStep
                : carriedPlateStackStep;

            // Y = height on CharTable face; X/Z slide on the face only.
            return new Vector3(
                baseOffset.x,
                baseOffset.y + stackStep * stackIndex,
                baseOffset.z);
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
            Tween[] activeTweens = _plateCollectionTweens.ToArray();
            for (int i = 0; i < activeTweens.Length; i++)
            {
                Tween tween = activeTweens[i];
                if (tween != null && tween.IsActive())
                    tween.Complete();
            }

            _plateCollectionTweens.Clear();
            UpdateCarriedPlateVisuals();
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

        private bool UsesCharTableVisual()
        {
            if (_cylinder == null)
                return false;

            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            return spawner != null && spawner.UsesCharTableCylinderVisual;
        }

        private bool UsesCharTableTrail()
        {
            return enableCharTableTrail && UsesCharTableVisual();
        }

        private void NotifyCharTablePlayerInput()
        {
            _lastPlayerInputTime = Time.unscaledTime;
            StopCharTableIdleShake(restoreRestPose: true);
        }

        /// <summary>
        /// Called after CharTable/pieces are spawned for a level so the start hint always plays.
        /// </summary>
        public void NotifyLevelPiecesSpawned()
        {
            _cylinder = null;
            _charTableHintParticles = null;
            _charTableTrailParticles = null;
            _charTableStartHintPlayed = false;
            _lastPlayerInputTime = -1f;
            _charTableVisualRestCaptured = false;
            _failTriggered = false;
            _successTriggered = false;
            _failurePreparing = false;

            ResolveGameplayReferences();
            TryPlayCharTableStartHint(force: true);
        }

        private void UpdateCharTableIdleHints()
        {
            if (!enableCharTableIdleHints || _failTriggered || _successTriggered)
                return;

            if (_cylinder == null)
                ResolveGameplayReferences();

            if (!UsesCharTableVisual())
                return;

            TryPlayCharTableStartHint(force: false);

            if (_trackingSwipe || _isAnimating || _dragCornerTransitionActive)
            {
                _lastPlayerInputTime = Time.unscaledTime;
                StopCharTableIdleShake(restoreRestPose: true);
                return;
            }

            if (_lastPlayerInputTime < 0f)
                _lastPlayerInputTime = Time.unscaledTime;

            if (Time.unscaledTime - _lastPlayerInputTime < Mathf.Max(0.5f, charTableIdleShakeDelay))
                return;

            if (_charTableIdleShakeTween != null && _charTableIdleShakeTween.IsActive())
                return;

            PlayCharTableIdleShake();
            _lastPlayerInputTime = Time.unscaledTime;
        }

        private bool TryPlayCharTableStartHint(bool force)
        {
            if (!force && _charTableStartHintPlayed)
                return false;

            if (!PlayCharTableStartHint())
                return false;

            _charTableStartHintPlayed = true;
            _lastPlayerInputTime = Time.unscaledTime;
            return true;
        }

        private bool PlayCharTableStartHint()
        {
            if (!enableCharTableIdleHints || !UsesCharTableVisual())
                return false;

            if (_cylinder == null)
                ResolveGameplayReferences();
            if (_cylinder == null)
                return false;

            EnsureCharTableHintParticles();
            if (_charTableHintParticles == null)
                return false;

            ConfigureCharTableHintParticles(_charTableHintParticles);
            _charTableHintParticles.transform.position =
                _cylinder.transform.position + Vector3.up * 0.35f;
            _charTableHintParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _charTableHintParticles.Play(true);
            return true;
        }

        private void EnsureCharTableHintParticles()
        {
            if (_cylinder == null)
                return;

            if (_charTableHintParticles != null)
            {
                if (_charTableHintParticles.transform.parent != _cylinder.transform)
                    _charTableHintParticles.transform.SetParent(_cylinder.transform, false);
                return;
            }

            Transform existing = _cylinder.transform.Find("CharTableStartHint");
            GameObject hintObject = existing != null
                ? existing.gameObject
                : new GameObject("CharTableStartHint");
            if (existing == null)
                hintObject.transform.SetParent(_cylinder.transform, false);

            hintObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            hintObject.transform.localRotation = Quaternion.identity;
            hintObject.transform.localScale = Vector3.one;

            _charTableHintParticles = hintObject.GetComponent<ParticleSystem>();
            if (_charTableHintParticles == null)
                _charTableHintParticles = hintObject.AddComponent<ParticleSystem>();

            ConfigureCharTableHintParticles(_charTableHintParticles);
        }

        private void ConfigureCharTableHintParticles(ParticleSystem particles)
        {
            if (particles == null)
                return;

            float duration = Mathf.Max(0.4f, charTableStartHintDuration);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = duration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.97f, 0.82f, 1f),
                new Color(1f, 1f, 1f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;
            main.gravityModifier = -0.05f;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 36),
                new ParticleSystem.Burst(0.15f, 22),
                new ParticleSystem.Burst(0.35f, 14),
            });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.65f;
            shape.radiusThickness = 0.4f;
            shape.arc = 360f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.06f),
                    new GradientAlphaKey(1f, 0.45f),
                    new GradientAlphaKey(0.45f, 0.8f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.5f),
                    new Keyframe(0.22f, 1f),
                    new Keyframe(1f, 0.2f)));

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
                return;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Material circleMat = CreateCharTableSoftCircleMaterial();
            if (circleMat != null)
                renderer.sharedMaterial = circleMat;
        }

        private static Texture2D _charTableSoftCircleTexture;
        private static Material _charTableSoftCircleMaterial;

        private static Material CreateCharTableSoftCircleMaterial()
        {
            if (_charTableSoftCircleMaterial != null)
                return _charTableSoftCircleMaterial;

            Shader particleShader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Mobile/Particles/Additive") ??
                Shader.Find("Sprites/Default");
            if (particleShader == null)
                return null;

            Texture2D circle = GetCharTableSoftCircleTexture();
            _charTableSoftCircleMaterial = new Material(particleShader)
            {
                name = "CharTableSoftCircleParticleMat",
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = circle,
                color = Color.white
            };

            if (_charTableSoftCircleMaterial.HasProperty("_BaseMap"))
                _charTableSoftCircleMaterial.SetTexture("_BaseMap", circle);
            if (_charTableSoftCircleMaterial.HasProperty("_MainTex"))
                _charTableSoftCircleMaterial.SetTexture("_MainTex", circle);
            Color baseTint = new Color(1.25f, 1.25f, 1.25f, 1f);
            if (_charTableSoftCircleMaterial.HasProperty("_BaseColor"))
                _charTableSoftCircleMaterial.SetColor("_BaseColor", baseTint);
            if (_charTableSoftCircleMaterial.HasProperty("_Color"))
                _charTableSoftCircleMaterial.SetColor("_Color", baseTint);

            if (_charTableSoftCircleMaterial.HasProperty("_Surface"))
                _charTableSoftCircleMaterial.SetFloat("_Surface", 1f);
            if (_charTableSoftCircleMaterial.HasProperty("_Blend"))
                _charTableSoftCircleMaterial.SetFloat("_Blend", 0f);
            if (_charTableSoftCircleMaterial.HasProperty("_SrcBlend"))
                _charTableSoftCircleMaterial.SetFloat(
                    "_SrcBlend",
                    (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (_charTableSoftCircleMaterial.HasProperty("_DstBlend"))
                _charTableSoftCircleMaterial.SetFloat(
                    "_DstBlend",
                    (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (_charTableSoftCircleMaterial.HasProperty("_ZWrite"))
                _charTableSoftCircleMaterial.SetFloat("_ZWrite", 0f);
            if (_charTableSoftCircleMaterial.HasProperty("_Mode"))
                _charTableSoftCircleMaterial.SetFloat("_Mode", 2f);

            _charTableSoftCircleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _charTableSoftCircleMaterial.DisableKeyword("_ALPHATEST_ON");
            _charTableSoftCircleMaterial.SetOverrideTag("RenderType", "Transparent");
            _charTableSoftCircleMaterial.renderQueue = 3000;
            return _charTableSoftCircleMaterial;
        }

        private static Texture2D GetCharTableSoftCircleTexture()
        {
            if (_charTableSoftCircleTexture != null)
                return _charTableSoftCircleTexture;

            const int size = 128;
            _charTableSoftCircleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CharTableSoftCircleParticle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            float center = (size - 1) * 0.5f;
            float radius = center * 0.9f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha;
                    if (distance >= 1f)
                        alpha = 0f;
                    else if (distance < 0.55f)
                        alpha = 1f;
                    else
                    {
                        float t = (distance - 0.55f) / 0.45f;
                        alpha = 1f - (t * t * (3f - 2f * t));
                    }

                    _charTableSoftCircleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            _charTableSoftCircleTexture.Apply(false, true);
            return _charTableSoftCircleTexture;
        }

        private Transform GetCharTableShakeTarget()
        {
            Transform visual = GetStickmanVisual();
            return visual != null ? visual : _cylinder != null ? _cylinder.transform : null;
        }

        private void CaptureCharTableVisualRestPose()
        {
            Transform target = GetCharTableShakeTarget();
            if (target == null)
                return;

            _charTableVisualRestLocalPosition = target.localPosition;
            _charTableVisualRestCaptured = true;
        }

        private void PlayCharTableIdleShake()
        {
            Transform target = GetCharTableShakeTarget();
            if (target == null)
                return;

            StopCharTableIdleShake(restoreRestPose: false);
            if (!_charTableVisualRestCaptured)
                CaptureCharTableVisualRestPose();

            float duration = Mathf.Max(0.15f, charTableIdleShakeDuration);
            float strength = Mathf.Max(0.01f, charTableIdleShakeStrength);
            _charTableIdleShakeTween = target
                .DOShakePosition(
                    duration,
                    strength,
                    vibrato: 14,
                    randomness: 80f,
                    snapping: false,
                    fadeOut: true)
                .SetUpdate(UpdateType.Normal)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (target != null && _charTableVisualRestCaptured)
                        target.localPosition = _charTableVisualRestLocalPosition;
                    _charTableIdleShakeTween = null;
                });
        }

        private void StopCharTableIdleShake(bool restoreRestPose)
        {
            if (_charTableIdleShakeTween != null && _charTableIdleShakeTween.IsActive())
                _charTableIdleShakeTween.Kill();
            _charTableIdleShakeTween = null;

            if (!restoreRestPose || !_charTableVisualRestCaptured)
                return;

            Transform target = GetCharTableShakeTarget();
            if (target != null)
                target.localPosition = _charTableVisualRestLocalPosition;
        }

        private void EnsureCharTableTrail()
        {
            if (!UsesCharTableTrail() || _cylinder == null)
                return;

            // Remove legacy smoke TrailRenderer if a previous build left one around.
            Transform legacySmoke = _cylinder.transform.Find("CharTableSmokeTrail");
            if (legacySmoke != null)
                Destroy(legacySmoke.gameObject);

            Transform piecesRoot = GetPiecesRoot();
            if (piecesRoot == null)
                piecesRoot = _cylinder.transform;

            if (_charTableTrailParticles != null)
            {
                if (_charTableTrailConfigVersion != CharTableTrailExhaustConfigVersion)
                {
                    Destroy(_charTableTrailParticles.gameObject);
                    _charTableTrailParticles = null;
                }
                else
                {
                    Transform trailTransform = _charTableTrailParticles.transform;
                    if (trailTransform.parent != piecesRoot)
                        trailTransform.SetParent(piecesRoot, true);
                    ApplyCharTableTrailExhaustPose();
                    RefreshCharTableTrailHeaviness();
                    return;
                }
            }

            GameObject prefab = ResolveCharTableTrailVfxPrefab();
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[CarryBlockJam] CharTable trail prefab missing (SoapBubbleEmitter).");
                return;
            }

            // Destroy any old trail instances under CharTable or pieces root.
            DestroyNamedChild(_cylinder.transform, "CharTableWindTrail");
            DestroyNamedChild(_cylinder.transform, "CharTableBubbleTrail");
            DestroyNamedChild(piecesRoot, "CharTableWindTrail");
            DestroyNamedChild(piecesRoot, "CharTableBubbleTrail");

            // Parent to board pieces root (not CharTable) so CharTable's Y-squash
            // scale does not hide / flatten the exhaust particles.
            GameObject trailObject = Instantiate(prefab, piecesRoot, false);
            trailObject.name = "CharTableBubbleTrail";
            trailObject.SetActive(true);

            _charTableTrailParticles = trailObject.GetComponent<ParticleSystem>();
            if (_charTableTrailParticles == null)
                _charTableTrailParticles = trailObject.GetComponentInChildren<ParticleSystem>(true);
            if (_charTableTrailParticles == null)
                return;

            ConfigureCharTableTrailParticles(_charTableTrailParticles);
            _charTableTrailConfigVersion = CharTableTrailExhaustConfigVersion;
            _charTableTrailHasLastPos = false;
            ApplyCharTableTrailExhaustPose();
            SetCharTableTrailEmitting(false);
            RefreshCharTableTrailHeaviness();
        }

        private static void DestroyNamedChild(Transform parent, string childName)
        {
            if (parent == null)
                return;

            Transform existing = parent.Find(childName);
            if (existing != null)
                Destroy(existing.gameObject);
        }

        private GameObject ResolveCharTableTrailVfxPrefab()
        {
            if (charTableTrailVfxPrefab != null)
                return charTableTrailVfxPrefab;

            if (_cachedCharTableTrailVfxPrefab != null &&
                _cachedCharTableTrailVfxPrefab.name != "SoapBubbleEmitter")
                _cachedCharTableTrailVfxPrefab = null;

#if UNITY_EDITOR
            // Prefer the package prefab in editor — Resources copies can miss materials.
            if (_cachedCharTableTrailVfxPrefab == null)
            {
                _cachedCharTableTrailVfxPrefab =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                        CharTableTrailVfxEditorPath);
            }
#endif

            if (_cachedCharTableTrailVfxPrefab == null)
                _cachedCharTableTrailVfxPrefab =
                    Resources.Load<GameObject>(CharTableTrailVfxResourcePath);

            return _cachedCharTableTrailVfxPrefab;
        }

        private void ConfigureCharTableTrailParticles(ParticleSystem particles)
        {
            if (particles == null)
                return;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // SoapBubbleEmitter: keep bubble materials/subemitters, retune the root
            // emitter into a one-sided rearward exhaust jet.
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startDelay = 0f;
            main.gravityModifier = 0f;
            main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

            // Mild gray soap tint so bubbles still read as bubbles.
            Color bubble = new Color(0.62f, 0.66f, 0.7f, 0.9f);
            main.startColor = new ParticleSystem.MinMaxGradient(bubble);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Max(0f, charTableTrailMinEmission * 0.35f);
            emission.rateOverDistance = Mathf.Max(0f, charTableTrailRateOverDistance);
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.03f;
            shape.radiusThickness = 1f;
            shape.length = 0.08f;
            shape.arc = 360f;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
            shape.scale = Vector3.one;
            shape.alignToDirection = false;
            shape.randomDirectionAmount = 0f;
            shape.sphericalDirectionAmount = 0f;
            shape.randomPositionAmount = 0f;

            // Push bubbles only rearward from the cone (+Z).
            float speed = Mathf.Max(0.8f, charTableTrailExhaustSpeed);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);

            float size = Mathf.Max(0.18f, charTableTrailExhaustSize);
            main.startSize3D = false;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);

            float life = Mathf.Max(0.12f, charTableTrailMinLifetime);
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.85f, life);
            main.maxParticles = 220;

            ParticleSystem.NoiseModule noise = particles.noise;
            // Keep a little authored drift if present, but don't let it spray sideways.
            if (noise.enabled)
            {
                noise.strength = new ParticleSystem.MinMaxCurve(
                    Mathf.Min(noise.strength.constant, 0.08f));
                noise.strengthMultiplier = Mathf.Min(noise.strengthMultiplier, 0.2f);
            }

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 100;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.lengthScale = 1f;
                renderer.velocityScale = 0f;
                renderer.cameraVelocityScale = 0f;
                renderer.maxParticleSize = 5f;
                renderer.pivot = Vector3.zero;
                renderer.alignment = ParticleSystemRenderSpace.View;
            }

            // Soft-configure child emitters (bubble/droplet subfx) for world space.
            ParticleSystem[] children =
                particles.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < children.Length; i++)
            {
                ParticleSystem child = children[i];
                if (child == null || child == particles)
                    continue;

                ParticleSystem.MainModule childMain = child.main;
                childMain.playOnAwake = false;
                childMain.simulationSpace = ParticleSystemSimulationSpace.World;
                childMain.scalingMode = ParticleSystemScalingMode.Hierarchy;

                ParticleSystemRenderer childRenderer =
                    child.GetComponent<ParticleSystemRenderer>();
                if (childRenderer != null)
                {
                    childRenderer.shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;
                    childRenderer.receiveShadows = false;
                    childRenderer.sortingOrder = 101;
                }
            }

            float scale = Mathf.Max(0.55f, charTableTrailVfxScale * 0.75f);
            particles.transform.localScale = Vector3.one * scale;
        }

        private void ApplyCharTableTrailExhaustPose()
        {
            if (_charTableTrailParticles == null || _cylinder == null)
                return;

            Vector3 back = _charTableTrailExhaustDir;
            back.y = 0f;
            if (back.sqrMagnitude < 0.0001f)
                back = Vector3.back;
            else
                back.Normalize();

            Transform trailTransform = _charTableTrailParticles.transform;
            trailTransform.position =
                _cylinder.transform.position +
                Vector3.up * charTableTrailHeight +
                back * Mathf.Max(0f, charTableTrailRearOffset);
            // Cone emits along +Z — aim that axis opposite travel (exhaust).
            trailTransform.rotation = Quaternion.LookRotation(back, Vector3.up);
            float scale = Mathf.Max(0.55f, charTableTrailVfxScale * 0.75f);
            trailTransform.localScale = Vector3.one * scale;
        }

        private void UpdateCharTableTrailDuringDrag(float segmentProgress)
        {
            if (!UsesCharTableTrail())
            {
                AbsorbCharTableTrail();
                return;
            }

            EnsureCharTableTrail();
            if (_charTableTrailParticles == null || _cylinder == null)
                return;

            Vector3 worldPos = _cylinder.transform.position;
            bool movedThisFrame = false;
            float moveDistance = 0f;
            if (_charTableTrailHasLastPos)
            {
                Vector3 move = worldPos - _charTableTrailLastWorldPos;
                move.y = 0f;
                moveDistance = move.magnitude;
                // Ignore tiny settle/jitter while CharTable is parked on a cell.
                float moveThreshold = 0.0015f;
                if (_grid != null)
                    moveThreshold = Mathf.Max(0.0015f, _grid.GridSpacingX * 0.012f);

                if (moveDistance > moveThreshold)
                {
                    _charTableTrailExhaustDir = -move.normalized;
                    movedThisFrame = true;
                }
            }
            else
            {
                _charTableTrailHasLastPos = true;
            }

            _charTableTrailLastWorldPos = worldPos;
            ApplyCharTableTrailExhaustPose();

            float progress = Mathf.Max(0f, segmentProgress);
            float delta = Mathf.Max(0f, progress - _trailSegmentProgressReported);
            // Segment resets to 0 on turns — don't subtract / wipe heaviness.
            if (progress + 0.0001f < _trailSegmentProgressReported)
                delta = progress;
            _trailSegmentProgressReported = progress;

            // Trail only while CharTable is actually translating — finger-down but
            // parked on a cell must not keep emitting.
            if (movedThisFrame)
            {
                if (delta > 0f)
                    _trailSessionCells += delta;
                else if (_grid != null && _grid.GridSpacingX > 0.01f)
                    _trailSessionCells += moveDistance / _grid.GridSpacingX;

                RefreshCharTableTrailHeaviness();
                KillCharTableTrailAbsorbTween();
                SetCharTableTrailEmitting(true);
            }
            else
            {
                RefreshCharTableTrailHeaviness();
                SetCharTableTrailEmitting(false);
            }
        }

        private void RefreshCharTableTrailHeaviness()
        {
            if (_charTableTrailParticles == null)
                return;

            float maxCells = Mathf.Max(1f, charTableTrailCellsForMaxHeavy);
            // Step up per completed cell so each cell visibly lengthens / densifies.
            float cellSteps = Mathf.Floor(Mathf.Max(0f, _trailSessionCells));
            float heavy = Mathf.Clamp01(cellSteps / maxCells);
            float eased = Mathf.SmoothStep(0f, 1f, heavy);

            ParticleSystem.MainModule main = _charTableTrailParticles.main;

            float lifetime = Mathf.Lerp(
                Mathf.Max(0.12f, charTableTrailMinLifetime),
                Mathf.Max(0.2f, charTableTrailMaxLifetime),
                eased);
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                lifetime * 0.85f,
                lifetime);

            float speed = Mathf.Lerp(
                Mathf.Max(0.8f, charTableTrailExhaustSpeed),
                Mathf.Max(1f, charTableTrailMaxExhaustSpeed),
                eased);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);

            float size = Mathf.Lerp(
                Mathf.Max(0.18f, charTableTrailExhaustSize),
                Mathf.Max(0.25f, charTableTrailMaxExhaustSize),
                eased);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.72f, size);
            main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(60f, 140f, eased));

            float emission = Mathf.Lerp(
                Mathf.Max(2f, charTableTrailMinEmission * 0.45f),
                Mathf.Max(8f, charTableTrailMaxEmission),
                eased);
            float distanceRate = Mathf.Lerp(
                Mathf.Max(0f, charTableTrailRateOverDistance * 0.55f),
                Mathf.Max(0f, charTableTrailMaxRateOverDistance),
                eased);
            ParticleSystem.EmissionModule emissionModule = _charTableTrailParticles.emission;
            emissionModule.rateOverTime = emission;
            emissionModule.rateOverDistance = distanceRate;

            float scale = Mathf.Max(0.6f, charTableTrailVfxScale * 0.8f) *
                          Mathf.Lerp(0.95f, 1.05f, eased);
            _charTableTrailParticles.transform.localScale = Vector3.one * scale;
        }

        private void SetCharTableTrailEmitting(bool emitting)
        {
            if (_charTableTrailParticles == null)
                return;

            bool shouldEmit = emitting && UsesCharTableTrail();
            if (shouldEmit)
            {
                if (!_charTableTrailParticles.isPlaying)
                    _charTableTrailParticles.Play(true);
            }
            else if (_charTableTrailParticles.isEmitting)
            {
                _charTableTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void AbsorbCharTableTrail()
        {
            if (_charTableTrailParticles == null)
                return;

            _charTableTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_charTableTrailAbsorbTween != null && _charTableTrailAbsorbTween.IsActive())
                return;

            if (!_charTableTrailParticles.IsAlive(true))
            {
                _charTableTrailHasLastPos = false;
                return;
            }

            float duration = Mathf.Max(0.04f, charTableTrailAbsorbDuration);
            ParticleSystem.EmissionModule emissionModule = _charTableTrailParticles.emission;
            float startRate = emissionModule.rateOverTime.constant;
            if (startRate <= 0.01f)
                startRate = Mathf.Max(0.01f, charTableTrailMinEmission);
            float startDistance = emissionModule.rateOverDistance.constant;

            _charTableTrailAbsorbTween = DOTween
                .To(
                    () => 0f,
                    t =>
                    {
                        if (_charTableTrailParticles == null)
                            return;

                        float remain = (1f - t) * (1f - t);
                        ParticleSystem.EmissionModule emission =
                            _charTableTrailParticles.emission;
                        emission.rateOverTime = startRate * remain;
                        emission.rateOverDistance = startDistance * remain;
                    },
                    1f,
                    duration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    if (_charTableTrailParticles != null)
                    {
                        _charTableTrailParticles.Stop(
                            true,
                            ParticleSystemStopBehavior.StopEmittingAndClear);
                        RefreshCharTableTrailHeaviness();
                    }

                    _charTableTrailHasLastPos = false;
                    _charTableTrailAbsorbTween = null;
                });
        }

        private void KillCharTableTrailAbsorbTween()
        {
            if (_charTableTrailAbsorbTween == null)
                return;

            if (_charTableTrailAbsorbTween.IsActive())
                _charTableTrailAbsorbTween.Kill();
            _charTableTrailAbsorbTween = null;
        }

        private void ClearCharTableTrail(bool resetHeaviness)
        {
            if (resetHeaviness)
            {
                _trailSessionCells = 0f;
                _trailSegmentProgressReported = 0f;
            }

            KillCharTableTrailAbsorbTween();
            _charTableTrailHasLastPos = false;
            if (_charTableTrailParticles == null)
                return;

            _charTableTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            RefreshCharTableTrailHeaviness();
        }

        public void PrepareForFailure(Action completed)
        {
            CaptureFailureOriginCell();
            _failTriggered = true;
            _trackingSwipe = false;
            // Sync logical cell to the visual board position so later snaps don't yank
            // CharTable back to a stale start cell while the fail panel opens.
            SyncCylinderLogicalCellToVisual();
            ClearCharTableTrail(resetHeaviness: true);
            StopCharTableIdleShake(restoreRestPose: true);
            if (_charTableHintParticles != null)
                _charTableHintParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ShowHighlights(false);
            CompletePlateCollectionAnimation();

            if (_failurePreparing)
                return;

            _failurePreparing = true;
            StartCoroutine(PlayFailureSequence(completed));
        }

        private void SyncCylinderLogicalCellToVisual()
        {
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return;
            if (_failureOriginRow < 0 || _failureOriginColumn < 0)
                return;
            if (IsBoxOwnedCell(_failureOriginRow, _failureOriginColumn))
                return;
            if (_cylinder.Row == _failureOriginRow &&
                _cylinder.Column == _failureOriginColumn)
            {
                SnapCylinderToLogicalCell();
                return;
            }

            ClearStickmanOccupantFromCell(_cylinder.Row, _cylinder.Column);
            _cylinder.PlaceOnGrid(
                _grid,
                GetPiecesRoot(),
                _failureOriginRow,
                _failureOriginColumn);
            if (_grid.TryGetCell(
                    _failureOriginRow,
                    _failureOriginColumn,
                    out PuzzleCell cell) &&
                cell != null)
            {
                cell.Occupant = _cylinder.gameObject;
            }
        }

        private void CaptureFailureOriginCell()
        {
            ResolveGameplayReferences();
            if (_cylinder == null || _grid == null || !_grid.IsBuilt)
                return;

            Vector3 gridLocalPosition =
                _cylinder.transform.localPosition - _cylinder.GridOffset;
            int column = Mathf.RoundToInt(
                gridLocalPosition.x / _grid.GridSpacingX +
                (_grid.Columns - 1) * 0.5f);
            int row = Mathf.RoundToInt(
                (_grid.Rows - 1) * 0.5f -
                gridLocalPosition.z / _grid.GridSpacingZ);

            _failureOriginRow = Mathf.Clamp(row, 0, _grid.Rows - 1);
            _failureOriginColumn = Mathf.Clamp(column, 0, _grid.Columns - 1);
        }

        private void GetFailureOriginCell(out int row, out int column)
        {
            if (_failureOriginRow >= 0 && _failureOriginColumn >= 0)
            {
                row = _failureOriginRow;
                column = _failureOriginColumn;
                return;
            }

            row = _cylinder != null ? _cylinder.Row : -1;
            column = _cylinder != null ? _cylinder.Column : -1;
        }

        private IEnumerator PlayFailureSequence(Action completed)
        {
            // Let an in-flight move or delivery finish so its occupancy and plate
            // lists are not left half-updated.
            while (_isAnimating)
                yield return null;

            ResolveGameplayReferences();
            float sequenceStart = Time.realtimeSinceStartup;

            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            bool usesCharTable =
                spawner != null && spawner.UsesCharTableCylinderVisual;
            Vector2Int? excludedDropCell = null;
            if (usesCharTable &&
                TryGetCharTableFailureFaceCell(out Vector2Int faceCell))
                excludedDropCell = faceCell;
            Tween plateDrop = DropCarriedPlatesForFailure(excludedDropCell);
            RefreshStickmanAnimation(moving: false);
            _stickmanAnimator?.PlayFailure();
            PlayCharTableFailureRotation();

            if (plateDrop != null && plateDrop.IsActive())
                yield return plateDrop.WaitForCompletion();

            float remainingDelay = Mathf.Max(
                0f,
                failureUiDelay - (Time.realtimeSinceStartup - sequenceStart));
            if (remainingDelay > 0f)
                yield return new WaitForSecondsRealtime(remainingDelay);

            completed?.Invoke();
        }

        private bool TryGetCharTableFailureFaceCell(out Vector2Int cell)
        {
            cell = default;
            if (_cylinder == null || _grid == null || board == null)
                return false;

            Transform visual = GetStickmanVisual();
            if (visual == null || visual.parent == null)
                return false;

            CarryBlockJamPrefabSettings settings =
                board.PrefabSettings;
            Vector3 failureRotation = settings != null
                ? settings.charTableFailureRotation
                : new Vector3(90f, 0f, 0f);

            // Match PlayCharTableFailureRotation: apply failure euler on top of current local pose.
            Quaternion fallenLocalRotation =
                visual.localRotation * Quaternion.Euler(failureRotation);
            Vector3 faceParentDirection = fallenLocalRotation * Vector3.up;
            Vector3 faceBoardDirection = board.transform.InverseTransformDirection(
                visual.parent.TransformDirection(faceParentDirection));

            int rowOffset = 0;
            int columnOffset = 0;
            if (Mathf.Abs(faceBoardDirection.x) > Mathf.Abs(faceBoardDirection.z))
                columnOffset = faceBoardDirection.x >= 0f ? 1 : -1;
            else
                rowOffset = faceBoardDirection.z >= 0f ? -1 : 1;

            GetFailureOriginCell(out int originRow, out int originColumn);
            int row = originRow + rowOffset;
            int column = originColumn + columnOffset;
            if (!_grid.IsInside(row, column))
                return false;

            cell = new Vector2Int(row, column);
            return true;
        }

        private void PlayCharTableFailureRotation()
        {
            CarryBlockJamRuntimePieceSpawner spawner =
                GetComponent<CarryBlockJamRuntimePieceSpawner>();
            if (spawner == null || !spawner.UsesCharTableCylinderVisual)
                return;

            Transform visual = GetStickmanVisual();
            if (visual == null)
                return;

            CarryBlockJamPrefabSettings settings =
                board != null ? board.PrefabSettings : null;
            Vector3 addedRotation = settings != null
                ? settings.charTableFailureRotation
                : new Vector3(90f, 0f, 0f);
            Vector3 addedOffset = settings != null
                ? settings.charTableFailureOffset
                : new Vector3(0f, 0.25f, 0f);
            float duration = settings != null
                ? settings.charTableFailureDuration
                : 0.35f;

            visual.DOKill();
            visual.DOLocalMove(
                    visual.localPosition + addedOffset,
                    Mathf.Max(0.01f, duration))
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
            visual.DOLocalRotate(
                    visual.localEulerAngles + addedRotation,
                    Mathf.Max(0.01f, duration),
                    RotateMode.Fast)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private Tween DropCarriedPlatesForFailure(Vector2Int? excludedCell = null)
        {
            if (!HasCarriedPlates)
                return null;

            List<CarryBlockJamBoardPiece> plates = DetachCarriedPlates();
            Transform piecesRoot = GetPiecesRoot();
            List<Vector2Int> dropCells = FindFailureDropCells(
                plates.Count,
                excludedCell);
            GetFailureOriginCell(out int failureRow, out int failureColumn);
            Vector3 visualFallbackCenter =
                _grid != null && _grid.IsInside(failureRow, failureColumn)
                    ? _grid.GetWorldPosition(failureRow, failureColumn)
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

        private List<Vector2Int> FindFailureDropCells(
            int requestedCount,
            Vector2Int? excludedCell = null)
        {
            var result = new List<Vector2Int>(Mathf.Max(0, requestedCount));
            if (requestedCount <= 0)
                return result;
            if (_grid == null || _cylinder == null || !_grid.IsBuilt)
                return result;

            GetFailureOriginCell(out int startRow, out int startColumn);
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

                    // Explore through blockers so empty cells beyond tables are still reachable.
                    queue.Enqueue(new Vector2Int(nextRow, nextColumn));

                    bool isOrigin =
                        nextRow == startRow &&
                        nextColumn == startColumn;
                    bool isExcluded =
                        excludedCell.HasValue &&
                        excludedCell.Value.x == nextRow &&
                        excludedCell.Value.y == nextColumn;
                    if (isOrigin || isExcluded)
                        continue;

                    bool occupiedByOther =
                        cell.Occupant != null &&
                        cell.Occupant != _cylinder.gameObject;
                    if (occupiedByOther ||
                        IsBoxOwnedCell(nextRow, nextColumn) ||
                        IsExitCell(nextRow, nextColumn))
                        continue;

                    result.Add(new Vector2Int(nextRow, nextColumn));
                    if (result.Count >= requestedCount)
                        return result;
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
            _trackingSwipe = false;
            ClearCharTableTrail(resetHeaviness: true);
            StopCharTableIdleShake(restoreRestPose: false);
            if (_charTableHintParticles != null)
                _charTableHintParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ShowHighlights(false);
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

            return IsOrthogonallyAdjacentToAnyTable(row, column);
        }

        private bool IsOrthogonallyAdjacentToAnyTable(int row, int column)
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
                if (CanDeliverToTable(storageBox))
                    return true;
            }

            return false;
        }
    }
}
