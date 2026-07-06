using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    public class CarryBlockJamStickman : CarryBlockJamGridPiece
    {
        [SerializeField] private PieceColorType carriedColor = PieceColorType.None;
        [SerializeField] private int carriedPlateCount;

        public PieceColorType CarriedColor => carriedColor;
        public int CarriedPlateCount => carriedPlateCount;
        public bool IsCarrying => carriedPlateCount > 0;

        public bool CanCarry(PieceColorType plateColor)
        {
            if (plateColor == PieceColorType.None)
                return false;

            return !IsCarrying || carriedColor == plateColor;
        }

        public void AddCarriedPlates(PieceColorType plateColor, int count)
        {
            if (!CanCarry(plateColor) || count <= 0)
                return;

            carriedColor = plateColor;
            carriedPlateCount += count;
        }

        public void ClearHands()
        {
            carriedColor = PieceColorType.None;
            carriedPlateCount = 0;
        }
    }
}
