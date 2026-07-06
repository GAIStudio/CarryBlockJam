using GAITemplate;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    /// <summary>
    /// Spawns gameplay pieces at runtime. Keeps piece setup separate from the baked board.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CarryBlockJamRuntimePieceSpawner : MonoBehaviour
    {
        private const int DefaultPlateCountPerColor = 2;
        private const string StickmanAssetPath = "Assets/[Models]/Stickman.fbx";

        [SerializeField] private CarryBlockJamSimpleBoard board;
        [SerializeField] private BoardCylinderPlacement cylinder = BoardCylinderPlacement.CreateDefault();
        [SerializeField] private GameObject cylinderVisualPrefab;
        [SerializeField] private bool randomizeBoxes = true;
        [SerializeField] private BoardBoxPlacement[] boxes = BoardBoxPlacement.CreateDefaults();
        [SerializeField] private BoardPlatePlacement[] plates = BoardPlatePlacement.CreateDefaults();

        private Transform _piecesRoot;

        private void Start()
        {
            if (board == null)
                board = FindObjectOfType<CarryBlockJamSimpleBoard>();

            if (board == null)
            {
                Debug.LogWarning("[CarryBlockJam] Runtime piece spawner could not find a board.");
                return;
            }

            if (cylinder == null)
                cylinder = BoardCylinderPlacement.CreateDefault();

            if (boxes == null || boxes.Length == 0)
                boxes = BoardBoxPlacement.CreateDefaults();

            if (plates == null || plates.Length == 0)
                plates = BoardPlatePlacement.CreateDefaults();

            board.EnsureGridLayout();
            SpawnPieces();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cylinderVisualPrefab == null)
                cylinderVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanAssetPath);
        }
