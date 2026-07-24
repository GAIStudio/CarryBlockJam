using System.Collections.Generic;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Hidden plate: shows HiddenTexture until surrounding (non-self) plates are collected,
    /// then reveals its true color and becomes pickable.
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

            enabled = false;
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
                if (current.Kind == CarryBlockJamPieceKind.Plate &&
                    current != _platePiece)
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
