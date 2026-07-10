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
        [SerializeField] private bool isCurtained;

        public CarryBlockJamPieceKind Kind => kind;
        public PieceColorType Color => isColorHidden || isFrozen || isCurtained ? PieceColorType.None : color;
        public PieceColorType TrueColor => color;
        public bool IsColorHidden => isColorHidden;
        public bool IsFrozen => isFrozen;
        public bool IsCurtained => isCurtained;
        public Vector3 GridOffset => gridOffset;
        public Vector3 StackedOffset => stackedOffset;
        public Vector3 CarriedOffset => carriedOffset;
        public int Row { get; private set; } = -1;
        public int Column { get; private set; } = -1;
        public CarryBlockJamBoardPiece StackedBelow { get; private set; }
        public CarryBlockJamBoardPiece StackedAbove { get; private set; }

        public void Initialize(
            CarryBlockJamPieceKind pieceKind,
            PieceColorType pieceColor,
            Vector3 pieceGridOffset,
            Vector3 pieceCarriedOffset,
            Vector3 pieceGridRotationEuler = default,
            Vector3 pieceStackedOffset = default)
        {
            kind = pieceKind;
            color = pieceColor;
            gridOffset = pieceGridOffset;
            carriedOffset = pieceCarriedOffset;
            gridRotationEuler = pieceGridRotationEuler;
            if (pieceStackedOffset != default)
                stackedOffset = pieceStackedOffset;
        }

        public void SetStackedOffset(Vector3 offset) => stackedOffset = offset;

        /// <summary>
        /// Local position where the next plate should sit on this piece.
        /// Tables use the mesh top; plates keep their configured stack step.
        /// </summary>
        public Vector3 GetStackAttachLocalPosition()
        {
            if (kind == CarryBlockJamPieceKind.Box && TryGetLocalRendererTopY(out float topY))
                return new Vector3(0f, topY + 0.02f, 0f);

            return stackedOffset;
        }

        public void SetColorHidden(bool hidden) => isColorHidden = hidden;

        public void RevealHiddenColor() => isColorHidden = false;

        public void SetFrozen(bool frozen) => isFrozen = frozen;

        public void Unfreeze() => isFrozen = false;

        public void SetCurtained(bool curtained) => isCurtained = curtained;

        public void OpenCurtain() => isCurtained = false;

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
            transform.localPosition = basePiece.GetStackAttachLocalPosition();
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

        private bool TryGetLocalRendererTopY(out float topY)
        {
            topY = 0f;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool found = false;
            float maxY = float.MinValue;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                Bounds worldBounds = renderer.bounds;
                Vector3 localMin = transform.InverseTransformPoint(worldBounds.min);
                Vector3 localMax = transform.InverseTransformPoint(worldBounds.max);
                float rendererTop = Mathf.Max(localMin.y, localMax.y);
                if (!found || rendererTop > maxY)
                {
                    maxY = rendererTop;
                    found = true;
                }
            }

            if (!found)
                return false;

            topY = maxY;
            return true;
        }
    }
}
