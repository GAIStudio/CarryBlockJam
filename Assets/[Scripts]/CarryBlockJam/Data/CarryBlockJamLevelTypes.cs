using System;
using System.Collections.Generic;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    public enum CarryBlockJamStickmanSpawnMode
    {
        Center = 0,
        RandomEmptyCell = 1,
        FixedCell = 2,
    }

    [Serializable]
    public struct CarryBlockJamGridCoordinate
    {
        public int row;
        public int column;

        public CarryBlockJamGridCoordinate(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

        public Vector2Int ToVector2Int() => new Vector2Int(row, column);
    }

    [Serializable]
    public class CarryBlockJamColorSetup
    {
        public PieceColorType color = PieceColorType.Red;

        [Min(0)] public int minPlateCount = 1;
        [Min(0)] public int maxPlateCount = 3;
        [Min(1)] public int minPlateStacks = 1;
        [Min(1)] public int maxPlateStacks = 2;

        public int GetValidatedMaxPlateCount() => Mathf.Max(minPlateCount, maxPlateCount);
        public int GetValidatedMaxPlateStacks() => Mathf.Max(minPlateStacks, maxPlateStacks);
    }

    [Serializable]
    public class CarryBlockJamBoxPlacement
    {
        public PieceColorType color = PieceColorType.Red;
        public int row;
        public int column;
        public bool isHidden;
        public bool isFrozen;
        public bool isCurtain;
        public PieceColorType curtainColor = PieceColorType.Purple;
        [Min(1)] public int unlockMoves = 3;
    }

    [Serializable]
    public class CarryBlockJamPlatePlacement
    {
        public PieceColorType color = PieceColorType.Red;
        public int row;
        public int column;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class CarryBlockJamExitGoal
    {
        public PieceColorType color = PieceColorType.Red;
        [Min(1)] public int requiredPlateCount = 3;
    }

    [Serializable]
    public class CarryBlockJamExitDefinition
    {
        [HideInInspector] public BoardBorderSide side = BoardBorderSide.Top;
        [HideInInspector] [Min(0)] public int startIndex = 0;
        [HideInInspector] [Min(1)] public int length = 1;
        [HideInInspector] public Vector3 positionOffset;
        [HideInInspector] public Vector3 rotation;
        public List<CarryBlockJamExitGoal> goals = new List<CarryBlockJamExitGoal>
        {
            new CarryBlockJamExitGoal(),
        };
    }

    [Serializable]
    public class CarryBlockJamPrimitiveVisualSettings
    {
        public GameObject prefab;
        public PrimitiveType primitiveType = PrimitiveType.Cube;
        public Vector3 localScale = Vector3.one;
        public Vector3 localPosition;
        public Vector3 localRotation;
        public bool tintWithPieceColor = true;
    }
}
