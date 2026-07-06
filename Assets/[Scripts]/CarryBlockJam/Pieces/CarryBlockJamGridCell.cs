using UnityEngine;

namespace CarryBlockJam
{
    public class CarryBlockJamGridCell : MonoBehaviour
    {
        [SerializeField] private int row;
        [SerializeField] private int column;
        [SerializeField] private bool blocked;
        [SerializeField] private CarryBlockJamBox box;
        [SerializeField] private CarryBlockJamPlate plateStack;
        [SerializeField] private CarryBlockJamStickman stickman;

        public int Row => row;
        public int Column => column;
        public bool Blocked => blocked;
        public CarryBlockJamBox Box => box;
        public CarryBlockJamPlate PlateStack => plateStack;
        public CarryBlockJamStickman Stickman => stickman;

        public void Configure(int cellRow, int cellColumn, bool isBlocked)
        {
            row = cellRow;
            column = cellColumn;
            blocked = isBlocked;
        }

        public void SetBox(CarryBlockJamBox value) => box = value;
        public void SetPlateStack(CarryBlockJamPlate value) => plateStack = value;
        public void SetStickman(CarryBlockJamStickman value) => stickman = value;

        public bool IsEmptyForMovement() => !blocked && box == null && plateStack == null && stickman == null;
    }
}
