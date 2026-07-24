using System.Collections.Generic;
using GAITemplate;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    /// <summary>
    /// Shared art material paths and PieceColorType → Mat_*-{Suffix} naming.
    /// </summary>
    public static class CarryBlockJamArtMaterialUtility
    {
        public const string TableMaterialsFolder = "Assets/[Materials]";
        public const string PlateMaterialsFolder = "Assets/[Materials]/-Plate Materials";
        public const string GateUpMaterialsFolder = "Assets/[Materials]/-GateUp Materials";
        public const string GateBottomMaterialsFolder = "Assets/[Materials]/-GateBottom Materials";
        public const string GateLeftMaterialsFolder = "Assets/[Materials]/-GateLeft Materials";
        public const string GateRightMaterialsFolder = "Assets/[Materials]/-GateRight Materials";

        public static string GetGateMaterialsFolder(BoardBorderSide side)
        {
            return side switch
            {
                BoardBorderSide.Bottom => GateBottomMaterialsFolder,
                BoardBorderSide.Left => GateLeftMaterialsFolder,
                BoardBorderSide.Right => GateRightMaterialsFolder,
                _ => GateUpMaterialsFolder,
            };
        }

        public static string GetGateMaterialPrefix(BoardBorderSide side)
        {
            return side switch
            {
                BoardBorderSide.Bottom => "Mat_GateBottom",
                BoardBorderSide.Left => "Mat_GateLeft",
                BoardBorderSide.Right => "Mat_GateRight",
                _ => "Mat_GateUp",
            };
        }

        private static readonly Dictionary<string, Material> MaterialCache = new();

        /// <summary>
        /// Material filename suffix used by Mat_Table / Mat_Plate / Mat_Gate* assets.
        /// </summary>
        public static string GetMaterialSuffix(PieceColorType color)
        {
            return color switch
            {
                PieceColorType.Red => "Red",
                PieceColorType.Blue => "Blue",
                PieceColorType.Green => "Green",
                PieceColorType.Yellow => "Yellow",
                PieceColorType.Orange => "Orange",
                PieceColorType.Purple => "Purple",
                PieceColorType.Pink => "Pink",
                PieceColorType.Cherry => "Cherry",
                PieceColorType.Lightblue => "LightBlue",
                PieceColorType.GreenDark => "DarkGreen",
                PieceColorType.Lime or PieceColorType.SeaGreen or PieceColorType.GreenOlive => "Green",
                PieceColorType.Amber => "Yellow",
                PieceColorType.Apricot => "Orange",
                PieceColorType.Lilac => "Pink",
                PieceColorType.Plum or PieceColorType.Hidden => "Purple",
                PieceColorType.Brown => "Orange",
                PieceColorType.Navy => "Blue",
                PieceColorType.White => "White",
                PieceColorType.Grey => "Grey",
                PieceColorType.Black => "Black",
                PieceColorType.Black2 => "Black2",
                _ => null,
            };
        }

        public static bool TryParseMaterialSuffix(string suffix, out PieceColorType color)
        {
            color = PieceColorType.None;
            if (string.IsNullOrEmpty(suffix))
                return false;

            if (suffix.Equals("LightBlue", System.StringComparison.OrdinalIgnoreCase))
            {
                color = PieceColorType.Lightblue;
                return true;
            }

            if (suffix.Equals("DarkGreen", System.StringComparison.OrdinalIgnoreCase))
            {
                color = PieceColorType.GreenDark;
                return true;
            }

            if (suffix.Equals("Black2", System.StringComparison.OrdinalIgnoreCase))
            {
                color = PieceColorType.Black2;
                return true;
            }

            if (suffix.Equals("Black", System.StringComparison.OrdinalIgnoreCase))
            {
                color = PieceColorType.Black;
                return true;
            }

            return System.Enum.TryParse(suffix, true, out color)
                && color != PieceColorType.None
                && color != PieceColorType.Hidden;
        }

        public static Material LoadMaterial(string materialsFolder, string materialName, string resourcesSubfolder)
        {
            if (string.IsNullOrEmpty(materialName))
                return null;

            string cacheKey = $"{materialsFolder}/{materialName}";
            if (MaterialCache.TryGetValue(cacheKey, out Material cached) && cached != null)
                return cached;

            Material material = null;

            // Prefer Resources so editor Play Mode and player builds resolve the same mats.
            if (!string.IsNullOrEmpty(resourcesSubfolder))
                material = Resources.Load<Material>($"{resourcesSubfolder}/{materialName}");

#if UNITY_EDITOR
            if (material == null && !string.IsNullOrEmpty(materialsFolder))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>($"{materialsFolder}/{materialName}.mat");

                // Color mats may live in typed subfolders (e.g. -Table Materials).
                if (material == null)
                    material = FindMaterialInFolder(materialsFolder, materialName);
            }

            // Legacy flat folder fallback while assets migrate.
            if (material == null)
                material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/[Materials]/{materialName}.mat");
            if (material == null)
                material = FindMaterialInFolder("Assets/[Materials]", materialName);
#endif

            if (material != null)
                MaterialCache[cacheKey] = material;

            return material;
        }

        public static Material LoadColoredMaterial(
            string materialsFolder,
            string baseMaterialName,
            PieceColorType color,
            string resourcesSubfolder)
        {
            if (!PieceColorPalette.IsPaintable(color))
                return PieceColorPalette.GetMaterial(color);

            string suffix = GetMaterialSuffix(color);
            if (!string.IsNullOrEmpty(suffix))
            {
                Material material = LoadMaterial(materialsFolder, $"{baseMaterialName}-{suffix}", resourcesSubfolder);
                if (material != null)
                    return material;
            }

            return PieceColorPalette.GetMaterial(color);
        }

#if UNITY_EDITOR
        private static Material FindMaterialInFolder(string materialsFolder, string materialName)
        {
            if (string.IsNullOrEmpty(materialsFolder) || string.IsNullOrEmpty(materialName))
                return null;

            string[] guids = AssetDatabase.FindAssets($"{materialName} t:Material", new[] { materialsFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;

                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.Equals(fileName, materialName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                    return material;
            }

            return null;
        }
#endif
    }
}
