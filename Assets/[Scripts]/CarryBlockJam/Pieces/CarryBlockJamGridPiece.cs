using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    public abstract class CarryBlockJamGridPiece : MonoBehaviour
    {
        [SerializeField] private PieceColorType color = PieceColorType.None;
        [SerializeField] private Vector3 gridOffset;
        [SerializeField] private Vector3 gridRotationEuler;

        public PieceColorType Color => color;
        public Vector3 GridOffset => gridOffset;
        public int Row { get; private set; } = -1;
        public int Column { get; private set; } = -1;

        public virtual void Configure(PieceColorType pieceColor, Vector3 pieceGridOffset, Vector3 pieceGridRotationEuler)
        {
            color = pieceColor;
            gridOffset = pieceGridOffset;
            gridRotationEuler = pieceGridRotationEuler;
        }

        public virtual void PlaceOnGrid(PuzzleGrid grid, Transform parent, int row, int column)
        {
            if (grid == null || parent == null)
                return;

            transform.SetParent(parent, false);
            transform.localPosition = grid.GetLocalPosition(row, column) + gridOffset;
            transform.localRotation = Quaternion.Euler(gridRotationEuler);
            Row = row;
            Column = column;
        }

        public virtual void ClearGridPosition()
        {
            Row = -1;
            Column = -1;
        }
    }
}
