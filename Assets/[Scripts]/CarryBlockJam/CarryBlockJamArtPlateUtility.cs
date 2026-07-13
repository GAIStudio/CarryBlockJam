using System.Collections.Generic;
using GAITemplate;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    /// <summary>
    /// Applies art plate materials (Mat_Plate-{Color}) onto M_Plate visuals.
    /// </summary>
    public static class CarryBlockJamArtPlateUtility
    {
        private const string MaterialsFolder = "Assets/[Materials]";
        private const string ResourcesFolder = "Materials/Plates";
        private const string BaseMaterialName = "Mat_Plate";

        private static readonly Dictionary<string, Material> MaterialCache = new();

        public static void ApplyPlateColor(Transform plateRoot, PieceColorType color)
        {
            if (plateRoot == null)
                return;

            Renderer renderer = plateRoot.GetComponentInChildren<Renderer>(true);
            if (renderer == null)
                return;

            Material colorMaterial = LoadColorMaterial(color);
            if (colorMaterial == null)
                return;

            renderer.sharedMaterial = colorMaterial;
        }

        private static Material LoadColorMaterial(PieceColorType color)
        {
            if (!PieceColorPalette.IsPaintable(color) || color == PieceColorType.Grey)
                return PieceColorPalette.GetMaterial(color);

            Material material = LoadMaterial($"{BaseMaterialName}-{color}");
            if (material != null)
                return material;

            string fallbackSuffix = color switch
            {
                PieceColorType.Yellow or PieceColorType.Amber
                    or PieceColorType.Apricot or PieceColorType.Cherry => "Orange",
                PieceColorType.Lime or PieceColorType.GreenDark or PieceColorType.GreenOlive
                    or PieceColorType.SeaGreen => "Green",
                PieceColorType.Lightblue or PieceColorType.Navy or PieceColorType.White => "Blue",
                PieceColorType.Lilac or PieceColorType.Plum
                    or PieceColorType.Brown or PieceColorType.Hidden => "Purple",
                PieceColorType.Pink => "Pink",
                PieceColorType.Red => "Red",
                PieceColorType.Orange => "Orange",
                PieceColorType.Blue => "Blue",
                PieceColorType.Green => "Green",
                PieceColorType.Purple => "Purple",
                _ => null,
            };

            if (!string.IsNullOrEmpty(fallbackSuffix))
            {
                material = LoadMaterial($"{BaseMaterialName}-{fallbackSuffix}");
                if (material != null)
                    return material;
            }

            return PieceColorPalette.GetMaterial(color);
        }

        private static Material LoadMaterial(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return null;

            if (MaterialCache.TryGetValue(materialName, out Material cached) && cached != null)
                return cached;

            Material material = null;
#if UNITY_EDITOR
            material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{materialName}.mat");
#endif
            if (material == null)
                material = Resources.Load<Material>($"{ResourcesFolder}/{materialName}");

            if (material != null)
                MaterialCache[materialName] = material;

            return material;
        }
    }
}
