using UnityEngine;

namespace GAITemplate
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PuzzleGrid))]
    public class PuzzleBoardLayout : MonoBehaviour
    {
        public PuzzleMechanicType mechanicType = PuzzleMechanicType.Grid;

        [Header("Grid")]
        public int rows = 4;
        public int columns = 4;
        public float gridSpacingX = 1f;
        public float gridSpacingZ = 1f;

        [Tooltip("Per-level layout from LevelData. Set at runtime by LevelBase.")]
        public LevelData levelData;

        [Tooltip("Legacy spawn profile. Ignored when levelData is set.")]
        public GridSpawnProfile spawnProfile;

        [Tooltip("Fallback when catalog resolution returns nothing.")]
        public GameObject cellPrefab;

        [Tooltip("Treat empty (None) level cells as holes: no piece spawns and the cell is a logical " +
                 "wall (non-walkable). Lets levels form irregular shapes that walls wrap around.")]
        public bool emptyCellsAreHoles = true;

        [Header("Floor Triggers")]
        [Tooltip("Add a trigger BoxCollider to each floor cell so WallAreaBuilder can detect and " +
                 "carve away walls that overlap the play area.")]
        public bool addFloorTriggers = true;

        [Tooltip("Layer assigned to the generated floor trigger. -1 keeps the slot's default layer.")]
        public int floorTriggerLayer = -1;

        [Min(0.01f)]
        [Tooltip("Trigger size as a fraction of the cell spacing on X/Z.")]
        public float floorTriggerScale = 0.5f;

        [Min(0.01f)]
        [Tooltip("Trigger height (Y) for the floor box.")]
        public float floorTriggerHeight = 0.2f;

        [Header("Slide Lane")]
        public int laneCount = 3;
        public int depth = 5;
        public float laneSpacing = 1.2f;
        public float depthSpacing = 0.8f;
        public GameObject piecePrefab;

        [Header("Generated")]
        public Transform slotsRoot;

        public PuzzleGrid Grid { get; private set; }

        private void Awake()
        {
            Grid = GetComponent<PuzzleGrid>();

            if (Application.isPlaying)
                RebuildLayout();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Grid == null)
                Grid = GetComponent<PuzzleGrid>();
        }

        [ContextMenu("Rebuild Layout")]
        private void RebuildLayoutFromContextMenu() => RebuildLayout();
