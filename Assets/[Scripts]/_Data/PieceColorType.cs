using System.Collections.Generic;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Matches Beads_Out BallTypes ordinals for shared color definitions.
    /// </summary>
    public enum PieceColorType
    {
        None = 0,
        Red,
        Blue,
        Green,
        Yellow,
        Orange,
        Purple,
        Grey,
        Lime,
        Lightblue,
        Pink,
        Brown,
        SeaGreen,
        Lilac,
        Amber,
        Apricot,
        Cherry,
        GreenDark,
        GreenOlive,
        Navy,
        Plum,
        White,
        Hidden,
    }

    public static class PieceColorPalette
    {
        private const string MaterialsResourceFolder = "Materials/GamePieceColors";
        private static readonly Dictionary<PieceColorType, Material> MaterialCache = new();

        public static Material GetMaterial(PieceColorType type)
        {
            if (!IsPaintable(type))
                return null;

            if (MaterialCache.TryGetValue(type, out Material cached) && cached != null)
                return cached;

            Material material = Resources.Load<Material>($"{MaterialsResourceFolder}/{type}");
            if (material == null)
            {
                Debug.LogWarning(
                    $"[PieceColorPalette] Material not found at Resources/{MaterialsResourceFolder}/{type}");
                return null;
            }

            MaterialCache[type] = material;
            return material;
        }

        public static Color GetColor(PieceColorType type)
        {
            switch (type)
            {
                case PieceColorType.None:
                    return Color.black;
                case PieceColorType.Red:
                    return new Color(0.906f, 0.306f, 0.294f);
                case PieceColorType.Blue:
                    return new Color(0.306f, 0.569f, 0.965f);
                case PieceColorType.Green:
                    return new Color(0.467f, 0.925f, 0.349f);
                case PieceColorType.Yellow:
                    return new Color(0.980f, 0.898f, 0.353f);
                case PieceColorType.Orange:
                    return new Color(0.933f, 0.541f, 0.271f);
                case PieceColorType.Purple:
                    return new Color(0.698f, 0.306f, 0.961f);
                case PieceColorType.Grey:
                    return new Color(0.670f, 0.678f, 0.682f);
                case PieceColorType.Lime:
                    return new Color(0.882f, 0.917f, 0.471f);
                case PieceColorType.Lightblue:
                    return new Color(0.396f, 0.796f, 0.977f);
                case PieceColorType.Pink:
                    return new Color(0.921f, 0.404f, 0.969f);
                case PieceColorType.Brown:
                    return new Color(0.490f, 0.243f, 0.200f);
                case PieceColorType.SeaGreen:
                    return new Color(0.424f, 0.737f, 0.510f);
                case PieceColorType.Lilac:
                    return new Color(0.957f, 0.757f, 0.980f);
                case PieceColorType.Amber:
                    return new Color(0.867f, 0.792f, 0.549f);
                case PieceColorType.Apricot:
                    return new Color(0.969f, 0.824f, 0.581f);
                case PieceColorType.Cherry:
                    return new Color(0.667f, 0.235f, 0.251f);
                case PieceColorType.GreenDark:
                    return new Color(0.247f, 0.431f, 0.243f);
                case PieceColorType.GreenOlive:
                    return new Color(0.651f, 0.651f, 0.267f);
                case PieceColorType.Navy:
                    return new Color(0.177f, 0.177f, 0.627f);
                case PieceColorType.Plum:
                    return new Color(0.510f, 0.204f, 0.573f);
                case PieceColorType.White:
                    return new Color(0.941f, 0.917f, 0.820f);
                default:
                    return Color.black;
            }
        }

        public static bool IsPaintable(PieceColorType type) =>
            type != PieceColorType.None && type != PieceColorType.Hidden;
    }
}
