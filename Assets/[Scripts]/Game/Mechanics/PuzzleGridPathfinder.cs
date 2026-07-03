using System.Collections.Generic;

namespace GAITemplate
{
    /// <summary>
    /// Grid reachability checks (Beads_Out-style flood fill to row 0).
    /// </summary>
    public static class PuzzleGridPathfinder
    {
        private static readonly int[] RowOffsets = { -1, 1, 0, 0 };
        private static readonly int[] ColumnOffsets = { 0, 0, -1, 1 };

        public static bool CanReachRowZero(PuzzleGrid grid, int startRow, int startColumn)
        {
            if (grid == null || !grid.IsBuilt)
                return false;

            if (startRow == 0)
                return grid.TryGetCell(0, startColumn, out PuzzleCell frontCell) && frontCell != null && frontCell.IsWalkable;

            if (!grid.TryGetCell(startRow, startColumn, out PuzzleCell start) || start == null || !start.IsEmpty)
                return false;

            var visited = new bool[grid.Rows, grid.Columns];
            var queue = new Queue<(int row, int column)>();
            queue.Enqueue((startRow, startColumn));
            visited[startRow, startColumn] = true;

            while (queue.Count > 0)
            {
                (int row, int column) = queue.Dequeue();

                if (row == 0)
                    return true;

                for (int i = 0; i < 4; i++)
                {
                    int nextRow = row + RowOffsets[i];
                    int nextColumn = column + ColumnOffsets[i];

                    if (!grid.IsInside(nextRow, nextColumn) || visited[nextRow, nextColumn])
                        continue;

                    if (!grid.TryGetCell(nextRow, nextColumn, out PuzzleCell next) || next == null || !next.IsEmpty)
                        continue;

                    visited[nextRow, nextColumn] = true;
                    queue.Enqueue((nextRow, nextColumn));
                }
            }

            return false;
        }
    }
}
