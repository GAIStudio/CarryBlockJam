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
        public static readonly Vector3 DefaultCellScale = Vector3.one * 1.5f;

        [SerializeField] private int rows = DefaultRows;
        [SerializeField] private int columns = DefaultColumns;
        [SerializeField] private float gridSpacingX = DefaultSpacing;
        [SerializeField] private float gridSpacingZ = DefaultSpacing;
        [SerializeField] private Vector3 cellScale = DefaultCellScale;
        [SerializeField] private PieceColorType cellColor = PieceColorType.Grey;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private CarryBlockJamPrefabSettings prefabSettings;
        [SerializeField] private Transform cellsRoot;
        [SerializeField] private Transform exitsRoot;

        [Header("Exits")]
        [SerializeField] private List<BoardExitSettings> exits = new List<BoardExitSettings>
        {
            BoardExitSettings.CreateLeftDefault(),
            BoardExitSettings.CreateRightDefault(),
        };

        public int Rows => rows;
        public int Columns => columns;
        public float GridSpacingX => gridSpacingX;
        public float GridSpacingZ => gridSpacingZ;
        public float CellScale => cellScale.x;
        public Vector3 CellScaleXYZ => cellScale;
        public PieceColorType CellColor => cellColor;
        public GameObject CellPrefab => cellPrefab;
        public CarryBlockJamPrefabSettings PrefabSettings => prefabSettings;
        public Transform CellsRoot => cellsRoot;
        public Transform ExitsRoot => exitsRoot;
        public IReadOnlyList<BoardExitSettings> Exits => exits;

        private void Awake()
        {
            EnsureGridLayout();
            EnsureRuntimeSystems();
        }

        public void RegisterGrid() => EnsureGridLayout();

        public void EnsureGridLayout()
        {
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

        public void ApplyLevelData(LevelData levelData)
        {
            if (levelData == null)
                return;

            rows = Mathf.Max(1, levelData.gridRows);
            columns = Mathf.Max(1, levelData.gridColumns);

            if (levelData.carryBlockJam != null)
            {
                gridSpacingX = Mathf.Max(0.01f, levelData.carryBlockJam.gridSpacingX);
                gridSpacingZ = Mathf.Max(0.01f, levelData.carryBlockJam.gridSpacingZ);
            }

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
    }
}
