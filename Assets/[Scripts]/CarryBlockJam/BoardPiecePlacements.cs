using System;
using GAITemplate;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace CarryBlockJam
{
    [Serializable]
    public class BoardCylinderPlacement
    {
        public int row = 2;
        public int column = 3;
        public Vector3 localScale = Vector3.one;
        // Mixamo root is near the hips; Y=1.75 keeps the body above M_GridCell.
        public Vector3 positionOffset = new Vector3(0f, 1.75f, 0f);
        public Vector3 rotation;

        public static BoardCylinderPlacement CreateDefault()
        {
            return new BoardCylinderPlacement
            {
                row = 2,
                column = 3,
                localScale = Vector3.one,
                positionOffset = new Vector3(0f, 1.75f, 0f),
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

        public static BoardPieceVisualSettings CreateTableDefault()
        {
            return new BoardPieceVisualSettings
            {
                scale = Vector3.one,
                offset = new Vector3(0f, 0.399f, 0f),
            };
        }

        public static BoardPieceVisualSettings CreateBoxDefault() => CreateTableDefault();

        public static BoardPieceVisualSettings CreatePlateDefault()
        {
            return new BoardPieceVisualSettings
            {
                scale = Vector3.one,
                offset = new Vector3(0f, 0.75f, 0f),
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
        public Vector3 topOffset;
        public Vector3 bottomOffset;
        public Vector3 leftOffset;
        public Vector3 rightOffset;
        public Color color = Color.white;
        public bool bold = true;
        public bool useOutline = true;

        public static BoardExitLabelSettings CreateDefault() => new BoardExitLabelSettings();

        public Vector3 GetOffsetForSide(BoardBorderSide side)
        {
            // Left/Right fall back to the legacy top/bottom mapping while unset,
            // so existing scenes keep their label placement.
            return side switch
            {
                BoardBorderSide.Bottom => bottomOffset,
                BoardBorderSide.Left => leftOffset == Vector3.zero ? topOffset : leftOffset,
                BoardBorderSide.Right => rightOffset == Vector3.zero ? bottomOffset : rightOffset,
                _ => topOffset,
            };
        }
    }

    [Serializable]
    public class BoardFrozenBoxVisualSettings
    {
        public GameObject model;
        public Material material;
        public Vector3 scale = Vector3.one;
        public Vector3 offset;
        [FormerlySerializedAs("autoFitToBox")]
        public bool autoFitToTable = true;
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
    public class BoardCurtainBoxVisualSettings
    {
        public GameObject model;
        public Material curtainMaterial;
        public Color curtainTint = Color.white;
        public Sprite colorSprite;
        public Vector3 curtainScale = Vector3.one;
        public Vector3 curtainOffset;
        [FormerlySerializedAs("autoFitToBox")]
        public bool autoFitToTable = true;
        [Min(0.1f)] public float coverPadding = 1.05f;
        [Header("Curtain Badge")]
        public Vector3 badgeOffset;
        public Vector3 badgeScale = new Vector3(0.85f, 0.85f, 0.85f);
        public Vector3 badgeRotation = new Vector3(90f, 180f, 0f);

        public static BoardCurtainBoxVisualSettings CreateDefault()
        {
            return new BoardCurtainBoxVisualSettings();
        }

        public Vector3 GetResolvedBadgeScale()
        {
            Vector3 scale = badgeScale;
            if (Mathf.Abs(scale.x) < 0.001f && Mathf.Abs(scale.y) < 0.001f && Mathf.Abs(scale.z) < 0.001f)
                return new Vector3(0.85f, 0.85f, 0.85f);

            if (Mathf.Abs(scale.x) < 0.001f) scale.x = 0.85f;
            if (Mathf.Abs(scale.y) < 0.001f) scale.y = 0.85f;
            if (Mathf.Abs(scale.z) < 0.001f) scale.z = 0.85f;
            return scale;
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
        public bool isCurtain;
        public PieceColorType curtainColor = PieceColorType.Purple;
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

        public static BoardBoxPlacement CreateCurtain(
            int row,
            int column,
            PieceColorType boxColor,
            PieceColorType curtainColor)
        {
            return new BoardBoxPlacement
            {
                row = row,
                column = column,
                color = boxColor,
                isCurtain = true,
                curtainColor = curtainColor,
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
        public bool isHidden;

        public static BoardPlatePlacement Create(
            int row,
            int column,
            PieceColorType color,
            bool isHidden = false)
        {
            return new BoardPlatePlacement
            {
                row = row,
                column = column,
                color = color,
                isHidden = isHidden,
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