#endif

        private void SpawnPieces()
        {
            PuzzleGrid grid = board.GetComponent<PuzzleGrid>();
            if (grid == null || grid.Rows <= 0 || grid.Columns <= 0)
            {
                Debug.LogWarning("[CarryBlockJam] Runtime piece spawner could not resolve a valid grid.");
                return;
            }

            EnsurePiecesRoot();
            ClearSpawnedPieces(grid);

            int spawnedCount = 0;
            if (SpawnCylinder(grid))
                spawnedCount++;

            List<PieceColorType> activeColors = GetActiveColors();
            var occupied = new HashSet<Vector2Int>();
            if (cylinder != null)
                occupied.Add(new Vector2Int(cylinder.row, cylinder.column));

            BoardBoxPlacement[] boxPlacements = GetBoxPlacements(grid, activeColors, occupied);
            spawnedCount += SpawnBoxes(grid, boxPlacements);
            RegisterOccupiedCells(boxPlacements, occupied);

            BoardPlatePlacement[] platePlacements = GetPlatePlacements(grid, activeColors, occupied);
            spawnedCount += SpawnPlates(grid, platePlacements);

            if (spawnedCount == 0)
            {
                Debug.LogWarning("[CarryBlockJam] Runtime piece spawner did not create any pieces.");
                return;
            }

            Debug.Log($"[CarryBlockJam] Spawned {spawnedCount} runtime pieces under {board.name}/RuntimePieces.");
        }

        private void EnsurePiecesRoot()
        {
            if (_piecesRoot != null)
                return;

            Transform existing = board.transform.Find("RuntimePieces");
            if (existing != null)
            {
                _piecesRoot = existing;
                return;
            }

            var rootObject = new GameObject("RuntimePieces");
            _piecesRoot = rootObject.transform;
            _piecesRoot.SetParent(board.transform, false);
        }

        private bool SpawnCylinder(PuzzleGrid grid)
        {
            if (cylinder == null)
                cylinder = BoardCylinderPlacement.CreateDefault();

            if (!IsInsideGrid(grid, cylinder.row, cylinder.column))
            {
                cylinder.row = Mathf.Clamp((grid.Rows - 1) / 2, 0, grid.Rows - 1);
                cylinder.column = Mathf.Clamp(grid.Columns / 2, 0, grid.Columns - 1);
            }

            var cylinderObject = new GameObject($"Cylinder_{cylinder.row}_{cylinder.column}");
            cylinderObject.transform.SetParent(_piecesRoot, false);
            cylinderObject.transform.localRotation = Quaternion.Euler(cylinder.rotation);
            cylinderObject.transform.localScale = Vector3.one;

            if (!CreateCylinderVisual(cylinderObject.transform))
            {
                CreatePrimitiveVisual(
                    PrimitiveType.Cylinder,
                    "Visual",
                    cylinderObject.transform,
                    cylinder.localScale,
                    PieceColorType.White);
            }

            CarryBlockJamBoardPiece piece = cylinderObject.AddComponent<CarryBlockJamBoardPiece>();
            piece.Initialize(
                CarryBlockJamPieceKind.Cylinder,
                PieceColorType.White,
                cylinder.positionOffset,
                new Vector3(0f, 0.3f, 0f),
                cylinder.rotation);
            piece.PlaceOnGrid(grid, _piecesRoot, cylinder.row, cylinder.column);

            if (grid.TryGetCell(cylinder.row, cylinder.column, out PuzzleCell cell) && cell != null)
                cell.Occupant = cylinderObject;
            return true;
        }

        private bool CreateCylinderVisual(Transform parent)
        {
            if (parent == null || cylinderVisualPrefab == null)
                return false;

            GameObject visual = Instantiate(cylinderVisualPrefab, parent, false);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = cylinder.localScale;
            GroundVisualToParent(visual.transform);

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Destroy(colliders[i]);

            return true;
        }

        private static void GroundVisualToParent(Transform visualRoot)
        {
            if (visualRoot == null || !TryGetLocalRendererBounds(visualRoot, out Bounds localBounds))
                return;

            Vector3 localPosition = visualRoot.localPosition;
            localPosition.y -= localBounds.min.y;
            visualRoot.localPosition = localPosition;
        }

        private static bool TryGetLocalRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Bounds worldBounds = renderer.bounds;
                Vector3 localMin = root.InverseTransformPoint(worldBounds.min);
                Vector3 localMax = root.InverseTransformPoint(worldBounds.max);
                Bounds localBounds = new Bounds((localMin + localMax) * 0.5f, localMax - localMin);

                if (!hasBounds)
                {
                    bounds = localBounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(localBounds.min);
                bounds.Encapsulate(localBounds.max);
            }

            return hasBounds;
        }

        private int SpawnBoxes(PuzzleGrid grid, BoardBoxPlacement[] placements)
        {
            if (placements == null || placements.Length == 0)
                return 0;

            int spawnedCount = 0;

            for (int i = 0; i < placements.Length; i++)
            {
                BoardBoxPlacement placement = placements[i];
                if (placement == null || !IsInsideGrid(grid, placement.row, placement.column))
                    continue;

                var boxObject = new GameObject($"Box_{placement.row}_{placement.column}_{placement.color}");
                boxObject.transform.SetParent(_piecesRoot, false);
                boxObject.transform.localRotation = Quaternion.identity;
                boxObject.transform.localScale = Vector3.one;

                CreatePrimitiveVisual(
                    PrimitiveType.Cube,
                    "Visual",
                    boxObject.transform,
                    placement.localScale,
                    placement.color);
                CarryBlockJamBoardPiece piece = boxObject.AddComponent<CarryBlockJamBoardPiece>();
                piece.Initialize(
                    CarryBlockJamPieceKind.Box,
                    placement.color,
                    placement.positionOffset,
                    new Vector3(0f, 1.55f, 0f));
                piece.PlaceOnGrid(grid, _piecesRoot, placement.row, placement.column);

                if (grid.TryGetCell(placement.row, placement.column, out PuzzleCell cell) && cell != null)
                    cell.Occupant = boxObject;

                spawnedCount++;
            }

            return spawnedCount;
        }

        private int SpawnPlates(PuzzleGrid grid, BoardPlatePlacement[] placements)
        {
            if (placements == null || placements.Length == 0)
                return 0;

            int spawnedCount = 0;

            for (int i = 0; i < placements.Length; i++)
            {
                BoardPlatePlacement placement = placements[i];
                if (placement == null || !IsInsideGrid(grid, placement.row, placement.column))
                    continue;

                var plateObject = new GameObject($"Plate_{placement.row}_{placement.column}_{placement.color}");
                plateObject.transform.SetParent(_piecesRoot, false);
                plateObject.transform.localRotation = Quaternion.identity;
                plateObject.transform.localScale = Vector3.one;

                CreatePrimitiveVisual(
                    PrimitiveType.Cylinder,
                    "Visual",
                    plateObject.transform,
                    placement.localScale,
                    placement.color);
                CarryBlockJamBoardPiece piece = plateObject.AddComponent<CarryBlockJamBoardPiece>();
                piece.Initialize(
                    CarryBlockJamPieceKind.Plate,
                    placement.color,
                    placement.positionOffset,
                    new Vector3(0f, 1.15f, 0f));
                piece.PlaceOnGrid(grid, _piecesRoot, placement.row, placement.column);

                if (grid.TryGetCell(placement.row, placement.column, out PuzzleCell cell) && cell != null)
                    cell.Occupant = plateObject;

                spawnedCount++;
            }

            return spawnedCount;
        }

        private BoardBoxPlacement[] GetBoxPlacements(
            PuzzleGrid grid,
            List<PieceColorType> activeColors,
            HashSet<Vector2Int> occupied)
        {
            if (!randomizeBoxes && boxes != null && boxes.Length > 0)
                return GetManualBoxPlacements(activeColors);

            if (activeColors == null || activeColors.Count == 0)
            {
                Debug.LogWarning("[CarryBlockJam] No active colors found for runtime box generation.");
                return Array.Empty<BoardBoxPlacement>();
            }

            var placements = new List<BoardBoxPlacement>(activeColors.Count);
            HashSet<Vector2Int> localOccupied = occupied != null
                ? new HashSet<Vector2Int>(occupied)
                : new HashSet<Vector2Int>();

            for (int colorIndex = 0; colorIndex < activeColors.Count; colorIndex++)
            {
                if (!TryAddRandomBoxPlacement(grid, activeColors[colorIndex], localOccupied, placements))
                {
                    Debug.LogWarning("[CarryBlockJam] Could not place one box for each active color.");
                    break;
                }
            }

            return placements.ToArray();
        }

        private BoardPlatePlacement[] GetPlatePlacements(
            PuzzleGrid grid,
            List<PieceColorType> activeColors,
            HashSet<Vector2Int> occupied)
        {
            if (!randomizeBoxes && plates != null && plates.Length > 0)
                return plates;

            if (activeColors == null || activeColors.Count == 0)
                return Array.Empty<BoardPlatePlacement>();

            var placements = new List<BoardPlatePlacement>();
            HashSet<Vector2Int> localOccupied = occupied != null
                ? new HashSet<Vector2Int>(occupied)
                : new HashSet<Vector2Int>();

            for (int colorIndex = 0; colorIndex < activeColors.Count; colorIndex++)
            {
                PieceColorType color = activeColors[colorIndex];
                int plateCount = GetPlateCountForColor(color);
                for (int plateIndex = 0; plateIndex < plateCount; plateIndex++)
                {
                    if (!TryAddRandomPlatePlacement(grid, color, localOccupied, placements))
                    {
                        Debug.LogWarning($"[CarryBlockJam] Could not place all plates for color {color}.");
                        break;
                    }
                }
            }

            return placements.ToArray();
        }

        private List<PieceColorType> GetActiveColors()
        {
            List<PieceColorType> levelColors = GetSpawnColorsFromLevelSettings();
            return levelColors.Count > 0 ? levelColors : GetSpawnColorsFromExits();
        }

        private List<PieceColorType> GetSpawnColorsFromLevelSettings()
        {
            var colors = new List<PieceColorType>();
            LevelData levelData = ResolveLevelData();
            if (levelData?.carryBlockJam?.colorSetups == null)
                return colors;

            var seen = new HashSet<PieceColorType>();
            for (int i = 0; i < levelData.carryBlockJam.colorSetups.Count; i++)
            {
                CarryBlockJamColorSetup setup = levelData.carryBlockJam.colorSetups[i];
                if (setup == null || !PieceColorPalette.IsPaintable(setup.color) || !seen.Add(setup.color))
                    continue;

                colors.Add(setup.color);
            }

            return colors;
        }

        private List<PieceColorType> GetSpawnColorsFromExits()
        {
            var colors = new List<PieceColorType>();
            var seen = new HashSet<PieceColorType>();
            IReadOnlyList<BoardExitSettings> exits = board != null ? board.Exits : null;
            if (exits == null)
                return colors;

            for (int i = 0; i < exits.Count; i++)
            {
                BoardExitSettings exit = exits[i];
                if (exit == null || !seen.Add(exit.color))
                    continue;

                colors.Add(exit.color);
            }

            return colors;
        }

        private bool TryAddRandomBoxPlacement(
            PuzzleGrid grid,
            PieceColorType color,
            HashSet<Vector2Int> occupied,
            List<BoardBoxPlacement> placements)
        {
            var candidates = new List<Vector2Int>();
            for (int row = 0; row < grid.Rows; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (occupied.Contains(cell) || IsBlockedSpawnCellForColor(grid, row, column, color))
                        continue;

                    candidates.Add(cell);
                }
            }

            if (candidates.Count == 0)
                return false;

            Shuffle(candidates);
            Vector2Int chosen = candidates[0];
            occupied.Add(chosen);
            placements.Add(BoardBoxPlacement.Create(chosen.x, chosen.y, color));
            return true;
        }

        private bool TryAddRandomPlatePlacement(
            PuzzleGrid grid,
            PieceColorType color,
            HashSet<Vector2Int> occupied,
            List<BoardPlatePlacement> placements)
        {
            var candidates = new List<Vector2Int>();
            for (int row = 0; row < grid.Rows; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (occupied.Contains(cell) || IsBlockedSpawnCellForColor(grid, row, column, color))
                        continue;

                    candidates.Add(cell);
                }
            }

            if (candidates.Count == 0)
                return false;

            Shuffle(candidates);
            Vector2Int chosen = candidates[0];
            occupied.Add(chosen);
            placements.Add(BoardPlatePlacement.Create(chosen.x, chosen.y, color));
            return true;
        }

        private bool IsBlockedSpawnCellForColor(PuzzleGrid grid, int row, int column, PieceColorType color)
        {
            if (board == null || grid == null)
                return false;

            var exits = board.Exits;
            if (exits == null)
                return false;

            for (int i = 0; i < exits.Count; i++)
            {
                BoardExitSettings exit = exits[i];
                if (exit == null || exit.color != color)
                    continue;

                if (IsExitFrontCell(grid, row, column, exit))
                    return true;
            }

            return false;
        }

        private static bool IsExitFrontCell(PuzzleGrid grid, int row, int column, BoardExitSettings exit)
        {
            if (exit == null || grid == null)
                return false;

            return exit.side switch
            {
                BoardBorderSide.Left => column == 0 &&
                                        row >= exit.startIndex &&
                                        row < exit.startIndex + Mathf.Max(1, exit.length),
                BoardBorderSide.Right => column == grid.Columns - 1 &&
                                         row >= exit.startIndex &&
                                         row < exit.startIndex + Mathf.Max(1, exit.length),
                BoardBorderSide.Top => row == 0 &&
                                       column >= exit.startIndex &&
                                       column < exit.startIndex + Mathf.Max(1, exit.length),
                BoardBorderSide.Bottom => row == grid.Rows - 1 &&
                                          column >= exit.startIndex &&
                                          column < exit.startIndex + Mathf.Max(1, exit.length),
                _ => false,
            };
        }

        private GameObject CreatePrimitiveVisual(
            PrimitiveType primitiveType,
            string objectName,
            Transform parent,
            Vector3 localScale,
            PieceColorType color)
        {
            GameObject visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = objectName;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;

            GamePiece piece = visual.AddComponent<GamePiece>();
            piece.ApplyColor(color);

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            return visual;
        }

        private void ClearSpawnedPieces(PuzzleGrid grid)
        {
            if (grid != null)
            {
                for (int row = 0; row < grid.Rows; row++)
                {
                    for (int column = 0; column < grid.Columns; column++)
                        grid.ClearOccupant(row, column);
                }
            }

            if (_piecesRoot == null)
                return;

            for (int i = _piecesRoot.childCount - 1; i >= 0; i--)
                Destroy(_piecesRoot.GetChild(i).gameObject);
        }

        private static void Shuffle(List<Vector2Int> cells)
        {
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (cells[i], cells[swapIndex]) = (cells[swapIndex], cells[i]);
            }
        }

        private static bool IsInsideGrid(PuzzleGrid grid, int row, int column)
        {
            return row >= 0 && row < grid.Rows && column >= 0 && column < grid.Columns;
        }

        private LevelData ResolveLevelData()
        {
            if (LevelManager.instance != null && LevelManager.instance.currentLevelData != null)
                return LevelManager.instance.currentLevelData;

            if (LevelBase.Instance != null)
                return LevelBase.Instance.ActiveLevelData;

            return null;
        }

        private int GetPlateCountForColor(PieceColorType color)
        {
            LevelData levelData = ResolveLevelData();
            if (levelData?.carryBlockJam?.colorSetups == null)
                return DefaultPlateCountPerColor;

            for (int i = 0; i < levelData.carryBlockJam.colorSetups.Count; i++)
            {
                CarryBlockJamColorSetup setup = levelData.carryBlockJam.colorSetups[i];
                if (setup == null || setup.color != color)
                    continue;

                int minCount = Mathf.Max(0, setup.minPlateCount);
                int maxCount = Mathf.Max(minCount, setup.maxPlateCount);
                return UnityEngine.Random.Range(minCount, maxCount + 1);
            }

            return DefaultPlateCountPerColor;
        }

        private static void RegisterOccupiedCells(BoardBoxPlacement[] placements, HashSet<Vector2Int> occupied)
        {
            if (placements == null || occupied == null)
                return;

            for (int i = 0; i < placements.Length; i++)
            {
                BoardBoxPlacement placement = placements[i];
                if (placement == null)
                    continue;

                occupied.Add(new Vector2Int(placement.row, placement.column));
            }
        }

        private BoardBoxPlacement[] GetManualBoxPlacements(List<PieceColorType> activeColors)
        {
            if (boxes == null || boxes.Length == 0)
                return Array.Empty<BoardBoxPlacement>();

            if (activeColors == null || activeColors.Count == 0)
                return boxes;

            var placements = new List<BoardBoxPlacement>();
            var seen = new HashSet<PieceColorType>();
            for (int i = 0; i < boxes.Length; i++)
            {
                BoardBoxPlacement placement = boxes[i];
                if (placement == null || !seen.Add(placement.color) || !activeColors.Contains(placement.color))
                    continue;

                placements.Add(placement);
            }

            return placements.ToArray();
        }
    }
}
