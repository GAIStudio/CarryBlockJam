using GAITemplate;
using System.Collections.Generic;
using UnityEngine;

namespace CarryBlockJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PuzzleGrid))]
    public class CarryBlockJamSimpleBoard : MonoBehaviour
    {
        public const int DefaultRows = 6;
        public const int DefaultColumns = 6;
        public const float DefaultSpacing = 1.1f;
        public static readonly Vector3 DefaultCellScale = Vector3.one;

        [SerializeField] private int rows = DefaultRows;
        [SerializeField] private int columns = DefaultColumns;
        [SerializeField] private float gridSpacingX = DefaultSpacing;
        [SerializeField] private float gridSpacingZ = DefaultSpacing;
        [SerializeField] private Vector3 cellScale = DefaultCellScale;
        [SerializeField] private PieceColorType cellColor = PieceColorType.Grey;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Material cellMaterial;
        [SerializeField] private CarryBlockJamPrefabSettings prefabSettings;
        [SerializeField] private Transform cellsRoot;
        [SerializeField] private Transform exitsRoot;

        [Header("Exit Label (All Levels)")]
        [SerializeField] private BoardExitLabelSettings exitLabel = BoardExitLabelSettings.CreateDefault();

        [Header("Exits")]
        [HideInInspector]
        [SerializeField] private List<BoardExitSettings> exits = new List<BoardExitSettings>();

        public int Rows => rows;
        public int Columns => columns;
        public float GridSpacingX => gridSpacingX;
        public float GridSpacingZ => gridSpacingZ;
        public float CellScale => cellScale.x;
        public Vector3 CellScaleXYZ => cellScale;
        public PieceColorType CellColor => cellColor;
        public GameObject CellPrefab => cellPrefab;
        public Material CellMaterial => cellMaterial;
        public CarryBlockJamPrefabSettings PrefabSettings => prefabSettings;
        public BoardExitLabelSettings ExitLabel => exitLabel;
        public Transform CellsRoot => cellsRoot;
        public Transform ExitsRoot => exitsRoot;
        public IReadOnlyList<BoardExitSettings> Exits => exits;

        public void RefreshExitLabelVisuals()
        {
            CarryBlockJamExit[] runtimeExits = GetComponentsInChildren<CarryBlockJamExit>(true);
            for (int i = 0; i < runtimeExits.Length; i++)
            {
                if (runtimeExits[i] != null)
                    runtimeExits[i].ApplyLabelPresentation(exitLabel);
            }
        }

        private void Awake()
        {
            EnsureVisualRoots();
            EnsureGridLayout();
            EnsureRuntimeSystems();
        }

        public void RegisterGrid() => EnsureGridLayout();

        public void EnsureGridLayout()
        {
            EnsureVisualRoots();
            PuzzleGrid grid = GetComponent<PuzzleGrid>();
            grid.BeginLayout(rows, columns, gridSpacingX, gridSpacingZ);

            Transform root = cellsRoot != null ? cellsRoot : transform.Find("Cells");
            if (root != null)
            {
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform cell = root.GetChild(i);
                    if (!TryParseCellName(cell.name, out int row, out int col))
                        continue;

                    grid.RegisterCell(row, col, cell, false);
                }
            }

            grid.EndLayout();
        }

        private static bool TryParseCellName(string name, out int row, out int column)
        {
            row = 0;
            column = 0;
            if (!name.StartsWith("Cell_"))
                return false;

            string[] parts = name.Split('_');
            return parts.Length == 3 &&
                   int.TryParse(parts[1], out row) &&
                   int.TryParse(parts[2], out column);
        }

        private void EnsureRuntimeSystems()
        {
            if (GetComponent<CarryBlockJamLevelController>() == null)
                gameObject.AddComponent<CarryBlockJamLevelController>();

            if (GetComponent<CarryBlockJamRuntimePieceSpawner>() == null)
                gameObject.AddComponent<CarryBlockJamRuntimePieceSpawner>();

            if (GetComponent<CarryBlockJamSwipeController>() == null)
                gameObject.AddComponent<CarryBlockJamSwipeController>();
        }

        public void ApplyLevelData(LevelData levelData) => ApplyLevelData(levelData, rebuildVisuals: true);

        public void SyncLevelSettings(LevelData levelData)
        {
            if (levelData == null)
                return;

            rows = Mathf.Max(1, levelData.gridRows);
            columns = Mathf.Max(1, levelData.gridColumns);

            if (levelData.carryBlockJam != null)
            {
                CarryBlockJamExitLayout.NormalizeExits(
                    levelData.carryBlockJam.exits,
                    Mathf.Max(1, levelData.gridRows),
                    Mathf.Max(1, levelData.gridColumns));

                cellScale = levelData.carryBlockJam.gridCellScale;
                gridSpacingX = Mathf.Max(0.01f, levelData.carryBlockJam.gridSpacingX);
                gridSpacingZ = Mathf.Max(0.01f, levelData.carryBlockJam.gridSpacingZ);
                exits = BuildExitSettings(levelData.carryBlockJam.exits);
            }

            EnsureVisualRoots();
        }

        public void ApplyLevelData(LevelData levelData, bool rebuildVisuals)
        {
            SyncLevelSettings(levelData);

            if (rebuildVisuals)
                RebuildRuntimeCells(force: true);
            else
                EnsureGridLayout();
        }

        public BoardExitSettings GetExit(BoardBorderSide side, PieceColorType color)
        {
            if (exits == null)
                return null;

            for (int i = 0; i < exits.Count; i++)
            {
                BoardExitSettings exit = exits[i];
                if (exit != null && exit.side == side && exit.color == color)
                    return exit;
            }

            return null;
        }

        private void EnsureVisualRoots()
        {
            if (cellsRoot == null)
                cellsRoot = EnsureChild("Cells");

            if (exitsRoot == null)
                exitsRoot = EnsureChild("Exits");
        }

        private Transform EnsureChild(string name)
        {
            Transform child = transform.Find(name);
            if (child != null)
                return child;

            var childObject = new GameObject(name);
            child = childObject.transform;
            child.SetParent(transform, false);
            return child;
        }

        private void RebuildRuntimeCells(bool force = false)
        {
            if (!Application.isPlaying || cellsRoot == null)
                return;

            if (!force && cellsRoot.childCount > 0)
                return;

            for (int i = cellsRoot.childCount - 1; i >= 0; i--)
                Destroy(cellsRoot.GetChild(i).gameObject);

            PuzzleGrid grid = GetComponent<PuzzleGrid>();
            if (grid == null)
                return;

            grid.BeginLayout(rows, columns, gridSpacingX, gridSpacingZ);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Vector3 localPos = grid.GetLocalPosition(row, column);

                    var slot = new GameObject($"Cell_{row}_{column}");
                    slot.transform.SetParent(cellsRoot, false);
                    slot.transform.localPosition = localPos;
                    slot.transform.localRotation = Quaternion.identity;
                    slot.transform.localScale = Vector3.one;

                    CreateCellVisual(slot.transform);
                    grid.RegisterCell(row, column, slot.transform, false);
                }
            }

            grid.EndLayout();
        }

        private void CreateCellVisual(Transform parent)
        {
            if (parent == null)
                return;

            GameObject visual = null;
            if (cellPrefab != null)
            {
                visual = Instantiate(cellPrefab, parent, false);
                visual.name = cellPrefab.name;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Cell";
                visual.transform.SetParent(parent, false);
            }

            if (visual == null)
                return;

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = cellScale;

            if (cellMaterial != null)
            {
                ApplyCellMaterial(visual, cellMaterial);
            }
            else if (cellPrefab == null)
            {
                GamePiece piece = visual.GetComponent<GamePiece>();
                if (piece == null)
                    piece = visual.AddComponent<GamePiece>();
                piece.ApplyColor(cellColor);
            }

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        private static void ApplyCellMaterial(GameObject visual, Material material)
        {
            if (visual == null || material == null)
                return;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterial = material;
            }
        }

        private static List<BoardExitSettings> BuildExitSettings(List<CarryBlockJamExitDefinition> definitions)
        {
            var results = new List<BoardExitSettings>();
            if (definitions == null)
                return results;

            for (int i = 0; i < definitions.Count; i++)
            {
                CarryBlockJamExitDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                PieceColorType initialColor = PieceColorType.None;
                if (definition.goals != null && definition.goals.Count > 0 && definition.goals[0] != null)
                    initialColor = definition.goals[0].color;

                results.Add(new BoardExitSettings
                {
                    side = definition.side,
                    startIndex = definition.startIndex,
                    length = Mathf.Max(1, definition.length),
                    color = initialColor,
                    positionOffset = definition.positionOffset,
                    rotation = definition.rotation,
                });
            }

            return results;
        }
    }
}
