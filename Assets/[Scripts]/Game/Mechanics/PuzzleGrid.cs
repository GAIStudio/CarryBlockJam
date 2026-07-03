using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Logical grid indexed by row/column. Uses the same spacing math as <see cref="PuzzleBoardLayout"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PuzzleGrid : MonoBehaviour
    {
        private PuzzleCell[,] _cells;
        private int _rows;
        private int _columns;
        private float _spacingX;
        private float _spacingZ;
        private bool _isBuilt;

        public int Rows => _rows;
        public int Columns => _columns;
        public float GridSpacingX => _spacingX;
        public float GridSpacingZ => _spacingZ;
        public bool IsBuilt => _isBuilt;

        public void BeginLayout(int rows, int columns, float spacingX, float spacingZ)
        {
            _rows = Mathf.Max(0, rows);
            _columns = Mathf.Max(0, columns);
            _spacingX = Mathf.Max(0.01f, spacingX);
            _spacingZ = Mathf.Max(0.01f, spacingZ);
            _cells = _rows > 0 && _columns > 0 ? new PuzzleCell[_rows, _columns] : null;
            _isBuilt = false;
        }

        public void RegisterCell(int row, int column, Transform slot, bool isWall = false)
        {
            if (_cells == null || !IsInside(row, column) || slot == null)
                return;

            _cells[row, column] = new PuzzleCell(row, column, slot, isWall);
        }

        public void EndLayout()
        {
            _isBuilt = _cells != null && _rows > 0 && _columns > 0;
        }

        public Vector3 GetLocalPosition(int row, int column)
        {
            float offsetX = (_columns - 1) * _spacingX * 0.5f;
            float offsetZ = (_rows - 1) * _spacingZ * 0.5f;
            // Row 0 → +Z (back/top of screen), Row N-1 → -Z (front/bottom).
            // Editör konvansiyonu ile uyumlu: [0,0] sol-üst.
            return new Vector3(
                column * _spacingX - offsetX,
                0f,
                offsetZ - row * _spacingZ);
        }

        public Vector3 GetWorldPosition(int row, int column)
        {
            return transform.TransformPoint(GetLocalPosition(row, column));
        }

        public bool TryGetCell(int row, int column, out PuzzleCell cell)
        {
            if (_cells != null && IsInside(row, column))
            {
                cell = _cells[row, column];
                return cell != null;
            }

            cell = null;
            return false;
        }

        public bool TryGetCell(Vector3 worldPosition, out PuzzleCell cell)
        {
            if (TryGetGridIndices(worldPosition, out int row, out int column))
                return TryGetCell(row, column, out cell);

            cell = null;
            return false;
        }

        /// <summary>
        /// Inverse of <see cref="GetLocalPosition"/>. Must stay in sync with layout placement.
        /// </summary>
        public bool TryGetGridIndices(Vector3 worldPosition, out int row, out int column)
        {
            row = 0;
            column = 0;

            if (_columns <= 0 || _rows <= 0)
                return false;

            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            column = Mathf.RoundToInt(localPos.x / _spacingX + (_columns - 1) * 0.5f);
            // Z mapping ters çevrildi: row = (rows-1)/2 - z/spacing
            row = Mathf.RoundToInt((_rows - 1) * 0.5f - localPos.z / _spacingZ);
            return IsInside(row, column);
        }

        public bool IsInside(int row, int column)
        {
            return row >= 0 && row < _rows && column >= 0 && column < _columns;
        }

        public void SetWall(int row, int column, bool isWall)
        {
            if (!TryGetCell(row, column, out PuzzleCell cell) || cell == null)
                return;

            cell.IsWall = isWall;
        }

        public void ClearOccupant(int row, int column)
        {
            if (TryGetCell(row, column, out PuzzleCell cell) && cell != null)
                cell.Occupant = null;
        }
    }
}
