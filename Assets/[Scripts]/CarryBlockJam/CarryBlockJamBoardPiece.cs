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
        private const float TableToPlateClearance = 0.02f;
        private const float PlateToPlateClearance = 0.01f;

        [SerializeField] private CarryBlockJamPieceKind kind;
        [SerializeField] private PieceColorType color = PieceColorType.None;
        [SerializeField] private Vector3 gridOffset;
        [SerializeField] private Vector3 carriedOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private Vector3 stackedOffset = new Vector3(0f, 0.3f, 0f);
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
            else if (pieceKind == CarryBlockJamPieceKind.Box)
                stackedOffset = new Vector3(0f, 0.55f, 0f);
            else if (pieceKind == CarryBlockJamPieceKind.Plate)
                stackedOffset = new Vector3(0f, 0.25f, 0f);
        }

        public void SetStackedOffset(Vector3 offset) => stackedOffset = offset;

        /// <summary>
        /// Local position for <paramref name="incomingPlate"/> so it rests above this piece with a gap.
        /// </summary>
        public Vector3 GetStackAttachLocalPosition(CarryBlockJamBoardPiece incomingPlate = null)
        {
            float clearance = kind == CarryBlockJamPieceKind.Box
                ? TableToPlateClearance
                : PlateToPlateClearance;

            if (!TryGetVisualLocalTopY(out float baseTop))
                return stackedOffset;

            float incomingBottom = 0f;
            if (incomingPlate != null)
                incomingPlate.TryGetVisualLocalBottomY(out incomingBottom);

            // Sit the incoming plate's mesh bottom just above this piece's mesh top.
            return new Vector3(0f, baseTop + clearance - incomingBottom, 0f);
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
            transform.localPosition = basePiece.GetStackAttachLocalPosition(this);
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

        private bool TryGetVisualLocalTopY(out float topY)
        {
            return TryGetVisualLocalExtents(out _, out topY);
        }

        private bool TryGetVisualLocalBottomY(out float bottomY)
        {
            return TryGetVisualLocalExtents(out bottomY, out _);
        }

        private bool TryGetVisualLocalExtents(out float bottomY, out float topY)
        {
            bottomY = 0f;
            topY = 0f;

            Transform visual = transform.Find("Visual");
            if (visual == null)
                return false;

            MeshFilter meshFilter = visual.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return false;

            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            Transform meshTransform = meshFilter.transform;
            bool found = false;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            Vector3 center = meshBounds.center;
            Vector3 extents = meshBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 world = meshTransform.TransformPoint(corner);
                        Vector3 local = transform.InverseTransformPoint(world);
                        if (!found)
                        {
                            minY = local.y;
                            maxY = local.y;
                            found = true;
                        }
                        else
                        {
                            minY = Mathf.Min(minY, local.y);
                            maxY = Mathf.Max(maxY, local.y);
                        }
                    }
                }
            }

            if (!found)
                return false;

            bottomY = minY;
            topY = maxY;
            return true;
        }
    }
}
