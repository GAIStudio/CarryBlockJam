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
        [SerializeField] private List<CarryBlockJamExitGoal> goals = new List<CarryBlockJamExitGoal>();
        [SerializeField] private int currentGoalIndex;
        [SerializeField] private int remainingPlateCount;
        [SerializeField] private GamePiece gateVisual;
        [SerializeField] private GamePiece carVisual;
        [SerializeField] private TMP_Text goalLabel;

        public BoardBorderSide Side => side;
        public int StartIndex => startIndex;
        public int Length => length;
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
            gateVisual = gatePiece;
            carVisual = carPiece;
            goalLabel = label;

            if (goalLabel != null && labelSettings != null)
                CarryBlockJamExitLabelUtility.ApplyLabelSettings(goalLabel, labelSettings);
            else
                CarryBlockJamExitLabelUtility.ApplyRuntimeOutline(goalLabel, labelSettings);

            RefreshVisuals();
        }

        public bool CanAccept(PieceColorType color) =>
            !IsCompleted && color != PieceColorType.None && color == CurrentColor;

        public int Consume(PieceColorType color, int plateCount)
        {
            if (!CanAccept(color) || plateCount <= 0)
                return 0;

            int consumed = Mathf.Min(remainingPlateCount, plateCount);
            remainingPlateCount -= consumed;

            if (remainingPlateCount == 0)
                AdvanceGoal();
            else
                RefreshVisuals();

            return consumed;
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
            if (gateVisual != null)
                gateVisual.ApplyColor(PieceColorPalette.IsPaintable(CurrentColor) ? CurrentColor : PieceColorType.Grey);

            if (carVisual != null && PieceColorPalette.IsPaintable(CurrentColor))
                carVisual.ApplyColor(CurrentColor);

            if (goalLabel != null)
            {
                goalLabel.text = IsCompleted ? string.Empty : remainingPlateCount.ToString();
                goalLabel.ForceMeshUpdate();
            }
        }
    }
}
