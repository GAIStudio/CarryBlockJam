using GAITemplate;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
        private const string StickmanAssetPath = "Assets/[Models]/Stickman.fbx";
        private const string StickmanMaterialPath = "Assets/[Materials]/Mat_Stickman.mat";
        private const string StickmanControllerPath = "Assets/[Animations]/Stickman.controller";
        private const string TableModelPath = "Assets/[Models]/M_Table.fbx";
        private const string PlateModelPath = "Assets/[Models]/M_Plate.fbx";
        private const string FrozenBoxModelPath = "Assets/[Models]/M_Ice.fbx";
        private const string FrozenBoxMaterialPath = "Assets/[Materials]/Mat_Ice.mat";
        private const string CurtainBoxModelPath = "Assets/[Models]/M_Box.fbx";
        private const string CurtainBoxMaterialPath = "Assets/[Materials]/Mat_Box.mat";

        [SerializeField] private CarryBlockJamSimpleBoard board;
        [SerializeField] private BoardCylinderPlacement cylinder = BoardCylinderPlacement.CreateDefault();
        [SerializeField] private GameObject cylinderVisualPrefab;
        [SerializeField] private Material stickmanMaterial;
        [SerializeField] private RuntimeAnimatorController stickmanAnimatorController;

        public RuntimeAnimatorController StickmanAnimatorController => stickmanAnimatorController;

        [Header("Frozen Table Visual (All Levels)")]
        [FormerlySerializedAs("frozenBoxVisual")]
        [SerializeField] private BoardFrozenBoxVisualSettings frozenTableVisual = BoardFrozenBoxVisualSettings.CreateDefault();

        [Header("Curtain Table Visual (All Levels)")]
        [FormerlySerializedAs("curtainBoxVisual")]
        [SerializeField] private BoardCurtainBoxVisualSettings curtainTableVisual = BoardCurtainBoxVisualSettings.CreateDefault();

        [Header("Table Visual (All Levels)")]
        [FormerlySerializedAs("boxVisual")]
        [SerializeField] private BoardPieceVisualSettings tableVisual = BoardPieceVisualSettings.CreateTableDefault();

        [Header("Plate Visual (All Levels)")]
        [SerializeField] private BoardPieceVisualSettings plateVisual = BoardPieceVisualSettings.CreatePlateDefault();

        [Header("Manual Fallback Placements")]
        [FormerlySerializedAs("randomizeBoxes")]
        [SerializeField] private bool randomizeTables = true;
        [FormerlySerializedAs("boxes")]
        [SerializeField] private BoardBoxPlacement[] tables = BoardBoxPlacement.CreateDefaults();
        [SerializeField] private BoardPlatePlacement[] plates = BoardPlatePlacement.CreateDefaults();

        private Transform _piecesRoot;
        private LevelData _levelDataOverride;

        private void Start()
        {
            RespawnFromLevel();
        }

        public void RespawnFromLevel(LevelData levelData)
        {
            _levelDataOverride = levelData;
            try
            {
                RespawnFromLevel();
            }
            finally
            {
                _levelDataOverride = null;
            }
        }

        public void RespawnFromLevel()
        {
            if (board == null)
                board = GetComponent<CarryBlockJamSimpleBoard>() ?? FindObjectOfType<CarryBlockJamSimpleBoard>();

            if (board == null)
            {
                Debug.LogWarning("[CarryBlockJam] Runtime piece spawner could not find a board.");
                return;
            }

            if (cylinder == null)
                cylinder = BoardCylinderPlacement.CreateDefault();

            if (tables == null || tables.Length == 0)
                tables = BoardBoxPlacement.CreateDefaults();

            if (plates == null || plates.Length == 0)
                plates = BoardPlatePlacement.CreateDefaults();

            EnsureRuntimeAssets();
            board.EnsureGridLayout();
            SpawnPieces();
        }

        private void EnsureRuntimeAssets()
        {
#if UNITY_EDITOR
            if (cylinderVisualPrefab == null)
                cylinderVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanAssetPath);

            if (stickmanMaterial == null)
                stickmanMaterial = AssetDatabase.LoadAssetAtPath<Material>(StickmanMaterialPath);

            if (stickmanAnimatorController == null)
                stickmanAnimatorController =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StickmanControllerPath);

            EnsureTableVisualDefaults(tableVisual);
            EnsurePlateVisualDefaults(plateVisual);
            EnsureFrozenTableVisualDefaults(frozenTableVisual);
            EnsureCurtainTableVisualDefaults(curtainTableVisual);
#endif
        }

