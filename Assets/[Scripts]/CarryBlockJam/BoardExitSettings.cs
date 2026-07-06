using System;
using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    [Serializable]
    public class BoardExitSettings
    {
        public BoardBorderSide side = BoardBorderSide.Left;
        public int startIndex = 2;
        public int length = 2;
        public PieceColorType color = PieceColorType.Red;
        public Vector3 positionOffset;
        public Vector3 rotation = new Vector3(0f, 90f, 0f);

        public static BoardExitSettings CreateLeftDefault()
        {
            return new BoardExitSettings
            {
                side = BoardBorderSide.Left,
                startIndex = 2,
                length = 2,
                color = PieceColorType.Red,
                positionOffset = Vector3.zero,
                rotation = new Vector3(0f, 90f, 0f),
            };
        }

        public static BoardExitSettings CreateRightDefault()
        {
            return new BoardExitSettings
            {
                side = BoardBorderSide.Right,
                startIndex = 2,
                length = 2,
                color = PieceColorType.Purple,
                positionOffset = Vector3.zero,
                rotation = new Vector3(0f, 270f, 0f),
            };
        }
    }

    public enum BoardBorderSide
    {
        Top = 0,
        Bottom = 1,
        Left = 2,
        Right = 3,
    }
}
