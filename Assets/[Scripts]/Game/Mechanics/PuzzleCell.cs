using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Logical grid cell. Visual slot lives on <see cref="Slot"/>; gameplay can track <see cref="Occupant"/>.
    /// </summary>
    public class PuzzleCell
    {
        public int Row { get; }
        public int Column { get; }
        public Transform Slot { get; }
        public bool IsWall { get; set; }

        public GameObject Occupant { get; set; }

        public PuzzleCell(int row, int column, Transform slot, bool isWall)
        {
            Row = row;
            Column = column;
            Slot = slot;
            IsWall = isWall;
        }

        public bool IsEmpty => Occupant == null && !IsWall;

        public bool IsWalkable => !IsWall;
    }
}
