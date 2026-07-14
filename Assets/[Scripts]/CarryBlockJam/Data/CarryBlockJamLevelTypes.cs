using System;
using System.Collections.Generic;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    public enum CarryBlockJamStickmanSpawnMode
    {
        Center = 0,
        RandomEmptyCell = 1,
        FixedCell = 2,
    }

    [Serializable]
    public struct CarryBlockJamGridCoordinate
    {
        public int row;
        public int column;

        public CarryBlockJamGridCoordinate(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

        public Vector2Int ToVector2Int() => new Vector2Int(row, column);
    }

    [Serializable]
    public class CarryBlockJamColorSetup
    {
        public PieceColorType color = PieceColorType.Red;

        [Min(0)] public int minPlateCount = 1;
        [Min(0)] public int maxPlateCount = 3;
        [Min(1)] public int minPlateStacks = 1;
        [Min(1)] public int maxPlateStacks = 2;

        public int GetValidatedMaxPlateCount() => Mathf.Max(minPlateCount, maxPlateCount);
        public int GetValidatedMaxPlateStacks() => Mathf.Max(minPlateStacks, maxPlateStacks);
    }

    [Serializable]
    public class CarryBlockJamBoxPlacement
    {
        public PieceColorType color = PieceColorType.Red;
        public int row;
        public int column;
        public bool isHidden;
        public bool isFrozen;
        public bool isCurtain;
        public PieceColorType curtainColor = PieceColorType.Purple;
        [Min(1)] public int unlockMoves = 3;
    }

    [Serializable]
    public class CarryBlockJamPlatePlacement
    {
        public PieceColorType color = PieceColorType.Red;
        public int row;
        public int column;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class CarryBlockJamExitGoal
    {
        public PieceColorType color = PieceColorType.Red;
        [Min(1)] public int requiredPlateCount = 3;
    }

    [Serializable]
    public class CarryBlockJamExitDefinition
    {
        [Tooltip("Grid row for this exit (gates cover one cell). Use row 0 for top, last row for bottom.")]
        public int row = -1;

        [Tooltip("Grid column for this exit (gates cover one cell).")]
        public int column = -1;

        [HideInInspector] public BoardBorderSide side = BoardBorderSide.Top;
        [HideInInspector] [Min(0)] public int startIndex = 0;
        [HideInInspector] [Min(1)] public int length = 1;
        [HideInInspector] public Vector3 positionOffset;
        [HideInInspector] public Vector3 rotation;

        [Tooltip("Local scale multiplier for this exit gate model.")]
        public Vector3 modelScale = Vector3.one;

        [Tooltip("Ordered goals for this gate. Gate color follows the current goal.")]
        public List<CarryBlockJamExitGoal> goals = new List<CarryBlockJamExitGoal>
        {
            new CarryBlockJamExitGoal(),
        };

        /// <summary>
        /// Ensures goals exist and syncs border layout from the authored cell
        /// (or migrates legacy side/startIndex when row/column are unset).
        /// </summary>
        public void NormalizeLayout(int rows, int columns)
        {
            if (goals == null)
                goals = new List<CarryBlockJamExitGoal>();

            if (goals.Count == 0)
                goals.Add(new CarryBlockJamExitGoal());

            if (modelScale == Vector3.zero)
                modelScale = Vector3.one;

            length = 1;
            positionOffset = Vector3.zero;
            rotation = Vector3.zero;

            int safeRows = Mathf.Max(1, rows);
            int safeColumns = Mathf.Max(1, columns);

            // Unity deserializes missing row/column as 0 on old assets.
            // Prefer legacy side/startIndex when they disagree with default 0,0.
            bool cellUnset = row < 0 || column < 0;
            bool cellDefaultZero = row == 0 && column == 0;
            bool borderLooksAuthored =
                side != BoardBorderSide.Top ||
                startIndex != 0 ||
                length != 1;

            if (cellUnset || (cellDefaultZero && borderLooksAuthored))
                ApplyCellFromBorder(safeRows, safeColumns);
            else
            {
                row = Mathf.Clamp(row, 0, safeRows - 1);
                column = Mathf.Clamp(column, 0, safeColumns - 1);
                ApplyBorderFromCell(safeRows, safeColumns);
            }
        }

        public void ApplyBorderFromCell(int rows, int columns)
        {
            int safeRows = Mathf.Max(1, rows);
            int safeColumns = Mathf.Max(1, columns);
            row = Mathf.Clamp(row, 0, safeRows - 1);
            column = Mathf.Clamp(column, 0, safeColumns - 1);

            int distTop = row;
            int distBottom = (safeRows - 1) - row;
            int distLeft = column;
            int distRight = (safeColumns - 1) - column;

            int minEdge = Mathf.Min(Mathf.Min(distTop, distBottom), Mathf.Min(distLeft, distRight));
            if (minEdge == distTop)
            {
                side = BoardBorderSide.Top;
                row = 0;
                startIndex = column;
            }
            else if (minEdge == distBottom)
            {
                side = BoardBorderSide.Bottom;
                row = safeRows - 1;
                startIndex = column;
            }
            else if (minEdge == distLeft)
            {
                side = BoardBorderSide.Left;
                column = 0;
                startIndex = row;
            }
            else
            {
                side = BoardBorderSide.Right;
                column = safeColumns - 1;
                startIndex = row;
            }

            length = 1;
        }

        public void ApplyCellFromBorder(int rows, int columns)
        {
            int safeRows = Mathf.Max(1, rows);
            int safeColumns = Mathf.Max(1, columns);
            startIndex = Mathf.Clamp(startIndex, 0, (side == BoardBorderSide.Top || side == BoardBorderSide.Bottom)
                ? safeColumns - 1
                : safeRows - 1);
            length = 1;

            switch (side)
            {
                case BoardBorderSide.Top:
                    row = 0;
                    column = startIndex;
                    break;
                case BoardBorderSide.Bottom:
                    row = safeRows - 1;
                    column = startIndex;
                    break;
                case BoardBorderSide.Left:
                    row = startIndex;
                    column = 0;
                    break;
                default:
                    row = startIndex;
                    column = safeColumns - 1;
                    break;
            }
        }
    }

    [Serializable]
    public class CarryBlockJamPrimitiveVisualSettings
    {
        public GameObject prefab;
        public PrimitiveType primitiveType = PrimitiveType.Cube;
        public Vector3 localScale = Vector3.one;
        public Vector3 localPosition;
        public Vector3 localRotation;
        public bool tintWithPieceColor = true;
    }
}
