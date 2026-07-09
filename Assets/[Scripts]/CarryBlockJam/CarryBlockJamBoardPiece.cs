using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    public enum CarryBlockJamPieceKind
    {
        Cylinder = 0,
        Box = 1,
        Plate = 2,
    }

    [DisallowMultipleComponent]
    public class CarryBlockJamBoardPiece : MonoBehaviour
    {
        [SerializeField] private CarryBlockJamPieceKind kind;
        [SerializeField] private PieceColorType color = PieceColorType.None;
        [SerializeField] private Vector3 gridOffset;
        [SerializeField] private Vector3 carriedOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private Vector3 stackedOffset = new Vector3(0f, 0.72f, 0f);
        [SerializeField] private Vector3 gridRotationEuler;

        [SerializeField] private bool isColorHidden;
        [SerializeField] private bool isFrozen;

        public CarryBlockJamPieceKind Kind => kind;
        public PieceColorType Color => isColorHidden || isFrozen ? PieceColorType.None : color;
        public PieceColorType TrueColor => color;
        public bool IsColorHidden => isColorHidden;
        public bool IsFrozen => isFrozen;
        public Vector3 GridOffset => gridOffset;
        public Vector3 StackedOffset => stackedOffset;
        public int Row { get; private set; } = -1;
        public int Column { get; private set; } = -1;
        public CarryBlockJamBoardPiece StackedBelow { get; private set; }
        public CarryBlockJamBoardPiece StackedAbove { get; private set; }

        public void Initialize(
            CarryBlockJamPieceKind pieceKind,
            PieceColorType pieceColor,
            Vector3 pieceGridOffset,
            Vector3 pieceCarriedOffset,
            Vector3 pieceGridRotationEuler = default)
        {
            kind = pieceKind;
            color = pieceColor;
            gridOffset = pieceGridOffset;
            carriedOffset = pieceCarriedOffset;
            gridRotationEuler = pieceGridRotationEuler;
        }

        public void SetColorHidden(bool hidden) => isColorHidden = hidden;

        public void RevealHiddenColor() => isColorHidden = false;

        public void SetFrozen(bool frozen) => isFrozen = frozen;

        public void Unfreeze() => isFrozen = false;

        public void PlaceOnGrid(PuzzleGrid grid, Transform parent, int row, int column)
        {
            if (grid == null || parent == null)
                return;

            transform.SetParent(parent, false);
            transform.localPosition = grid.GetLocalPosition(row, column) + gridOffset;
            transform.localRotation = Quaternion.Euler(gridRotationEuler);
            Row = row;
            Column = column;
        }

        public void AttachToCarrier(Transform carrier)
        {
            if (carrier == null)
                return;

            transform.SetParent(carrier, false);
            transform.localPosition = carriedOffset;
            transform.localRotation = Quaternion.identity;
            Row = -1;
            Column = -1;
        }

        public void StackOnPiece(CarryBlockJamBoardPiece basePiece)
        {
            if (basePiece == null)
                return;

            ClearStackLinks();
            transform.SetParent(basePiece.transform, false);
            transform.localPosition = stackedOffset;
            transform.localRotation = Quaternion.Euler(gridRotationEuler);
            Row = basePiece.Row;
            Column = basePiece.Column;
            StackedBelow = basePiece;
            basePiece.StackedAbove = this;
        }

        public void ClearStackLinks()
        {
            if (StackedBelow != null && StackedBelow.StackedAbove == this)
                StackedBelow.StackedAbove = null;

            if (StackedAbove != null && StackedAbove.StackedBelow == this)
                StackedAbove.StackedBelow = null;

            StackedBelow = null;
            StackedAbove = null;
        }
    }
}
