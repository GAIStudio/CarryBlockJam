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
        [SerializeField] private float highlightHeight = 0.08f;
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.84f, 1f, 0.9f);

        private Camera _gameplayCamera;
        private PuzzleGrid _grid;
        private Vector2 _swipeStartScreen;
        private int _swipeStartRow;
        private int _swipeStartColumn;
        private bool _trackingSwipe;
        private bool _isAnimating;
        private CarryBlockJamBoardPiece _cylinder;
        private CarryBlockJamBoardPiece _carriedBox;
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

            if (!TryGetNearestGridCell(screenPosition, out int row, out int column))
                return;

            _swipeStartScreen = screenPosition;
            _swipeStartRow = row;
            _swipeStartColumn = column;
            _trackingSwipe = true;
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

            if (_carriedBox != null)
                ExecuteReleaseSwipe(rowStep, columnStep, requestedSteps);
            else
                ExecuteTravelSwipe(rowStep, columnStep, requestedSteps);
        }

        private void ExecuteTravelSwipe(int rowStep, int columnStep, int requestedSteps)
        {
            int row = _cylinder.Row;
            int column = _cylinder.Column;
            var path = new List<Vector2Int>();

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

                if (IsCarryablePiece(nextPiece))
                {
                    bool pickupFromPreviousCell = nextPiece.StackedBelow != null;
                    PrepareBoxForPickup(nextPiece, nextRow, nextColumn);

                    if (!pickupFromPreviousCell)
                        path.Add(new Vector2Int(nextRow, nextColumn));

                    MoveConnectedCylinder(path, () =>
                    {
                        nextPiece.AttachToCarrier(_cylinder.transform);
                        _carriedBox = nextPiece;
                    });
                    return;
                }

                break;
            }

            if (path.Count > 0)
                AnimateCylinderTravel(path);
        }

        private void ExecuteReleaseSwipe(int rowStep, int columnStep, int requestedSteps)
        {
            int startRow = _cylinder.Row;
            int startColumn = _cylinder.Column;
            int currentRow = startRow;
            int currentColumn = startColumn;
            var cylinderPath = new List<Vector2Int>();
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

                if (IsMatchingDropTarget(nextPiece))
                {
                    targetBox = nextPiece;
                }

                break;
            }

            if (_carriedBox == null)
                return;

            if (targetBox != null)
            {
                MoveConnectedCylinder(cylinderPath, () => AnimateCarriedBoxToTarget(targetBox));
                return;
            }

            if (TryResolveExit(rowStep, columnStep, currentRow, currentColumn, out Transform edgeExit) ||
                TryResolveExitAtCell(currentRow, currentColumn, out edgeExit))
            {
                SendCarriedBoxToExit(edgeExit, cylinderPath);
                return;
            }

            MoveConnectedCylinder(cylinderPath);
        }

        private void SendCarriedBoxToExit(Transform exitTransform, List<Vector2Int> cylinderPath)
        {
            if (_carriedBox == null || exitTransform == null)
                return;

            TweenCallback onCylinderComplete = () => AnimateBoxExit(exitTransform);
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
            if (cylinderPath == null || cylinderPath.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            AnimateCylinderTravel(cylinderPath, onComplete);
        }

        private void PrepareBoxForPickup(CarryBlockJamBoardPiece box, int row, int column)
        {
            if (box == null)
                return;

            CarryBlockJamBoardPiece stackedBelow = box.StackedBelow;
            box.ClearStackLinks();

            if (_grid.TryGetCell(row, column, out PuzzleCell cell) && cell != null)
                cell.Occupant = stackedBelow != null ? stackedBelow.gameObject : null;
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

            if (_cylinder.Row >= 0 && _cylinder.Column >= 0 && _grid.IsInside(_cylinder.Row, _cylinder.Column))
                _grid.ClearOccupant(_cylinder.Row, _cylinder.Column);

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int step = path[i];
                int targetRow = step.x;
                int targetColumn = step.y;
                Vector3 targetPosition = GetPieceLocalPosition(_cylinder, targetRow, targetColumn);
                sequence.Append(_cylinder.transform.DOLocalJump(
                    targetPosition,
                    moveJumpPower,
                    1,
                    moveDurationPerCell).SetEase(Ease.OutQuad));
                sequence.AppendCallback(() => _cylinder.PlaceOnGrid(_grid, GetPiecesRoot(), targetRow, targetColumn));
            }

            Vector2Int finalStep = path[path.Count - 1];
            sequence.OnComplete(() =>
            {
                if (_grid.TryGetCell(finalStep.x, finalStep.y, out PuzzleCell cell) && cell != null)
                    cell.Occupant = _cylinder.gameObject;

                _isAnimating = false;
                onComplete?.Invoke();
            });
        }

        private void AnimateBoxExit(Transform exitTransform)
        {
            if (_carriedBox == null || exitTransform == null)
            {
                _isAnimating = false;
                return;
            }

            _isAnimating = true;
            CarryBlockJamBoardPiece box = _carriedBox;
            _carriedBox = null;

            box.transform.SetParent(GetPiecesRoot(), true);
            box.transform.DOMove(exitTransform.position, exitTravelDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    Destroy(box.gameObject);
                    _isAnimating = false;
                });
        }

        private void AnimateCarriedBoxToTarget(CarryBlockJamBoardPiece targetBox)
        {
            if (_carriedBox == null || targetBox == null)
            {
                _isAnimating = false;
                return;
            }

            _isAnimating = true;
            CarryBlockJamBoardPiece box = _carriedBox;
            _carriedBox = null;

            box.ClearStackLinks();
            box.transform.SetParent(GetPiecesRoot(), true);
            Vector3 stackWorldTarget = targetBox.transform.TransformPoint(box.StackedOffset);
            box.transform.DOMove(stackWorldTarget, exitTravelDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    box.StackOnPiece(targetBox);
                    if (_grid.TryGetCell(targetBox.Row, targetBox.Column, out PuzzleCell cell) && cell != null)
                        cell.Occupant = box.gameObject;
                    _isAnimating = false;
                });
        }

        private Vector3 GetPieceLocalPosition(CarryBlockJamBoardPiece piece, int row, int column)
        {
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
        }

        private void UpdateSwipePreview()
        {
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

                if (_carriedBox == null)
                {
                    if (piece == null)
                    {
                        row = nextRow;
                        column = nextColumn;
                        path.Add(new Vector2Int(nextRow, nextColumn));
                        continue;
                    }

                    if (IsCarryablePiece(piece))
                    {
                        if (piece.StackedBelow == null)
                            path.Add(new Vector2Int(nextRow, nextColumn));
                        break;
                    }
                }
                else
                {
                    if (piece != null)
                    {
                        if (IsMatchingDropTarget(piece))
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

        private bool TryResolveExit(int rowStep, int columnStep, int row, int column, out Transform exitTransform)
        {
            exitTransform = null;
            if (_carriedBox == null || board == null || (columnStep == 0 && rowStep == 0))
                return false;

            BoardBorderSide side = ResolveExitSide(rowStep, columnStep);
            if (!IsOnExitBoundary(side, row, column))
                return false;

            BoardExitSettings settings = FindMatchingExit(side, row, column, _carriedBox.Color);
            if (settings == null)
                return false;

            exitTransform = FindExitTransform(settings);
            return exitTransform != null;
        }

        private bool TryResolveExitAtCell(int row, int column, out Transform exitTransform)
        {
            exitTransform = null;
            if (_carriedBox == null || board == null)
                return false;

            BoardExitSettings settings = FindMatchingExit(row, column, _carriedBox.Color);
            if (settings == null)
                return false;

            exitTransform = FindExitTransform(settings);
            return exitTransform != null;
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

        private Transform FindExitTransform(BoardExitSettings settings)
        {
            if (board == null || board.ExitsRoot == null || settings == null)
                return null;

            for (int i = 0; i < board.ExitsRoot.childCount; i++)
            {
                Transform child = board.ExitsRoot.GetChild(i);
                if (child.name == $"Exit_{settings.side}_{settings.startIndex}_{settings.color}")
                    return child;
            }

            return null;
        }

        private BoardExitSettings FindMatchingExit(
            BoardBorderSide side,
            int row,
            int column,
            PieceColorType color)
        {
            IReadOnlyList<BoardExitSettings> exits = board.Exits;
            if (exits == null)
                return null;

            for (int i = 0; i < exits.Count; i++)
            {
                BoardExitSettings exit = exits[i];
                if (exit == null || exit.side != side || exit.color != color)
                    continue;

                if (IsCellOnExit(row, column, exit))
                    return exit;
            }

            return null;
        }

        private BoardExitSettings FindMatchingExit(int row, int column, PieceColorType color)
        {
            IReadOnlyList<BoardExitSettings> exits = board.Exits;
            if (exits == null)
                return null;

            for (int i = 0; i < exits.Count; i++)
            {
                BoardExitSettings exit = exits[i];
                if (exit == null || exit.color != color)
                    continue;

                if (IsCellOnExit(row, column, exit))
                    return exit;
            }

            return null;
        }

        private bool IsCellOnExit(int row, int column, BoardExitSettings exit)
        {
            if (exit == null || _grid == null)
                return false;

            int length = Mathf.Max(1, exit.length);
            return exit.side switch
            {
                BoardBorderSide.Left => column == 0 &&
                                        row >= exit.startIndex &&
                                        row < exit.startIndex + length,
                BoardBorderSide.Right => column == _grid.Columns - 1 &&
                                         row >= exit.startIndex &&
                                         row < exit.startIndex + length,
                BoardBorderSide.Top => row == 0 &&
                                       column >= exit.startIndex &&
                                       column < exit.startIndex + length,
                BoardBorderSide.Bottom => row == _grid.Rows - 1 &&
                                          column >= exit.startIndex &&
                                          column < exit.startIndex + length,
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

            EnsureHighlightCount(path.Count);

            for (int i = 0; i < _highlightPool.Count; i++)
            {
                bool active = i < path.Count;
                Transform highlight = _highlightPool[i];
                highlight.gameObject.SetActive(active);
                if (!active)
                    continue;

                Vector2Int step = path[i];
                highlight.position = _grid.GetWorldPosition(step.x, step.y) + Vector3.up * highlightHeight;
                UpdateHighlightVisual(highlight, path, i);
            }
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
                    0.05f,
                    Mathf.Min(board.CellScaleXYZ.z, _grid.GridSpacingZ * 0.85f));

                Renderer renderer = highlight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null)
                        shader = Shader.Find("Standard");

                    Material material = new Material(shader);
                    material.color = highlightColor;
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
            highlight.localScale = new Vector3(stretchX, 0.05f, stretchZ);

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

        private static bool IsCarryablePiece(CarryBlockJamBoardPiece piece)
        {
            return piece != null &&
                   (piece.Kind == CarryBlockJamPieceKind.Box || piece.Kind == CarryBlockJamPieceKind.Plate);
        }

        private bool IsMatchingDropTarget(CarryBlockJamBoardPiece piece)
        {
            return IsCarryablePiece(piece) && _carriedBox != null && piece.Color == _carriedBox.Color;
        }
    }
}
