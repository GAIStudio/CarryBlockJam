using System.Collections.Generic;
using GAITemplate;
using TMPro;
using UnityEngine;

namespace CarryBlockJam
{
    public class CarryBlockJamExit : MonoBehaviour
    {
        [SerializeField] private BoardBorderSide side;
        [SerializeField] private int startIndex;
        [SerializeField] private int length = 1;
        [SerializeField] private int row = -1;
        [SerializeField] private int column = -1;
        [SerializeField] private List<CarryBlockJamExitGoal> goals = new List<CarryBlockJamExitGoal>();
        [SerializeField] private int currentGoalIndex;
        [SerializeField] private int remainingPlateCount;
        [SerializeField] private GamePiece gateVisual;
        [SerializeField] private GamePiece carVisual;
        [SerializeField] private TMP_Text goalLabel;
        [SerializeField] private bool useArtGateMaterials;
        [SerializeField] private bool artGateIsUp = true;

        public BoardBorderSide Side => side;
        public int StartIndex => startIndex;
        public int Length => length;
        public int Row => row;
        public int Column => column;
        public PieceColorType CurrentColor =>
            currentGoalIndex >= 0 && currentGoalIndex < goals.Count
                ? goals[currentGoalIndex].color
                : PieceColorType.None;
        public int RemainingPlateCount => remainingPlateCount;
        public bool IsCompleted => currentGoalIndex >= goals.Count;

        public void Configure(CarryBlockJamExitDefinition definition)
        {
            if (definition == null)
                return;

            side = definition.side;
            startIndex = definition.startIndex;
            length = Mathf.Max(1, definition.length);
            row = definition.row;
            column = definition.column;
            goals = new List<CarryBlockJamExitGoal>(definition.goals ?? new List<CarryBlockJamExitGoal>());
            currentGoalIndex = 0;
            remainingPlateCount = goals.Count > 0 ? Mathf.Max(0, goals[0].requiredPlateCount) : 0;
            RefreshVisuals();
        }

        public void BindVisuals(
            GamePiece gatePiece,
            GamePiece carPiece,
            TMP_Text label,
            BoardExitLabelSettings labelSettings = null)
        {
            useArtGateMaterials = false;
            gateVisual = gatePiece;
            carVisual = carPiece;
            goalLabel = label;

            if (goalLabel != null && labelSettings != null)
                CarryBlockJamExitLabelUtility.ApplyLabelSettings(goalLabel, labelSettings);
            else
                CarryBlockJamExitLabelUtility.ApplyRuntimeOutline(goalLabel, labelSettings);

            if (goalLabel != null && useArtGateMaterials)
                CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(goalLabel.transform, side, labelSettings);

            RefreshVisuals();
        }

        public void BindArtGate(
            bool isUpGate,
            TMP_Text label,
            BoardExitLabelSettings labelSettings = null)
        {
            useArtGateMaterials = true;
            artGateIsUp = isUpGate;
            gateVisual = null;
            carVisual = null;
            goalLabel = label;

            if (goalLabel != null && labelSettings != null)
                CarryBlockJamExitLabelUtility.ApplyLabelSettings(goalLabel, labelSettings);
            else
                CarryBlockJamExitLabelUtility.ApplyRuntimeOutline(goalLabel, labelSettings);

            if (goalLabel != null)
                CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(goalLabel.transform, side, labelSettings);

            RefreshVisuals();
        }

        public bool CanAccept(PieceColorType color) =>
            !IsCompleted && color != PieceColorType.None && color == CurrentColor;

        public int GetAcceptablePlateCount(PieceColorType color)
        {
            if (!CanAccept(color))
                return 0;

            return Mathf.Max(0, remainingPlateCount);
        }

        /// <summary>
        /// Consumes up to <paramref name="plateCount"/> plates at once (updates label once).
        /// Prefer <see cref="ConsumeOne"/> when animating deliveries one-by-one.
        /// </summary>
        public int Consume(PieceColorType color, int plateCount)
        {
            if (!CanAccept(color) || plateCount <= 0)
                return 0;

            int consumed = 0;
            while (consumed < plateCount && CanAccept(color))
            {
                if (ConsumeOne(color) <= 0)
                    break;
                consumed++;
            }

            return consumed;
        }

        /// <summary>
        /// Consumes a single matching plate and refreshes the goal label (x3 → x2 → x1 → hide).
        /// </summary>
        public int ConsumeOne(PieceColorType color)
        {
            if (!CanAccept(color) || remainingPlateCount <= 0)
                return 0;

            remainingPlateCount--;
            if (remainingPlateCount == 0)
                AdvanceGoal();
            else
                RefreshVisuals();

            return 1;
        }

        private void AdvanceGoal()
        {
            currentGoalIndex++;
            if (currentGoalIndex >= goals.Count)
            {
                remainingPlateCount = 0;
                RefreshVisuals();
                return;
            }

            remainingPlateCount = Mathf.Max(0, goals[currentGoalIndex].requiredPlateCount);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (useArtGateMaterials)
            {
                CarryBlockJamArtGateUtility.ApplyGateColor(
                    transform,
                    artGateIsUp,
                    PieceColorPalette.IsPaintable(CurrentColor) ? CurrentColor : PieceColorType.Grey);
            }
            else if (gateVisual != null)
            {
                gateVisual.ApplyColor(
                    PieceColorPalette.IsPaintable(CurrentColor) ? CurrentColor : PieceColorType.Grey);
            }

            if (carVisual != null && PieceColorPalette.IsPaintable(CurrentColor))
                carVisual.ApplyColor(CurrentColor);

            if (goalLabel != null)
            {
                goalLabel.text = IsCompleted ? string.Empty : $"x{remainingPlateCount}";
                goalLabel.gameObject.SetActive(!IsCompleted);

                PieceColorType labelColor = PieceColorPalette.IsPaintable(CurrentColor)
                    ? CurrentColor
                    : PieceColorType.White;
                // Label color always comes from -GateUp Materials (Mat_GateUp-{Color}).
                goalLabel.color = CarryBlockJamArtGateUtility.GetGateUpLabelTintColor(labelColor);

                if (goalLabel.font != null && goalLabel.font.material != null)
                    goalLabel.ForceMeshUpdate(true);
            }
        }

        public void ApplyLabelPresentation(BoardExitLabelSettings labelSettings)
        {
            if (goalLabel == null)
                return;

            if (useArtGateMaterials)
            {
                CarryBlockJamExitLabelUtility.ApplyArtGateLabelPlacement(
                    goalLabel.transform,
                    side,
                    labelSettings);
            }
            else if (labelSettings != null)
            {
                CarryBlockJamExitLabelUtility.ApplyLabelTransform(
                    goalLabel.transform,
                    side,
                    labelSettings);
            }
            else
            {
                CarryBlockJamExitLabelUtility.ApplyLabelSettings(goalLabel, labelSettings);
            }

            RefreshVisuals();
        }
    }
}
