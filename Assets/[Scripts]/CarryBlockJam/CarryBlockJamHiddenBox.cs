using System.Collections.Generic;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    [DisallowMultipleComponent]
    public sealed class CarryBlockJamHiddenBox : MonoBehaviour
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),
        };

        private CarryBlockJamBoardPiece _boxPiece;
        private Transform _visualRoot;
        private GamePiece _visualPiece;
        private readonly HashSet<CarryBlockJamBoardPiece> _surroundingPlates = new HashSet<CarryBlockJamBoardPiece>();
        private bool _isRevealed;

        public bool IsRevealed => _isRevealed;

        public void Bind(CarryBlockJamBoardPiece boxPiece, Transform visualRoot, GamePiece visualPiece)
        {
            _boxPiece = boxPiece;
            _visualRoot = visualRoot;
            _visualPiece = visualPiece;
        }

        public void Bind(CarryBlockJamBoardPiece boxPiece, GamePiece visualPiece)
        {
            Bind(boxPiece, visualPiece != null ? visualPiece.transform : null, visualPiece);
        }

        public void RegisterSurroundingPlates(PuzzleGrid grid)
        {
            _surroundingPlates.Clear();
            if (_boxPiece == null || grid == null)
                return;

            RegisterPlatesOnBoxStack();
            RegisterAdjacentPlates(grid);
        }

        public static void NotifyPlateCollected(CarryBlockJamBoardPiece plate)
        {
            if (plate == null)
                return;

            CarryBlockJamHiddenBox[] hiddenBoxes = FindObjectsOfType<CarryBlockJamHiddenBox>();
            for (int i = 0; i < hiddenBoxes.Length; i++)
                hiddenBoxes[i].HandlePlateCollected(plate);
        }

        private void HandlePlateCollected(CarryBlockJamBoardPiece plate)
        {
            if (_isRevealed || plate == null)
                return;

            if (!_surroundingPlates.Remove(plate))
                return;

            if (_surroundingPlates.Count == 0)
                Reveal();
        }

        private void Reveal()
        {
            if (_isRevealed || _boxPiece == null)
                return;

            _isRevealed = true;
            _boxPiece.RevealHiddenColor();

            if (_visualRoot != null)
                CarryBlockJamArtTableUtility.ApplyTableColor(_visualRoot, _boxPiece.TrueColor);
            else if (_visualPiece != null)
                _visualPiece.ApplyColor(_boxPiece.TrueColor);

            if (_visualPiece != null)
                _visualPiece.ApplyHidden(false);

            enabled = false;
        }

        private void RegisterPlatesOnBoxStack()
        {
            CarryBlockJamBoardPiece current = _boxPiece.StackedAbove;
            while (current != null)
            {
                if (current.Kind == CarryBlockJamPieceKind.Plate)
                    _surroundingPlates.Add(current);

                current = current.StackedAbove;
            }
        }

        private void RegisterAdjacentPlates(PuzzleGrid grid)
        {
            for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
            {
                Vector2Int offset = CardinalDirections[directionIndex];
                int row = _boxPiece.Row + offset.x;
                int column = _boxPiece.Column + offset.y;
                if (!grid.TryGetCell(row, column, out PuzzleCell cell) || cell?.Occupant == null)
                    continue;

                CarryBlockJamBoardPiece piece = cell.Occupant.GetComponent<CarryBlockJamBoardPiece>();
                if (piece == null)
                    continue;

                CollectPlatesFromStack(piece);
            }
        }

        private void CollectPlatesFromStack(CarryBlockJamBoardPiece piece)
        {
            CarryBlockJamBoardPiece basePiece = GetPickupBasePiece(piece);
            CarryBlockJamBoardPiece current = basePiece;
            while (current != null)
            {
                if (current.Kind == CarryBlockJamPieceKind.Plate)
                    _surroundingPlates.Add(current);

                current = current.StackedAbove;
            }
        }

        private static CarryBlockJamBoardPiece GetPickupBasePiece(CarryBlockJamBoardPiece piece)
        {
            if (piece == null)
                return null;

            CarryBlockJamBoardPiece current = piece;
            while (current.StackedBelow != null && current.StackedBelow.Kind == CarryBlockJamPieceKind.Plate)
                current = current.StackedBelow;

            return current;
        }
    }
}