#endif

        public void RebuildLayout()
        {
            if (Grid == null)
                Grid = GetComponent<PuzzleGrid>();

            EnsureSlotsRoot();
            ClearChildren(slotsRoot);

            switch (mechanicType)
            {
                case PuzzleMechanicType.Grid:
                    BuildGrid();
                    break;
                case PuzzleMechanicType.SlideLane:
                    BuildSlideLanes();
                    break;
            }
        }

        private void EnsureSlotsRoot()
        {
            if (slotsRoot != null && slotsRoot != transform)
                return;

            Transform existing = transform.Find("Cells");
            if (existing == null)
                existing = transform.Find("Slots");

            if (existing != null)
            {
                slotsRoot = existing;
                return;
            }

            var cells = new GameObject("Cells");
            cells.transform.SetParent(transform, false);
            slotsRoot = cells.transform;
        }

        private void BuildGrid()
        {
            Grid.BeginLayout(rows, columns, gridSpacingX, gridSpacingZ);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    bool isTunnel = levelData != null &&
                                    levelData.HasCellFlag(row, col, LevelCellFlag.Tunnel);
                    bool isWall   = !isTunnel && IsWallCell(row, col);

                    Vector3 localPos = Grid.GetLocalPosition(row, col);

                    GameObject prefab = isWall   ? null
                                      : isTunnel ? ResolveTunnelPrefab()
                                      : ResolveGridPrefab(row, col);

                    Transform slot = CreateSlot($"Cell_{row}_{col}", localPos, prefab);

                    if (isTunnel)
                        ApplyTunnelRotation(slot, row, col);
                    else
                        ApplyLevelColor(slot, row, col);

                    if (!isWall && addFloorTriggers)
                        AddFloorTrigger(slot);
                    Grid.RegisterCell(row, col, slot, isWall);
                }
            }

            // Post-pass: Tunnel cell'lere TunnelSpawner ekle + exit cell'in piece'ini bağla.
            foreach (var t in CollectTunnelDescriptors())
                InitializeTunnel(t);

            Grid.EndLayout();
        }

        // ── Tunnel build helpers ─────────────────────────────────────────────────────

        private struct TunnelDescriptor
        {
            public int row;
            public int col;
            public int exitRow;
            public int exitCol;
            public CellDirection direction;
            public PieceColorType[] queueColors;
        }

        private System.Collections.Generic.List<TunnelDescriptor> CollectTunnelDescriptors()
        {
            var list = new System.Collections.Generic.List<TunnelDescriptor>();
            if (levelData == null) return list;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (!levelData.HasCellFlag(row, col, LevelCellFlag.Tunnel))
                        continue;

                    var dir = levelData.GetCellDirection(row, col);
                    (int dr, int dc) = DirectionToDelta(dir);
                    list.Add(new TunnelDescriptor
                    {
                        row         = row,
                        col         = col,
                        exitRow     = row + dr,
                        exitCol     = col + dc,
                        direction   = dir,
                        queueColors = levelData.GetCellTunnelPieces(row, col),
                    });
                }
            }
            return list;
        }

        private static (int dr, int dc) DirectionToDelta(CellDirection dir) => dir switch
        {
            CellDirection.Front => ( 1,  0),  // +Z (row +)
            CellDirection.Back  => (-1,  0),  // -Z (row -)
            CellDirection.Right => ( 0,  1),  // +X (col +)
            CellDirection.Left  => ( 0, -1),  // -X (col -)
            _                   => ( 0,  0),
        };

        private void InitializeTunnel(TunnelDescriptor t)
        {
            if (!Grid.TryGetCell(t.row, t.col, out PuzzleCell tunnelCell) || tunnelCell?.Slot == null)
                return;

            var spawner = tunnelCell.Slot.GetComponent<TunnelSpawner>();
            if (spawner == null)
                spawner = tunnelCell.Slot.gameObject.AddComponent<TunnelSpawner>();

            if (!Grid.IsInside(t.exitRow, t.exitCol)) return;
            Grid.TryGetCell(t.exitRow, t.exitCol, out PuzzleCell exitCell);
            if (exitCell?.Slot == null) return;

            Vector3 tunnelLocalPos = Grid.GetLocalPosition(t.row, t.col);
            Vector3 exitLocalPos   = Grid.GetLocalPosition(t.exitRow, t.exitCol);

            // Queue'da TÜM piece'ler — exit cell kendi rengini korur, ilk send'de queue[0] DOMove ile gelir.
            spawner.Init(
                t.queueColors,
                ResolveGridPrefab(t.exitRow, t.exitCol),
                exitCell.Slot.parent,
                Grid,
                t.exitRow,
                t.exitCol,
                tunnelLocalPos,
                exitLocalPos);

            // Exit cell'deki piece'i bu spawner'a bağla → gönderildiğinde queue'dan piece DOMove eder.
            GamePiece exitPiece = exitCell.Slot.GetComponent<GamePiece>();
            if (exitPiece != null)
                exitPiece.sourceTunnel = spawner;
        }

        private GameObject ResolveTunnelPrefab()
        {
            GamePieceCatalog catalog = GameManager.instance != null
                ? GameManager.instance.PieceCatalog
                : null;
            return catalog != null ? catalog.tunnelPrefab : null;
        }

        private void ApplyTunnelRotation(Transform slot, int row, int column)
        {
            if (slot == null || levelData == null) return;

            CellDirection dir = levelData.GetCellDirection(row, column);
            float yaw = dir switch
            {
                CellDirection.Back  => 0f,
                CellDirection.Left  => 90f,
                CellDirection.Front => 180f,
                CellDirection.Right => 270f,
                _                   => 0f,
            };
            slot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void AddFloorTrigger(Transform slot)
        {
            if (slot == null)
                return;

            var triggerObject = new GameObject("FloorTrigger");
            triggerObject.transform.SetParent(slot, false);
            triggerObject.transform.localPosition = Vector3.zero;
            triggerObject.transform.localRotation = Quaternion.identity;
            triggerObject.transform.localScale = Vector3.one;

            if (floorTriggerLayer >= 0 && floorTriggerLayer < 32)
                triggerObject.layer = floorTriggerLayer;

            triggerObject.AddComponent<FloorTriggerMarker>();
            BoxCollider boxCollider = triggerObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(
                Mathf.Max(0.01f, gridSpacingX * floorTriggerScale),
                Mathf.Max(0.01f, floorTriggerHeight),
                Mathf.Max(0.01f, gridSpacingZ * floorTriggerScale));
        }

        private bool IsWallCell(int row, int column)
        {
            if (levelData != null)
                return emptyCellsAreHoles && levelData.GetCellColor(row, column) == PieceColorType.None;

            return spawnProfile != null && spawnProfile.IsWallCell(row, column);
        }

        private void ApplyLevelColor(Transform slot, int row, int column)
        {
            if (levelData == null || slot == null)
                return;

            GamePiece piece = slot.GetComponent<GamePiece>();
            if (piece == null)
                return;

            piece.ApplyColor(levelData.GetCellColor(row, column));
            piece.ApplyFlags(
                levelData.GetCellFlag(row, column),
                levelData.GetCellFlagValue(row, column));
        }

        private GameObject ResolveGridPrefab(int row, int column)
        {
            GamePieceCatalog catalog = GameManager.instance != null
                ? GameManager.instance.PieceCatalog
                : null;

            if (levelData != null)
                return PieceCatalogResolver.ResolveCellPrefab(catalog);

            if (spawnProfile != null && spawnProfile.IsWallCell(row, column))
                return null;

            return catalog != null ? catalog.defaultPiecePrefab : cellPrefab;
        }

        private void BuildSlideLanes()
        {
            float laneOffset = (laneCount - 1) * laneSpacing * 0.5f;

            for (int lane = 0; lane < laneCount; lane++)
            {
                float x = lane * laneSpacing - laneOffset;

                for (int slot = 0; slot < depth; slot++)
                {
                    Vector3 localPos = new Vector3(x, 0f, slot * depthSpacing);
                    CreateSlot($"Lane{lane}_Slot{slot}", localPos, piecePrefab);
                }
            }
        }

        private Transform CreateSlot(string slotName, Vector3 localPosition, GameObject prefab)
        {
            Transform slotTransform;

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, slotsRoot);
                instance.name = slotName;
                slotTransform = instance.transform;
            }
            else
            {
                var slot = new GameObject(slotName);
                slot.transform.SetParent(slotsRoot, false);
                slotTransform = slot.transform;
            }

            slotTransform.localPosition = localPosition;
            slotTransform.localRotation = Quaternion.identity;
            slotTransform.localScale = Vector3.one;
            return slotTransform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                    continue;

                GameObject childObject = child.gameObject;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(childObject, true);
                    continue;
                }
#endif
                Destroy(childObject);
            }
        }
    }
}
