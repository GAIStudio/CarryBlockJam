using System.Collections.Generic;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Hidden plate: shows HiddenTexture until surrounding <b>normal</b> plates are
    /// collected, then reveals its true color and becomes pickable.
    /// Locked neighboring hidden plates do not count toward unlock until they
    /// themselves are revealed.
    /// Level creator still paints <see cref="LevelCellFlag.Hidden"/>; gameplay
    /// interprets that as a plate instead of a table.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarryBlockJamHiddenPlate : MonoBehaviour
    {
        private static readonly Vector2Int[] SurroundingOffsets =
        {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1),
        };

        private CarryBlockJamBoardPiece _platePiece;
        private Transform _visualRoot;
        private GamePiece _visualPiece;
        private readonly HashSet<CarryBlockJamBoardPiece> _surroundingPlates =
            new HashSet<CarryBlockJamBoardPiece>();
        private bool _isRevealed;

        public bool IsRevealed => _isRevealed;

        public void Bind(
            CarryBlockJamBoardPiece platePiece,
            Transform visualRoot,
            GamePiece visualPiece)
        {
            _platePiece = platePiece;
            _visualRoot = visualRoot;
            _visualPiece = visualPiece;
        }

        public void RegisterSurroundingPlates(PuzzleGrid grid)
        {
            _surroundingPlates.Clear();
            if (_platePiece == null || grid == null)
                return;

            RegisterAdjacentPlates(grid);

            // Nothing to wait for — reveal immediately.
            if (_surroundingPlates.Count == 0)
                Reveal();
        }

        public static void NotifyPlateCollected(CarryBlockJamBoardPiece plate)
        {
            if (plate == null)
                return;

            CarryBlockJamHiddenPlate[] hiddenPlates =
                FindObjectsOfType<CarryBlockJamHiddenPlate>();
            for (int i = 0; i < hiddenPlates.Length; i++)
                hiddenPlates[i].HandlePlateCollected(plate);
        }

        private void HandlePlateCollected(CarryBlockJamBoardPiece plate)
        {
            if (_isRevealed || plate == null || plate == _platePiece)
                return;

            if (!_surroundingPlates.Remove(plate))
                return;

            if (_surroundingPlates.Count == 0)
                Reveal();
        }

        private void Reveal()
        {
            if (_isRevealed || _platePiece == null)
                return;

            _isRevealed = true;
            _platePiece.RevealHiddenColor();

            if (_visualRoot != null)
                CarryBlockJamArtPlateUtility.ApplyPlateColor(
                    _visualRoot,
                    _platePiece.TrueColor);
            else if (_visualPiece != null)
                _visualPiece.ApplyColor(_platePiece.TrueColor);

            if (_visualPiece != null)
                _visualPiece.ApplyHidden(false);

            // Once unlocked this plate counts as a normal neighbor for any
            // still-locked hidden plates beside it.
            RegisterSelfWithAdjacentLockedHiddenPlates();

            enabled = false;
        }

        private void RegisterSelfWithAdjacentLockedHiddenPlates()
        {
            if (_platePiece == null)
                return;

            CarryBlockJamHiddenPlate[] hiddenPlates =
                FindObjectsOfType<CarryBlockJamHiddenPlate>();
            for (int i = 0; i < hiddenPlates.Length; i++)
            {
                CarryBlockJamHiddenPlate other = hiddenPlates[i];
                if (other == null ||
                    other == this ||
                    other._isRevealed ||
                    other._platePiece == null)
                    continue;

                if (!IsSurroundingNeighbor(_platePiece, other._platePiece))
                    continue;

                other._surroundingPlates.Add(_platePiece);
            }
        }

        private static bool IsSurroundingNeighbor(
            CarryBlockJamBoardPiece a,
            CarryBlockJamBoardPiece b)
        {
            if (a == null || b == null)
                return false;

            int rowDelta = Mathf.Abs(a.Row - b.Row);
            int columnDelta = Mathf.Abs(a.Column - b.Column);
            return rowDelta <= 1 &&
                   columnDelta <= 1 &&
                   (rowDelta != 0 || columnDelta != 0);
        }

        private void RegisterAdjacentPlates(PuzzleGrid grid)
        {
            for (int directionIndex = 0; directionIndex < SurroundingOffsets.Length; directionIndex++)
            {
                Vector2Int offset = SurroundingOffsets[directionIndex];
                int row = _platePiece.Row + offset.x;
                int column = _platePiece.Column + offset.y;
                if (!grid.TryGetCell(row, column, out PuzzleCell cell) || cell?.Occupant == null)
                    continue;

                CarryBlockJamBoardPiece piece =
                    cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (piece == null)
                    continue;

                CollectPlatesFromStack(piece);
            }
        }

        private void CollectPlatesFromStack(CarryBlockJamBoardPiece piece)
        {
            CarryBlockJamBoardPiece current = GetPickupBasePiece(piece);
            while (current != null)
            {
                // Locked hidden plates do not count until they are revealed.
                if (current.Kind == CarryBlockJamPieceKind.Plate &&
                    current != _platePiece &&
                    !current.IsColorHidden)
                    _surroundingPlates.Add(current);

                current = current.StackedAbove;
            }
        }

        private static CarryBlockJamBoardPiece GetPickupBasePiece(CarryBlockJamBoardPiece piece)
        {
            if (piece == null)
                return null;

            CarryBlockJamBoardPiece current = piece;
            while (current.StackedBelow != null &&
                   current.StackedBelow.Kind == CarryBlockJamPieceKind.Plate)
                current = current.StackedBelow;

            return current;
        }
    }
}