#if UNITY_EDITOR
        private static void EnsureTableVisualDefaults(BoardPieceVisualSettings settings)
        {
            if (settings == null)
                return;

            if (settings.model == null)
                settings.model = AssetDatabase.LoadAssetAtPath<GameObject>(TableModelPath);
        }

        private static void EnsurePlateVisualDefaults(BoardPieceVisualSettings settings)
        {
            if (settings == null)
                return;

            if (settings.model == null)
                settings.model = AssetDatabase.LoadAssetAtPath<GameObject>(PlateModelPath);
        }

        private static void EnsureFrozenTableVisualDefaults(BoardFrozenBoxVisualSettings settings)
        {
            if (settings == null)
                return;

            if (settings.model == null)
                settings.model = AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBoxModelPath);

            if (settings.material == null)
                settings.material = AssetDatabase.LoadAssetAtPath<Material>(FrozenBoxMaterialPath);
        }

        private static void EnsureCurtainTableVisualDefaults(BoardCurtainBoxVisualSettings settings)
        {
            if (settings == null)
                return;

            if (settings.model == null)
                settings.model = AssetDatabase.LoadAssetAtPath<GameObject>(CurtainBoxModelPath);

            if (settings.curtainMaterial == null)
                settings.curtainMaterial = AssetDatabase.LoadAssetAtPath<Material>(CurtainBoxMaterialPath);
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cylinderVisualPrefab == null)
                cylinderVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanAssetPath);

            if (tableVisual == null)
                tableVisual = BoardPieceVisualSettings.CreateTableDefault();

            EnsureTableVisualDefaults(tableVisual);
            EnsureFrozenTableVisualDefaults(frozenTableVisual);
            EnsureCurtainTableVisualDefaults(curtainTableVisual);

            if (plateVisual == null)
                plateVisual = BoardPieceVisualSettings.CreatePlateDefault();

            EnsurePlateVisualDefaults(plateVisual);
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
            var occupied = new HashSet<Vector2Int>();
            if (SpawnCylinder(grid))
                spawnedCount++;

            if (cylinder != null)
                occupied.Add(new Vector2Int(cylinder.row, cylinder.column));

            BoardBoxPlacement[] boxPlacements = GetBoxPlacements(grid, occupied);
            spawnedCount += SpawnBoxes(grid, boxPlacements);
            RegisterOccupiedCells(boxPlacements, occupied);

            BoardPlatePlacement[] platePlacements = FilterInvalidPlatePlacements(
                GetPlatePlacements(grid, occupied, boxPlacements),
                boxPlacements);
            spawnedCount += SpawnPlates(grid, platePlacements);
            InitializeHiddenBoxes(grid);

            if (spawnedCount == 0)
            {
                Debug.LogWarning("[CarryBlockJam] Runtime piece spawner did not create any pieces.");
                return;
            }

            Debug.Log($"[CarryBlockJam] Spawned {spawnedCount} runtime pieces under {board.name}/RuntimePieces.");
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(board);
#endif
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

            ResolveStickmanSpawn(grid);
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
                GameObject fallback = CreatePrimitiveVisual(
                    PrimitiveType.Cylinder,
                    "Visual",
                    cylinderObject.transform,
                    cylinder.localScale,
                    PieceColorType.White);
                ApplyStickmanMaterial(fallback);
            }

            CarryBlockJamBoardPiece piece = cylinderObject.AddComponent<CarryBlockJamBoardPiece>();
            Vector3 spawnOffset = ResolveStickmanOffset();
            cylinder.positionOffset = spawnOffset;
            piece.Initialize(
                CarryBlockJamPieceKind.Cylinder,
                PieceColorType.White,
                spawnOffset,
                new Vector3(0f, 0.3f, 0f),
                cylinder.rotation);
            piece.PlaceOnGrid(grid, _piecesRoot, cylinder.row, cylinder.column);

            if (grid.TryGetCell(cylinder.row, cylinder.column, out PuzzleCell cell) && cell != null)
                cell.Occupant = cylinderObject;
            return true;
        }

        private Vector3 ResolveStickmanOffset()
        {
            CarryBlockJamPrefabSettings settings = board != null ? board.PrefabSettings : null;
            if (settings != null)
                return settings.stickmanOffset;

            if (cylinder != null)
                return cylinder.positionOffset;

            return BoardCylinderPlacement.CreateDefault().positionOffset;
        }

        private Vector3 ResolveStickmanRotation()
        {
            CarryBlockJamPrefabSettings settings = board != null ? board.PrefabSettings : null;
            if (settings != null)
                return settings.stickmanRotation;

            return new Vector3(0f, 180f, 0f);
        }

        private bool CreateCylinderVisual(Transform parent)
        {
            if (parent == null || cylinderVisualPrefab == null)
                return false;

            GameObject visual = Instantiate(cylinderVisualPrefab, parent, false);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(ResolveStickmanRotation());
            visual.transform.localScale = cylinder.localScale == Vector3.zero
                ? Vector3.one
                : cylinder.localScale;
            // Do not GroundVisualToParent here: SkinnedMeshRenderer.bounds are unreliable at
            // instantiate. Height comes from Prefab Settings → Stickman Offset.
            ApplyStickmanMaterial(visual);
            SetupStickmanAnimator(visual);

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                DestroyObject(colliders[i]);

            return true;
        }

        private void SetupStickmanAnimator(GameObject visualRoot)
        {
            if (visualRoot == null)
                return;

#if UNITY_EDITOR
            if (stickmanAnimatorController == null)
                stickmanAnimatorController =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StickmanControllerPath);
