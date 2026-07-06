using UnityEngine;

namespace CarryBlockJam
{
    public class CarryBlockJamBox : CarryBlockJamGridPiece
    {
        [SerializeField] private int storedPlateCount;

        public int StoredPlateCount => storedPlateCount;

        public bool CanStoreColor(CarryBlockJamPlate plate) =>
            plate != null && plate.Color == Color;

        public void AddStoredPlates(int count)
        {
            if (count <= 0)
                return;

            storedPlateCount += count;
        }

        public int RemoveStoredPlates(int count)
        {
            if (count <= 0 || storedPlateCount <= 0)
                return 0;

            int removed = Mathf.Min(storedPlateCount, count);
            storedPlateCount -= removed;
            return removed;
        }
    }
}
