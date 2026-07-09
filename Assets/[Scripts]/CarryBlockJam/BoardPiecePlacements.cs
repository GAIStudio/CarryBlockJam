using System;
using GAITemplate;
using TMPro;
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
    public class BoardPieceVisualSettings
    {
        public GameObject model;
        public Material material;
        public Vector3 scale = Vector3.one;
        public Vector3 offset;

        public static BoardPieceVisualSettings CreateBoxDefault()
        {
            return new BoardPieceVisualSettings
            {
                scale = Vector3.one * 1.1f,
                offset = new Vector3(0f, 0.75f, 0f),
            };
        }

        public static BoardPieceVisualSettings CreatePlateDefault()
        {
            return new BoardPieceVisualSettings
            {
                scale = new Vector3(0.9f, 0.16f, 0.9f),
                offset = new Vector3(0f, 0.18f, 0f),
            };
        }
    }

    [Serializable]
    public class BoardExitLabelSettings
    {
        public TMP_FontAsset font;
        public Material material;
        public float fontSize = 48f;
        public Vector3 scale = Vector3.one * 0.45f;
        public Vector3 offset;
        public Color color = Color.white;
        public bool bold = true;
        public bool useOutline = true;

        public static BoardExitLabelSettings CreateDefault() => new BoardExitLabelSettings();
    }

    [Serializable]
    public class BoardFrozenBoxVisualSettings
    {
        public GameObject model;
        public Material material;
        public Vector3 scale = Vector3.one;
        public Vector3 offset;
        public bool autoFitToBox = true;
        [Min(0.1f)] public float coverPadding = 1.1f;
        public TMP_FontAsset font;
        public float fontSize = 48f;
        public Vector3 textOffset;
        public Vector3 textScale = Vector3.one * 0.45f;
        public Color textColor = Color.white;
        public bool bold = true;
        public bool useOutline = true;

        public static BoardFrozenBoxVisualSettings CreateDefault()
        {
            return new BoardFrozenBoxVisualSettings();
        }
    }

    [Serializable]
    public class BoardBoxPlacement
    {
        public int row;
        public int column;
        public PieceColorType color = PieceColorType.Red;
        public bool isHidden;
        public bool isFrozen;
        [Min(1)] public int unlockMoves = 3;

        public static BoardBoxPlacement Create(int row, int column, PieceColorType color, bool isHidden = false)
        {
            return new BoardBoxPlacement
            {
                row = row,
                column = column,
                color = color,
                isHidden = isHidden,
            };
        }

        public static BoardBoxPlacement CreateFrozen(
            int row,
            int column,
            PieceColorType color,
            int unlockMoves)
        {
            return new BoardBoxPlacement
            {
                row = row,
                column = column,
                color = color,
                isFrozen = true,
                unlockMoves = Mathf.Max(1, unlockMoves),
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

        public static BoardPlatePlacement Create(int row, int column, PieceColorType color)
        {
            return new BoardPlatePlacement
            {
                row = row,
                column = column,
                color = color,
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