#endif
            CarryBlockJamStickmanAnimator.EnsureOnCylinder(visualRoot.transform.parent, stickmanAnimatorController);
        }

        private void ApplyStickmanMaterial(GameObject visualRoot)
        {
            if (visualRoot == null)
                return;

#if UNITY_EDITOR
            if (stickmanMaterial == null)
                stickmanMaterial = AssetDatabase.LoadAssetAtPath<Material>(StickmanMaterialPath);
#endif
            if (stickmanMaterial == null)
                return;

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = stickmanMaterial;
                    continue;
                }

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    materials[materialIndex] = stickmanMaterial;
                renderer.sharedMaterials = materials;
            }
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

                var boxObject = new GameObject($"Table_{placement.row}_{placement.column}_{placement.color}");
                boxObject.transform.SetParent(_piecesRoot, false);
                boxObject.transform.localRotation = Quaternion.identity;
                boxObject.transform.localScale = Vector3.one;

                PieceColorType visualColor = placement.isHidden ? PieceColorType.Grey : placement.color;
                GameObject visualObject = CreateBoxVisual(
                    tableVisual,
                    "Visual",
                    boxObject.transform,
                    visualColor);
                CarryBlockJamBoardPiece piece = boxObject.AddComponent<CarryBlockJamBoardPiece>();
                piece.Initialize(
                    CarryBlockJamPieceKind.Box,
                    placement.color,
                    tableVisual != null ? tableVisual.offset : new Vector3(0f, 0.399f, 0f),
                    new Vector3(0f, 1.55f, 0f));
                piece.PlaceOnGrid(grid, _piecesRoot, placement.row, placement.column);

                if (placement.isHidden)
                {
                    piece.SetColorHidden(true);
                    if (visualObject != null)
                        CarryBlockJamArtTableUtility.ApplyHiddenTableMaterial(visualObject.transform);

                    GamePiece visualPiece = visualObject != null ? visualObject.GetComponent<GamePiece>() : null;
                    if (visualPiece != null)
                        visualPiece.ApplyHidden(true);

                    CarryBlockJamHiddenBox hiddenBox = boxObject.AddComponent<CarryBlockJamHiddenBox>();
                    hiddenBox.Bind(piece, visualObject != null ? visualObject.transform : null, visualPiece);
                }
                else if (placement.isCurtain)
                {
                    PieceColorType curtainColor = PieceColorPalette.IsPaintable(placement.curtainColor)
                        ? placement.curtainColor
                        : placement.color;
                    piece.SetCurtained(true);
                    CarryBlockJamCurtainBox curtainBox = boxObject.AddComponent<CarryBlockJamCurtainBox>();
                    curtainBox.Bind(
                        piece,
                        visualObject != null ? visualObject.transform : null,
                        curtainColor,
                        CountRequiredExitPlatesForColor(curtainColor),
                        ResolveCurtainBoxVisualSettings());
                }
                else if (placement.isFrozen)
                {
                    piece.SetFrozen(true);
                    CarryBlockJamFrozenBox frozenBox = boxObject.AddComponent<CarryBlockJamFrozenBox>();
                    frozenBox.Bind(
                        piece,
                        visualObject != null ? visualObject.transform : null,
                        placement.unlockMoves,
                        ResolveFrozenBoxVisualSettings());
                }

                if (grid.TryGetCell(placement.row, placement.column, out PuzzleCell cell) && cell != null)
                    cell.Occupant = boxObject;

                spawnedCount++;
            }

            return spawnedCount;
        }

        private BoardFrozenBoxVisualSettings ResolveFrozenBoxVisualSettings() =>
            frozenTableVisual ?? BoardFrozenBoxVisualSettings.CreateDefault();

        private BoardCurtainBoxVisualSettings ResolveCurtainBoxVisualSettings() =>
            curtainTableVisual ?? BoardCurtainBoxVisualSettings.CreateDefault();

        private int CountRequiredExitPlatesForColor(PieceColorType color)
        {
            if (!PieceColorPalette.IsPaintable(color))
                return 0;

            LevelData levelData = ResolveLevelData();
            List<CarryBlockJamExitDefinition> exits = levelData?.carryBlockJam?.exits;
            if (exits == null || exits.Count == 0)
                return 0;

            int total = 0;
            for (int i = 0; i < exits.Count; i++)
            {
                CarryBlockJamExitDefinition exit = exits[i];
                if (exit?.goals == null)
                    continue;

                for (int goalIndex = 0; goalIndex < exit.goals.Count; goalIndex++)
                {
                    CarryBlockJamExitGoal goal = exit.goals[goalIndex];
                    if (goal == null || goal.color != color)
                        continue;

                    total += Mathf.Max(0, goal.requiredPlateCount);
                }
            }

            return total;
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

                if (IsBlockedPlateCell(placement.row, placement.column))
                    continue;

                grid.TryGetCell(placement.row, placement.column, out PuzzleCell cell);
                if (cell != null && cell.Occupant != null)
                    continue;

                var plateObject = new GameObject($"Plate_{placement.row}_{placement.column}_{placement.color}");
                plateObject.transform.SetParent(_piecesRoot, false);
                plateObject.transform.localRotation = Quaternion.identity;
                plateObject.transform.localScale = Vector3.one;

                CreatePlateVisual(plateVisual, "Visual", plateObject.transform, placement.color);
                CarryBlockJamBoardPiece piece = plateObject.AddComponent<CarryBlockJamBoardPiece>();
                piece.Initialize(
                    CarryBlockJamPieceKind.Plate,
                    placement.color,
                    plateVisual != null ? plateVisual.offset : new Vector3(0f, 0.75f, 0f),
                    new Vector3(0f, 1.15f, 0f));

                piece.PlaceOnGrid(grid, _piecesRoot, placement.row, placement.column);
                if (cell != null)
                    cell.Occupant = plateObject;

                spawnedCount++;
            }

            return spawnedCount;
        }

        private BoardPlatePlacement[] FilterInvalidPlatePlacements(
            BoardPlatePlacement[] placements,
            BoardBoxPlacement[] boxPlacements)
        {
            if (placements == null || placements.Length == 0)
                return Array.Empty<BoardPlatePlacement>();

            var filtered = new List<BoardPlatePlacement>(placements.Length);
            for (int i = 0; i < placements.Length; i++)
            {
                BoardPlatePlacement placement = placements[i];
                if (placement == null || IsBlockedPlateCell(placement.row, placement.column, boxPlacements))
                    continue;

                filtered.Add(placement);
            }

            return filtered.ToArray();
        }

        private bool IsBlockedPlateCell(int row, int column, BoardBoxPlacement[] boxPlacements = null)
        {
            if (cylinder != null && cylinder.row == row && cylinder.column == column)
                return true;

            if (boxPlacements == null)
                return false;

            for (int i = 0; i < boxPlacements.Length; i++)
            {
                BoardBoxPlacement placement = boxPlacements[i];
                if (placement == null)
                    continue;

                if (placement.row == row && placement.column == column)
                    return true;
            }

            return false;
        }

        private BoardBoxPlacement[] GetBoxPlacements(
            PuzzleGrid grid,
            HashSet<Vector2Int> occupied)
        {
            BoardBoxPlacement[] levelPlacements = GetLevelBoxPlacements();
            if (IsAutoTableGenerationDisabled())
                return levelPlacements;

            ExitDrivenSpawnPlan plan = BuildExitDrivenSpawnPlan();

            if (plan.BoxColors.Count > 0 && levelPlacements.Length > 0 && levelPlacements.Length < plan.BoxColors.Count)
                return MergeLevelAndGeneratedBoxPlacements(grid, occupied, levelPlacements, plan);

            if (levelPlacements.Length > 0)
                return levelPlacements;

            if (!randomizeTables && tables != null && tables.Length > 0)
                return GetManualBoxPlacements(GetActiveColors());

            return GenerateExitDrivenBoxPlacements(grid, occupied);
        }

        private bool IsAutoTableGenerationDisabled()
        {
            LevelData levelData = ResolveLevelData();
            return levelData?.carryBlockJam != null && levelData.carryBlockJam.disableAutoTables;
        }

        private BoardBoxPlacement[] MergeLevelAndGeneratedBoxPlacements(
            PuzzleGrid grid,
            HashSet<Vector2Int> occupied,
            BoardBoxPlacement[] levelPlacements,
            ExitDrivenSpawnPlan plan)
        {
            var placements = new List<BoardBoxPlacement>(levelPlacements);
            var boxCells = new HashSet<Vector2Int>();
            HashSet<Vector2Int> localOccupied = occupied != null
                ? new HashSet<Vector2Int>(occupied)
                : new HashSet<Vector2Int>();

            for (int i = 0; i < levelPlacements.Length; i++)
            {
                BoardBoxPlacement placement = levelPlacements[i];
                if (placement == null)
                    continue;

                var cell = new Vector2Int(placement.row, placement.column);
                boxCells.Add(cell);
                localOccupied.Add(cell);
            }

            Vector2Int stickmanCell = GetStickmanCell();

            for (int boxIndex = levelPlacements.Length; boxIndex < plan.BoxColors.Count; boxIndex++)
            {
                PieceColorType color = plan.BoxColors[boxIndex];
                List<Vector2Int> candidates = CollectBoxCandidates(grid, localOccupied);
                Shuffle(candidates);

                bool placed = false;
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    Vector2Int candidate = candidates[candidateIndex];
                    var trialBoxCells = new HashSet<Vector2Int>(boxCells) { candidate };
                    if (!IsStickmanAreaConnected(stickmanCell, trialBoxCells, grid, localOccupied))
                        continue;

                    placements.Add(BoardBoxPlacement.Create(candidate.x, candidate.y, color));
                    boxCells.Add(candidate);
                    localOccupied.Add(candidate);
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    Debug.LogWarning(
                        $"[CarryBlockJam] Could not place remaining auto box for exit color {color}.");
                }
            }

            return placements.ToArray();
        }

        private void InitializeHiddenBoxes(PuzzleGrid grid)
        {
            if (_piecesRoot == null || grid == null)
                return;

            CarryBlockJamHiddenBox[] hiddenBoxes = _piecesRoot.GetComponentsInChildren<CarryBlockJamHiddenBox>(true);
            for (int i = 0; i < hiddenBoxes.Length; i++)
            {
                if (hiddenBoxes[i] == null)
                    continue;

                hiddenBoxes[i].RegisterSurroundingPlates(grid);
            }
        }

        private BoardPlatePlacement[] GetPlatePlacements(
            PuzzleGrid grid,
            HashSet<Vector2Int> occupied,
            BoardBoxPlacement[] boxPlacements)
        {
            BoardPlatePlacement[] levelPlacements = GetLevelPlatePlacements();
            if (levelPlacements.Length > 0)
                return levelPlacements;

            if (!randomizeTables && plates != null && plates.Length > 0)
                return plates;

            return GenerateExitDrivenPlatePlacements(grid, occupied, boxPlacements);
        }

        private readonly struct ExitDrivenSpawnPlan
        {
            public ExitDrivenSpawnPlan(List<PieceColorType> boxColors, Dictionary<PieceColorType, int> plateCountsByColor)
            {
                BoxColors = boxColors ?? new List<PieceColorType>();
                PlateCountsByColor = plateCountsByColor ?? new Dictionary<PieceColorType, int>();
            }

            public List<PieceColorType> BoxColors { get; }
            public Dictionary<PieceColorType, int> PlateCountsByColor { get; }
        }

        private ExitDrivenSpawnPlan BuildExitDrivenSpawnPlan()
        {
            var boxColors = new List<PieceColorType>();
            var plateCountsByColor = new Dictionary<PieceColorType, int>();

            LevelData levelData = ResolveLevelData();
            List<CarryBlockJamExitDefinition> exits = levelData?.carryBlockJam?.exits;
            if (exits == null)
                return new ExitDrivenSpawnPlan(boxColors, plateCountsByColor);

            for (int exitIndex = 0; exitIndex < exits.Count; exitIndex++)
            {
                CarryBlockJamExitDefinition exit = exits[exitIndex];
                if (exit?.goals == null || exit.goals.Count == 0)
                    continue;

                bool boxColorAssigned = false;
                for (int goalIndex = 0; goalIndex < exit.goals.Count; goalIndex++)
                {
                    CarryBlockJamExitGoal goal = exit.goals[goalIndex];
                    if (goal == null || !PieceColorPalette.IsPaintable(goal.color))
                        continue;

                    if (!boxColorAssigned)
                    {
                        boxColors.Add(goal.color);
                        boxColorAssigned = true;
                    }

                    plateCountsByColor.TryGetValue(goal.color, out int currentCount);
                    plateCountsByColor[goal.color] = currentCount + Mathf.Max(0, goal.requiredPlateCount);
                }
            }

            return new ExitDrivenSpawnPlan(boxColors, plateCountsByColor);
        }

        private BoardBoxPlacement[] GenerateExitDrivenBoxPlacements(PuzzleGrid grid, HashSet<Vector2Int> occupied)
        {
            ExitDrivenSpawnPlan plan = BuildExitDrivenSpawnPlan();
            if (plan.BoxColors.Count == 0)
            {
                Debug.LogWarning("[CarryBlockJam] No exits found for auto box generation.");
                return Array.Empty<BoardBoxPlacement>();
            }

            var placements = new List<BoardBoxPlacement>(plan.BoxColors.Count);
            var boxCells = new HashSet<Vector2Int>();
            HashSet<Vector2Int> localOccupied = occupied != null
                ? new HashSet<Vector2Int>(occupied)
                : new HashSet<Vector2Int>();
            Vector2Int stickmanCell = GetStickmanCell();

            for (int i = 0; i < plan.BoxColors.Count; i++)
            {
                PieceColorType color = plan.BoxColors[i];
                List<Vector2Int> candidates = CollectBoxCandidates(grid, localOccupied);
                Shuffle(candidates);

                bool placed = false;
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    Vector2Int candidate = candidates[candidateIndex];
                    var trialBoxCells = new HashSet<Vector2Int>(boxCells) { candidate };
                    if (!IsStickmanAreaConnected(stickmanCell, trialBoxCells, grid, localOccupied))
                        continue;

                    placements.Add(BoardBoxPlacement.Create(candidate.x, candidate.y, color));
                    boxCells.Add(candidate);
                    localOccupied.Add(candidate);
                    placed = true;
                    break;
                }

                if (!placed)
                    Debug.LogWarning($"[CarryBlockJam] Could not place box for exit color {color}.");
            }

            return placements.ToArray();
        }

        private BoardPlatePlacement[] GenerateExitDrivenPlatePlacements(
            PuzzleGrid grid,
            HashSet<Vector2Int> occupied,
            BoardBoxPlacement[] boxPlacements)
        {
            ExitDrivenSpawnPlan plan = BuildExitDrivenSpawnPlan();
            if (plan.PlateCountsByColor.Count == 0)
                return Array.Empty<BoardPlatePlacement>();

            var placements = new List<BoardPlatePlacement>();
            var boxCells = new HashSet<Vector2Int>();
            if (boxPlacements != null)
            {
                for (int i = 0; i < boxPlacements.Length; i++)
                {
                    BoardBoxPlacement placement = boxPlacements[i];
                    if (placement == null)
                        continue;

                    boxCells.Add(new Vector2Int(placement.row, placement.column));
                }
            }

            HashSet<Vector2Int> localOccupied = occupied != null
                ? new HashSet<Vector2Int>(occupied)
                : new HashSet<Vector2Int>();
            Vector2Int stickmanCell = GetStickmanCell();
            var blockedPlateCells = new HashSet<Vector2Int>(boxCells) { stickmanCell };

            foreach (KeyValuePair<PieceColorType, int> entry in plan.PlateCountsByColor)
            {
                PieceColorType color = entry.Key;
                int requiredCount = entry.Value;
                for (int plateIndex = 0; plateIndex < requiredCount; plateIndex++)
                {
                    List<PlateSpawnCandidate> candidates = CollectPlateCandidates(
                        grid,
                        color,
                        localOccupied,
                        blockedPlateCells);
                    if (HasHiddenBoxes(boxPlacements))
                        PrioritizeCandidatesNearHiddenBoxes(candidates, boxPlacements);
                    else
                        Shuffle(candidates);

                    bool placed = false;
                    for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                    {
                        PlateSpawnCandidate candidate = candidates[candidateIndex];
                        if (!CanPlacePlateForStickmanPath(stickmanCell, candidate, boxCells, grid))
                            continue;

                        placements.Add(BoardPlatePlacement.Create(candidate.Row, candidate.Column, color));
                        localOccupied.Add(candidate.Cell);
                        placed = true;
                        break;
                    }

                    if (!placed && candidates.Count > 0)
                    {
                        PlateSpawnCandidate fallback = candidates[0];
                        placements.Add(BoardPlatePlacement.Create(fallback.Row, fallback.Column, color));
                        localOccupied.Add(fallback.Cell);
                        placed = true;
                        Debug.LogWarning(
                            $"[CarryBlockJam] Placed plate for {color} without path validation because no reachable cell was found.");
                    }

                    if (!placed)
                    {
                        Debug.LogWarning(
                            $"[CarryBlockJam] Could not place plate {plateIndex + 1}/{requiredCount} for color {color} while keeping stickman path.");
                        break;
                    }
                }
            }

            return placements.ToArray();
        }

        private readonly struct PlateSpawnCandidate
        {
            public PlateSpawnCandidate(int row, int column)
            {
                Row = row;
                Column = column;
            }

            public int Row { get; }
            public int Column { get; }
            public Vector2Int Cell => new Vector2Int(Row, Column);
        }

        private Vector2Int GetStickmanCell() => new Vector2Int(cylinder.row, cylinder.column);

        private List<Vector2Int> CollectBoxCandidates(PuzzleGrid grid, HashSet<Vector2Int> occupied)
        {
            var candidates = new List<Vector2Int>();
            for (int row = 0; row < grid.Rows; row++)
            {
                if (IsAutoSpawnBorderRow(grid, row))
                    continue;

                for (int column = 0; column < grid.Columns; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (occupied.Contains(cell) || IsBlockedSpawnCellForBox(grid, row, column))
                        continue;

                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        private List<PlateSpawnCandidate> CollectPlateCandidates(
            PuzzleGrid grid,
            PieceColorType color,
            HashSet<Vector2Int> occupied,
            HashSet<Vector2Int> blockedCells)
        {
            var candidates = new List<PlateSpawnCandidate>();

            for (int row = 0; row < grid.Rows; row++)
            {
                if (IsAutoSpawnBorderRow(grid, row))
                    continue;

                for (int column = 0; column < grid.Columns; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (blockedCells.Contains(cell))
                        continue;

                    if (occupied.Contains(cell) || IsBlockedSpawnCellForColor(grid, row, column, color))
                        continue;

                    candidates.Add(new PlateSpawnCandidate(row, column));
                }
            }

            return candidates;
        }

        private static bool IsAutoSpawnBorderRow(PuzzleGrid grid, int row)
        {
            if (grid == null || grid.Rows <= 0)
                return true;

            return row <= 0 || row >= grid.Rows - 1;
        }

        private static bool HasHiddenBoxes(BoardBoxPlacement[] boxPlacements)
        {
            if (boxPlacements == null)
                return false;

            for (int i = 0; i < boxPlacements.Length; i++)
            {
                if (boxPlacements[i] != null && boxPlacements[i].isHidden)
                    return true;
            }

            return false;
        }

        private static void PrioritizeCandidatesNearHiddenBoxes(
            List<PlateSpawnCandidate> candidates,
            BoardBoxPlacement[] boxPlacements)
        {
            if (candidates == null || candidates.Count <= 1 || boxPlacements == null || boxPlacements.Length == 0)
                return;

            HashSet<Vector2Int> preferredCells = BuildHiddenBoxSurroundingCells(boxPlacements);
            if (preferredCells.Count == 0)
                return;

            candidates.Sort((left, right) =>
            {
                bool leftPreferred = preferredCells.Contains(left.Cell);
                bool rightPreferred = preferredCells.Contains(right.Cell);
                if (leftPreferred == rightPreferred)
                    return 0;

                return leftPreferred ? -1 : 1;
            });
        }

        private static HashSet<Vector2Int> BuildHiddenBoxSurroundingCells(BoardBoxPlacement[] boxPlacements)
        {
            var preferredCells = new HashSet<Vector2Int>();
            for (int i = 0; i < boxPlacements.Length; i++)
            {
                BoardBoxPlacement placement = boxPlacements[i];
                if (placement == null || !placement.isHidden)
                    continue;

                var boxCell = new Vector2Int(placement.row, placement.column);

                for (int directionIndex = 0; directionIndex < HiddenBoxSurroundingOffsets.Length; directionIndex++)
                    preferredCells.Add(boxCell + HiddenBoxSurroundingOffsets[directionIndex]);
            }

            return preferredCells;
        }

        private bool CanPlacePlateForStickmanPath(
            Vector2Int stickmanCell,
            PlateSpawnCandidate candidate,
            HashSet<Vector2Int> boxCells,
            PuzzleGrid grid)
        {
            return CanStickmanReachCell(stickmanCell, candidate.Cell, boxCells, grid);
        }

        private bool IsStickmanAreaConnected(
            Vector2Int stickmanCell,
            HashSet<Vector2Int> boxCells,
            PuzzleGrid grid,
            HashSet<Vector2Int> occupied)
        {
            if (!IsInsideGrid(grid, stickmanCell.x, stickmanCell.y))
                return false;

            int reachableWalkableCells = 0;
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(stickmanCell);
            visited.Add(stickmanCell);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (!current.Equals(stickmanCell))
                    reachableWalkableCells++;

                for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
                {
                    Vector2Int next = current + CardinalDirections[directionIndex];
                    if (!IsInsideGrid(grid, next.x, next.y) || visited.Contains(next))
                        continue;

                    if (boxCells.Contains(next))
                        continue;

                    if (IsBlockedSpawnCellForBox(grid, next.x, next.y))
                        continue;

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            int totalWalkableCells = 0;
            for (int row = 0; row < grid.Rows; row++)
            {
                for (int column = 0; column < grid.Columns; column++)
                {
                    var cell = new Vector2Int(row, column);
                    if (boxCells.Contains(cell) || IsBlockedSpawnCellForBox(grid, row, column))
                        continue;

                    totalWalkableCells++;
                }
            }

            return reachableWalkableCells > 0 || totalWalkableCells <= 1;
        }

        private bool CanStickmanReachCell(
            Vector2Int stickmanCell,
            Vector2Int targetCell,
            HashSet<Vector2Int> boxCells,
            PuzzleGrid grid)
        {
            if (!IsInsideGrid(grid, targetCell.x, targetCell.y))
                return false;

            if (stickmanCell == targetCell)
                return true;

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(stickmanCell);
            visited.Add(stickmanCell);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == targetCell)
                    return true;

                for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
                {
                    Vector2Int next = current + CardinalDirections[directionIndex];
                    if (!IsInsideGrid(grid, next.x, next.y) || visited.Contains(next))
                        continue;

                    if (boxCells.Contains(next))
                        continue;

                    if (IsBlockedSpawnCellForBox(grid, next.x, next.y))
                        continue;

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        private static readonly Vector2Int[] HiddenBoxSurroundingOffsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1),
        };
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

        private bool IsBlockedSpawnCellForBox(PuzzleGrid grid, int row, int column)
        {
            if (board == null || grid == null)
                return false;

            var exits = board.Exits;
            if (exits == null)
                return false;

            for (int i = 0; i < exits.Count; i++)
            {
                BoardExitSettings exit = exits[i];
                if (exit == null)
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

        private GameObject CreateBoxVisual(
            BoardPieceVisualSettings visual,
            string objectName,
            Transform parent,
            PieceColorType color)
        {
            BoardPieceVisualSettings resolvedVisual = visual ?? BoardPieceVisualSettings.CreateTableDefault();
            GameObject visualObject;

            if (resolvedVisual.model != null)
            {
                visualObject = Instantiate(resolvedVisual.model, parent, false);
                visualObject.name = objectName;
            }
            else
            {
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.name = objectName;
                visualObject.transform.SetParent(parent, false);
                DestroyObject(visualObject.GetComponent<Collider>());
            }

            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = resolvedVisual.scale == Vector3.zero
                ? Vector3.one
                : resolvedVisual.scale;

            if (visualObject.GetComponent<GamePiece>() == null)
                visualObject.AddComponent<GamePiece>();

            if (resolvedVisual.model != null)
                CarryBlockJamArtTableUtility.ApplyTableColor(visualObject.transform, color);
            else if (resolvedVisual.material != null)
            {
                Renderer renderer = visualObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = resolvedVisual.material;
            }
            else
            {
                GamePiece piece = visualObject.GetComponent<GamePiece>();
                if (piece != null)
                    piece.ApplyColor(color);
            }

            Collider[] colliders = visualObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                DestroyObject(colliders[i]);

            return visualObject;
        }

        private GameObject CreatePlateVisual(
            BoardPieceVisualSettings visual,
            string objectName,
            Transform parent,
            PieceColorType color)
        {
            BoardPieceVisualSettings resolvedVisual = visual ?? BoardPieceVisualSettings.CreatePlateDefault();
            GameObject visualObject;

            if (resolvedVisual.model != null)
            {
                visualObject = Instantiate(resolvedVisual.model, parent, false);
                visualObject.name = objectName;
            }
            else
            {
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visualObject.name = objectName;
                visualObject.transform.SetParent(parent, false);
                DestroyObject(visualObject.GetComponent<Collider>());
            }

            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = resolvedVisual.scale == Vector3.zero
                ? Vector3.one
                : resolvedVisual.scale;

            if (visualObject.GetComponent<GamePiece>() == null)
                visualObject.AddComponent<GamePiece>();

            if (resolvedVisual.model != null)
                CarryBlockJamArtPlateUtility.ApplyPlateColor(visualObject.transform, color);
            else if (resolvedVisual.material != null)
            {
                Renderer renderer = visualObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = resolvedVisual.material;
            }
            else
            {
                GamePiece piece = visualObject.GetComponent<GamePiece>();
                if (piece != null)
                    piece.ApplyColor(color);
            }

            Collider[] colliders = visualObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                DestroyObject(colliders[i]);

            return visualObject;
        }

        private GameObject CreatePieceVisual(
            BoardPieceVisualSettings visual,
            string objectName,
            Transform parent,
            PieceColorType color,
            PrimitiveType fallbackPrimitive)
        {
            BoardPieceVisualSettings resolvedVisual = visual ?? new BoardPieceVisualSettings();
            GameObject visualObject;

            if (resolvedVisual.model != null)
            {
                visualObject = Instantiate(resolvedVisual.model, parent, false);
                visualObject.name = objectName;
            }
            else
            {
                visualObject = GameObject.CreatePrimitive(fallbackPrimitive);
                visualObject.name = objectName;
                visualObject.transform.SetParent(parent, false);
                DestroyObject(visualObject.GetComponent<Collider>());
            }

            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = resolvedVisual.scale;

            GamePiece piece = visualObject.GetComponent<GamePiece>();
            if (piece == null)
                piece = visualObject.AddComponent<GamePiece>();

            if (resolvedVisual.material != null)
            {
                Renderer renderer = visualObject.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = resolvedVisual.material;
            }
            else
            {
                piece.ApplyColor(color);
            }

            Collider collider = visualObject.GetComponent<Collider>();
            if (collider != null)
                DestroyObject(collider);

            return visualObject;
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
                DestroyObject(collider);

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
                DestroyObject(_piecesRoot.GetChild(i).gameObject);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(target);
                return;
            }
#endif
            Destroy(target);
        }

        private static void Shuffle<T>(List<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
            }
        }

        private static bool IsInsideGrid(PuzzleGrid grid, int row, int column)
        {
            return row >= 0 && row < grid.Rows && column >= 0 && column < grid.Columns;
        }

        private void ResolveStickmanSpawn(PuzzleGrid grid)
        {
            LevelData levelData = ResolveLevelData();
            if (grid == null)
                return;

            if (levelData?.carryBlockJam == null)
            {
                cylinder.row = Mathf.Clamp((grid.Rows - 1) / 2, 0, grid.Rows - 1);
                cylinder.column = Mathf.Clamp(grid.Columns / 2, 0, grid.Columns - 1);
                return;
            }

            CarryBlockJamLevelSettings settings = levelData.carryBlockJam;
            switch (settings.stickmanSpawnMode)
            {
                case CarryBlockJamStickmanSpawnMode.FixedCell:
                    ApplyStickmanCell(grid, settings.fixedStickmanCell.row, settings.fixedStickmanCell.column);
                    break;
                case CarryBlockJamStickmanSpawnMode.Center:
                    ApplyStickmanCell(grid, (grid.Rows - 1) / 2, grid.Columns / 2);
                    break;
                default:
                    ApplyStickmanCell(grid, (grid.Rows - 1) / 2, grid.Columns / 2);
                    break;
            }

            if (OverlapsManualBoxPlacement(levelData))
                ApplyStickmanCell(grid, settings.fixedStickmanCell.row, settings.fixedStickmanCell.column);
        }

        private void ApplyStickmanCell(PuzzleGrid grid, int row, int column)
        {
            cylinder.row = Mathf.Clamp(row, 0, grid.Rows - 1);
            cylinder.column = Mathf.Clamp(column, 0, grid.Columns - 1);
        }

        private bool OverlapsManualBoxPlacement(LevelData levelData)
        {
            List<CarryBlockJamBoxPlacement> placements = levelData?.carryBlockJam?.tablePlacements;
            if (placements == null || placements.Count == 0)
                return false;

            for (int i = 0; i < placements.Count; i++)
            {
                CarryBlockJamBoxPlacement placement = placements[i];
                if (placement == null)
                    continue;

                if (placement.row == cylinder.row && placement.column == cylinder.column)
                    return true;
            }

            return false;
        }

        private LevelData ResolveLevelData()
        {
            if (_levelDataOverride != null)
                return _levelDataOverride;

            if (LevelManager.instance != null && LevelManager.instance.currentLevelData != null)
                return LevelManager.instance.currentLevelData;

            if (LevelBase.Instance != null)
                return LevelBase.Instance.ActiveLevelData;

            return null;
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
            if (tables == null || tables.Length == 0)
                return Array.Empty<BoardBoxPlacement>();

            if (activeColors == null || activeColors.Count == 0)
                return tables;

            var placements = new List<BoardBoxPlacement>();
            var seen = new HashSet<PieceColorType>();
            for (int i = 0; i < tables.Length; i++)
            {
                BoardBoxPlacement placement = tables[i];
                if (placement == null || !seen.Add(placement.color) || !activeColors.Contains(placement.color))
                    continue;

                placements.Add(placement);
            }

            return placements.ToArray();
        }

        private BoardBoxPlacement[] GetLevelBoxPlacements()
        {
            LevelData levelData = ResolveLevelData();
            if (levelData == null)
                return Array.Empty<BoardBoxPlacement>();

            var placements = new List<BoardBoxPlacement>();
            var usedCells = new HashSet<Vector2Int>();

            if (levelData.carryBlockJam?.tablePlacements != null)
            {
                for (int i = 0; i < levelData.carryBlockJam.tablePlacements.Count; i++)
                {
                    CarryBlockJamBoxPlacement placement = levelData.carryBlockJam.tablePlacements[i];
                    if (placement == null)
                        continue;

                    var cell = new Vector2Int(placement.row, placement.column);
                    if (!usedCells.Add(cell))
                        continue;

                    placements.Add(new BoardBoxPlacement
                    {
                        row = placement.row,
                        column = placement.column,
                        color = placement.color,
                        isHidden = placement.isHidden,
                        isFrozen = placement.isFrozen,
                        isCurtain = placement.isCurtain,
                        curtainColor = PieceColorPalette.IsPaintable(placement.curtainColor)
                            ? placement.curtainColor
                            : placement.color,
                        unlockMoves = Mathf.Max(1, placement.unlockMoves),
                    });
                }
            }

            AppendHiddenBoxPlacementsFromGrid(levelData, placements, usedCells);
            AppendCurtainBoxPlacementsFromGrid(levelData, placements, usedCells);
            AppendFrozenBoxPlacementsFromGrid(levelData, placements, usedCells);
            return placements.ToArray();
        }

        private static void AppendHiddenBoxPlacementsFromGrid(
            LevelData levelData,
            List<BoardBoxPlacement> placements,
            HashSet<Vector2Int> usedCells)
        {
            if (levelData?.colorCells == null || placements == null || usedCells == null)
                return;

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if ((cell.flag & LevelCellFlag.Hidden) == 0)
                    continue;

                var gridCell = new Vector2Int(cell.row, cell.column);
                if (!usedCells.Add(gridCell))
                    continue;

                if (!PieceColorPalette.IsPaintable(cell.color))
                {
                    Debug.LogWarning(
                        $"[CarryBlockJam] Hidden cell [{cell.row},{cell.column}] needs a color to spawn a hidden box.");
                    usedCells.Remove(gridCell);
                    continue;
                }

                placements.Add(BoardBoxPlacement.Create(cell.row, cell.column, cell.color, isHidden: true));
            }
        }

        private static void AppendCurtainBoxPlacementsFromGrid(
            LevelData levelData,
            List<BoardBoxPlacement> placements,
            HashSet<Vector2Int> usedCells)
        {
            if (levelData?.colorCells == null || placements == null || usedCells == null)
                return;

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if ((cell.flag & LevelCellFlag.Curtain) == 0)
                    continue;

                var gridCell = new Vector2Int(cell.row, cell.column);
                if (!usedCells.Add(gridCell))
                    continue;

                if (!PieceColorPalette.IsPaintable(cell.color))
                {
                    Debug.LogWarning(
                        $"[CarryBlockJam] Curtain cell [{cell.row},{cell.column}] needs a table color.");
                    usedCells.Remove(gridCell);
                    continue;
                }

                PieceColorType collectColor = PieceColorPalette.IsPaintable(cell.secondaryColor)
                    ? cell.secondaryColor
                    : cell.color;
                if (!PieceColorPalette.IsPaintable(collectColor))
                {
                    Debug.LogWarning(
                        $"[CarryBlockJam] Curtain cell [{cell.row},{cell.column}] needs a collect (plate) color.");
                    usedCells.Remove(gridCell);
                    continue;
                }

                placements.Add(BoardBoxPlacement.CreateCurtain(
                    cell.row,
                    cell.column,
                    cell.color,
                    collectColor));
            }
        }

        private static void AppendFrozenBoxPlacementsFromGrid(
            LevelData levelData,
            List<BoardBoxPlacement> placements,
            HashSet<Vector2Int> usedCells)
        {
            if (levelData?.colorCells == null || placements == null || usedCells == null)
                return;

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if ((cell.flag & LevelCellFlag.Ice) == 0)
                    continue;

                var gridCell = new Vector2Int(cell.row, cell.column);
                if (!usedCells.Add(gridCell))
                    continue;

                PieceColorType color = cell.color;
                if (!PieceColorPalette.IsPaintable(color))
                {
                    // Ice cells need a table color; fall back so M_Ice still spawns.
                    color = PieceColorType.Pink;
                    Debug.LogWarning(
                        $"[CarryBlockJam] Frozen cell [{cell.row},{cell.column}] had no color; " +
                        "using Pink so the ice table can spawn. Set a color in Level Creator.");
                }

                placements.Add(BoardBoxPlacement.CreateFrozen(
                    cell.row,
                    cell.column,
                    color,
                    Mathf.Max(1, cell.flagValue)));
            }
        }

        private BoardPlatePlacement[] GetLevelPlatePlacements()
        {
            LevelData levelData = ResolveLevelData();
            if (levelData?.carryBlockJam?.platePlacements == null || levelData.carryBlockJam.platePlacements.Count == 0)
                return Array.Empty<BoardPlatePlacement>();

            var placements = new List<BoardPlatePlacement>();
            for (int i = 0; i < levelData.carryBlockJam.platePlacements.Count; i++)
            {
                CarryBlockJamPlatePlacement placement = levelData.carryBlockJam.platePlacements[i];
                if (placement == null)
                    continue;

                int resolvedCount = Mathf.Max(1, placement.count);
                for (int countIndex = 0; countIndex < resolvedCount; countIndex++)
                    placements.Add(BoardPlatePlacement.Create(placement.row, placement.column, placement.color));
            }

            return placements.ToArray();
        }

        private static CarryBlockJamBoardPiece GetTopStackPiece(CarryBlockJamBoardPiece basePiece)
        {
            CarryBlockJamBoardPiece current = basePiece;
            while (current != null && current.StackedAbove != null)
                current = current.StackedAbove;

            return current;
        }
    }
}
