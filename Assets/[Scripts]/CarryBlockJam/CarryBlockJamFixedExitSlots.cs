using System.Collections.Generic;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// The four art gates are fixed exits: two top, two bottom.
    /// Side/index are locked to gate positions; only goals (color + count) are authored.
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

        public static void ApplyFixedLayout(CarryBlockJamExitDefinition definition, int slotIndex, int columns)
        {
            if (definition == null)
                return;

            int left = GetLeftColumnIndex(columns);
            int right = GetRightColumnIndex(columns);
            int slot = Mathf.Clamp(slotIndex, 0, Count - 1);

            switch (slot)
            {
                case 0:
                    definition.side = BoardBorderSide.Top;
                    definition.startIndex = left;
                    break;
                case 1:
                    definition.side = BoardBorderSide.Top;
                    definition.startIndex = right;
                    break;
                case 2:
                    definition.side = BoardBorderSide.Bottom;
                    definition.startIndex = left;
                    break;
                default:
                    definition.side = BoardBorderSide.Bottom;
                    definition.startIndex = right;
                    break;
            }

            definition.length = 1;
            definition.positionOffset = Vector3.zero;
            definition.rotation = Vector3.zero;
            if (definition.modelScale == Vector3.zero)
                definition.modelScale = Vector3.one;

            if (definition.goals == null)
                definition.goals = new List<CarryBlockJamExitGoal>();

            if (definition.goals.Count == 0)
                definition.goals.Add(new CarryBlockJamExitGoal());
        }

        public static CarryBlockJamExitDefinition CreateDefault(int slotIndex, int columns)
        {
            var definition = new CarryBlockJamExitDefinition
            {
                modelScale = Vector3.one,
                goals = new List<CarryBlockJamExitGoal>
                {
                    new CarryBlockJamExitGoal(),
                },
            };
            ApplyFixedLayout(definition, slotIndex, columns);
            return definition;
        }

        public static void EnsureFixedExits(List<CarryBlockJamExitDefinition> exits, int columns)
        {
            if (exits == null)
                return;

            var preservedGoals = new List<List<CarryBlockJamExitGoal>>(Count);
            var preservedScales = new Vector3[Count];
            for (int i = 0; i < Count; i++)
            {
                preservedScales[i] = Vector3.one;
                if (i < exits.Count && exits[i] != null)
                {
                    if (exits[i].goals != null && exits[i].goals.Count > 0)
                        preservedGoals.Add(CloneGoals(exits[i].goals));
                    else
                        preservedGoals.Add(null);

                    if (exits[i].modelScale != Vector3.zero)
                        preservedScales[i] = exits[i].modelScale;
                }
                else
                    preservedGoals.Add(null);
            }

            // Prefer matching existing exits to slots by side + left/right half.
            MatchGoalsByLayout(exits, preservedGoals, preservedScales, columns);

            exits.Clear();
            for (int i = 0; i < Count; i++)
            {
                CarryBlockJamExitDefinition definition = CreateDefault(i, columns);
                if (preservedGoals[i] != null)
                    definition.goals = preservedGoals[i];
                definition.modelScale = preservedScales[i] == Vector3.zero ? Vector3.one : preservedScales[i];
                exits.Add(definition);
            }
        }

        private static void MatchGoalsByLayout(
            List<CarryBlockJamExitDefinition> exits,
            List<List<CarryBlockJamExitGoal>> preservedGoals,
            Vector3[] preservedScales,
            int columns)
        {
            if (exits == null)
                return;

            int left = GetLeftColumnIndex(columns);
            int right = GetRightColumnIndex(columns);
            bool[] filled = new bool[Count];

            for (int i = 0; i < Count; i++)
            {
                if (preservedGoals[i] != null)
                    filled[i] = true;
            }

            for (int i = 0; i < exits.Count; i++)
            {
                CarryBlockJamExitDefinition definition = exits[i];
                if (definition?.goals == null || definition.goals.Count == 0)
                    continue;

                int slot = ResolveSlotIndex(definition, left, right, columns);
                if (slot < 0 || filled[slot])
                    continue;

                preservedGoals[slot] = CloneGoals(definition.goals);
                if (definition.modelScale != Vector3.zero)
                    preservedScales[slot] = definition.modelScale;
                filled[slot] = true;
            }
        }

        private static int ResolveSlotIndex(
            CarryBlockJamExitDefinition definition,
            int leftColumn,
            int rightColumn,
            int columns)
        {
            if (definition == null)
                return -1;

            BoardBorderSide side = definition.side;
            if (side == BoardBorderSide.Left || side == BoardBorderSide.Right)
            {
                // Legacy side exits: Left → left column, Right → right column, top row preferred.
                int horizontal = side == BoardBorderSide.Left ? 0 : 1;
                return horizontal; // Top left / Top right
            }

            float center = definition.startIndex + (Mathf.Max(1, definition.length) - 1) * 0.5f;
            int horizontalSlot = center < columns * 0.5f ? 0 : 1;
            if (Mathf.Abs(center - leftColumn) <= Mathf.Abs(center - rightColumn))
                horizontalSlot = 0;
            else
                horizontalSlot = 1;

            return side == BoardBorderSide.Bottom ? 2 + horizontalSlot : horizontalSlot;
        }

        private static List<CarryBlockJamExitGoal> CloneGoals(List<CarryBlockJamExitGoal> source)
        {
            var clone = new List<CarryBlockJamExitGoal>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                CarryBlockJamExitGoal goal = source[i];
                if (goal == null)
                    continue;

                clone.Add(new CarryBlockJamExitGoal
                {
                    color = goal.color,
                    requiredPlateCount = Mathf.Max(1, goal.requiredPlateCount),
                });
            }

            if (clone.Count == 0)
                clone.Add(new CarryBlockJamExitGoal());

            return clone;
        }
    }
}
