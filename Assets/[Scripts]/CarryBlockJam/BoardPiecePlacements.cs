using System;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    [Serializable]
    public class BoardCylinderPlacement
    {
        public int row = 2;
        public int column = 3;
        public Vector3 localScale = new Vector3(0.75f, 1.2f, 0.75f);
        public Vector3 positionOffset = new Vector3(0f, 0.15f, 0f);
        public Vector3 rotation;

        public static BoardCylinderPlacement CreateDefault()
        {
            return new BoardCylinderPlacement
            {
                row = 2,
                column = 3,
                localScale = new Vector3(0.75f, 1.2f, 0.75f),
                positionOffset = new Vector3(0f, 0.25f, 0f),
                rotation = Vector3.zero,
            };
        }
    }

    [Serializable]
    public class BoardBoxPlacement
    {
        public int row;
        public int column;
        public PieceColorType color = PieceColorType.Red;
        public Vector3 localScale = Vector3.one * 1.1f;
        public Vector3 positionOffset = new Vector3(0f, 0.75f, 0f);

        public static BoardBoxPlacement Create(int row, int column, PieceColorType color)
        {
            return new BoardBoxPlacement
            {
                row = row,
                column = column,
                color = color,
                localScale = Vector3.one * 1.1f,
                positionOffset = new Vector3(0f, 0.75f, 0f),
            };
        }

        public static BoardBoxPlacement[] CreateDefaults()
        {
            return new[]
            {
                Create(1, 1, PieceColorType.Red),
                Create(1, 4, PieceColorType.Purple),
            };
        }
    }

    [Serializable]
    public class BoardPlatePlacement
    {
        public int row;
        public int column;
        public PieceColorType color = PieceColorType.Red;
        public Vector3 localScale = new Vector3(0.9f, 0.16f, 0.9f);
        public Vector3 positionOffset = new Vector3(0f, 0.18f, 0f);

        public static BoardPlatePlacement Create(int row, int column, PieceColorType color)
        {
            return new BoardPlatePlacement
            {
                row = row,
                column = column,
                color = color,
                localScale = new Vector3(0.9f, 0.16f, 0.9f),
                positionOffset = new Vector3(0f, 0.18f, 0f),
            };
        }

        public static BoardPlatePlacement[] CreateDefaults()
        {
            return new[]
            {
                Create(2, 1, PieceColorType.Red),
                Create(4, 2, PieceColorType.Red),
                Create(2, 4, PieceColorType.Purple),
                Create(4, 4, PieceColorType.Purple),
            };
        }
    }
}
