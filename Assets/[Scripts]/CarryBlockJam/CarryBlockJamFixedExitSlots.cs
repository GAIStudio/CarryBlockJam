using System.Collections.Generic;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Helpers for optional N exits authored by row/column (one cell each).
    /// Legacy side/startIndex data migrates via <see cref="CarryBlockJamExitDefinition.NormalizeLayout"/>.
    /// </summary>
    public static class CarryBlockJamExitLayout
    {
        public static CarryBlockJamExitDefinition CreateDefault(int row, int column)
        {
            return new CarryBlockJamExitDefinition
            {
                row = Mathf.Max(0, row),
                column = Mathf.Max(0, column),
                modelScale = Vector3.one,
                goals = new List<CarryBlockJamExitGoal>
                {
                    new CarryBlockJamExitGoal(),
                },
            };
        }

        public static void NormalizeExits(List<CarryBlockJamExitDefinition> exits, int rows, int columns)
        {
            if (exits == null)
                return;

            for (int i = exits.Count - 1; i >= 0; i--)
            {
                if (exits[i] == null)
                {
                    exits.RemoveAt(i);
                    continue;
                }

                exits[i].NormalizeLayout(rows, columns);
            }
        }
    }

    /// <summary>
    /// Deprecated fixed-slot helpers kept for older call sites / labels.
    /// Prefer <see cref="CarryBlockJamExitLayout"/>.
    /// </summary>
    public static class CarryBlockJamFixedExitSlots
    {
        public const int Count = 4;

        public static readonly string[] Labels =
        {
            "Top Left Gate",
            "Top Right Gate",
            "Bottom Left Gate",
            "Bottom Right Gate",
        };

        public static int GetLeftColumnIndex(int columns)
        {
            int safeColumns = Mathf.Max(1, columns);
            return Mathf.Clamp((safeColumns / 2) - 2, 0, safeColumns - 1);
        }

        public static int GetRightColumnIndex(int columns)
        {
            int safeColumns = Mathf.Max(1, columns);
            return Mathf.Clamp((safeColumns / 2) + 1, 0, safeColumns - 1);
        }

        public static CarryBlockJamExitDefinition CreateDefault(int slotIndex, int columns)
        {
            int left = GetLeftColumnIndex(columns);
            int right = GetRightColumnIndex(columns);
            int slot = Mathf.Clamp(slotIndex, 0, Count - 1);
            int row = slot <= 1 ? 0 : 7;
            int column = (slot % 2 == 0) ? left : right;
            CarryBlockJamExitDefinition definition = CarryBlockJamExitLayout.CreateDefault(row, column);
            definition.NormalizeLayout(8, columns);
            return definition;
        }

        public static void EnsureFixedExits(List<CarryBlockJamExitDefinition> exits, int columns)
        {
            CarryBlockJamExitLayout.NormalizeExits(exits, rows: 8, columns);
        }
    }
}
