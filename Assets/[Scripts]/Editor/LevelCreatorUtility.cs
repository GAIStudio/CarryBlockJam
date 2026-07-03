using UnityEditor;
using UnityEngine;

namespace GAITemplate.Editor
{
    public static class LevelCreatorUtility
    {
        public static void WriteGridToLevel(
            LevelData levelData,
            int rows,
            int columns,
            PieceColorType[,] cellColors,
            LevelCellFlag[,] cellFlags = null,
            int[,] cellFlagValues = null,
            CellDirection[,] cellDirections = null,
            System.Collections.Generic.List<PieceColorType>[,] cellTunnelPieces = null)
        {
            if (levelData == null)
                return;

            levelData.gridRows = rows;
            levelData.gridColumns = columns;

            var cells = new System.Collections.Generic.List<LevelColorCell>();
            if (cellColors != null)
            {
                int colorRows = cellColors.GetLength(0);
                int colorColumns = cellColors.GetLength(1);
                for (int row = 0; row < colorRows; row++)
                {
                    for (int column = 0; column < colorColumns; column++)
                    {
                        PieceColorType color = cellColors[row, column];
                        LevelCellFlag flag = ReadAt(cellFlags, row, column);
                        int flagValue = ReadAt(cellFlagValues, row, column);
                        CellDirection direction = ReadAt(cellDirections, row, column);
                        PieceColorType[] tunnelPieces = ReadTunnelPiecesAt(cellTunnelPieces, row, column);

                        // Hem renk hem flag boşsa cell'i hiç kaydetme.
                        if (color == PieceColorType.None && flag == LevelCellFlag.None)
                            continue;

                        cells.Add(new LevelColorCell
                        {
                            row = row,
                            column = column,
                            color = color,
                            flag = flag,
                            flagValue = flagValue,
                            direction = direction,
                            tunnelPieces = tunnelPieces,
                        });
                    }
                }
            }

            levelData.colorCells = cells.ToArray();
            EditorUtility.SetDirty(levelData);
        }

        public static void ReadColorsIntoGrid(LevelData levelData, PieceColorType[,] cellColors)
        {
            if (cellColors == null) return;
            ClearGrid(cellColors);

            if (levelData == null || levelData.colorCells == null) return;

            int rows = cellColors.GetLength(0);
            int columns = cellColors.GetLength(1);

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if (cell.row < 0 || cell.row >= rows || cell.column < 0 || cell.column >= columns)
                    continue;

                cellColors[cell.row, cell.column] = cell.color;
            }
        }

        public static void ReadFlagsIntoGrid(LevelData levelData, LevelCellFlag[,] cellFlags)
        {
            if (cellFlags == null) return;
            ClearGrid(cellFlags);

            if (levelData == null || levelData.colorCells == null) return;

            int rows = cellFlags.GetLength(0);
            int columns = cellFlags.GetLength(1);

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if (cell.row < 0 || cell.row >= rows || cell.column < 0 || cell.column >= columns)
                    continue;

                cellFlags[cell.row, cell.column] = cell.flag;
            }
        }

        public static void ReadFlagValuesIntoGrid(LevelData levelData, int[,] cellFlagValues)
        {
            if (cellFlagValues == null) return;
            ClearGrid(cellFlagValues);

            if (levelData == null || levelData.colorCells == null) return;

            int rows = cellFlagValues.GetLength(0);
            int columns = cellFlagValues.GetLength(1);

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if (cell.row < 0 || cell.row >= rows || cell.column < 0 || cell.column >= columns)
                    continue;

                cellFlagValues[cell.row, cell.column] = cell.flagValue;
            }
        }

        public static void ReadDirectionsIntoGrid(LevelData levelData, CellDirection[,] cellDirections)
        {
            if (cellDirections == null) return;
            ClearGrid(cellDirections);

            if (levelData == null || levelData.colorCells == null) return;

            int rows = cellDirections.GetLength(0);
            int columns = cellDirections.GetLength(1);

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if (cell.row < 0 || cell.row >= rows || cell.column < 0 || cell.column >= columns)
                    continue;

                cellDirections[cell.row, cell.column] = cell.direction;
            }
        }

        public static void ReadTunnelPiecesIntoGrid(LevelData levelData,
            System.Collections.Generic.List<PieceColorType>[,] cellTunnelPieces)
        {
            if (cellTunnelPieces == null) return;

            int rows = cellTunnelPieces.GetLength(0);
            int columns = cellTunnelPieces.GetLength(1);
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                    cellTunnelPieces[row, column] = new System.Collections.Generic.List<PieceColorType>();

            if (levelData == null || levelData.colorCells == null) return;

            for (int i = 0; i < levelData.colorCells.Length; i++)
            {
                LevelColorCell cell = levelData.colorCells[i];
                if (cell.row < 0 || cell.row >= rows || cell.column < 0 || cell.column >= columns)
                    continue;

                cellTunnelPieces[cell.row, cell.column] = cell.tunnelPieces != null
                    ? new System.Collections.Generic.List<PieceColorType>(cell.tunnelPieces)
                    : new System.Collections.Generic.List<PieceColorType>();
            }
        }

        private static PieceColorType[] ReadTunnelPiecesAt(
            System.Collections.Generic.List<PieceColorType>[,] grid, int row, int column)
        {
            if (grid == null) return null;
            if (row < 0 || row >= grid.GetLength(0)) return null;
            if (column < 0 || column >= grid.GetLength(1)) return null;
            var list = grid[row, column];
            return list != null && list.Count > 0 ? list.ToArray() : null;
        }

        private static T ReadAt<T>(T[,] grid, int row, int column)
        {
            if (grid == null) return default;
            if (row < 0 || row >= grid.GetLength(0)) return default;
            if (column < 0 || column >= grid.GetLength(1)) return default;
            return grid[row, column];
        }

        private static void ClearGrid<T>(T[,] grid)
        {
            int rows = grid.GetLength(0);
            int columns = grid.GetLength(1);
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                    grid[row, column] = default;
        }
    }
}
