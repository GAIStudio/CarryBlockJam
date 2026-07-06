using UnityEngine;

namespace CarryBlockJam
{
    public class CarryBlockJamPlate : CarryBlockJamGridPiece
    {
        [SerializeField] private int plateCount = 1;

        public int PlateCount => plateCount;

        public void SetPlateCount(int count)
        {
            plateCount = Mathf.Max(0, count);
        }

        public bool CanMergeWith(CarryBlockJamPlate other) =>
            other != null && other.Color == Color;

        public void AddPlates(int count)
        {
            if (count <= 0)
                return;

            plateCount += count;
        }

        public int RemovePlates(int count)
        {
            if (count <= 0 || plateCount <= 0)
                return 0;

            int removed = Mathf.Min(plateCount, count);
            plateCount -= removed;
            return removed;
        }
    }
}
