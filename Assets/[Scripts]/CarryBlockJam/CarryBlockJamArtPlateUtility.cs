using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Applies art plate materials (Mat_Plate-{Color}) onto M_Plate visuals.
    /// </summary>
    public static class CarryBlockJamArtPlateUtility
    {
        private const string BaseMaterialName = "Mat_Plate";
        private const string ResourcesFolder = "Materials/Plates";

        public static void ApplyPlateColor(Transform plateRoot, PieceColorType color)
        {
            if (plateRoot == null)
                return;

            Renderer renderer = plateRoot.GetComponentInChildren<Renderer>(true);
            if (renderer == null)
                return;

            Material colorMaterial = CarryBlockJamArtMaterialUtility.LoadColoredMaterial(
                CarryBlockJamArtMaterialUtility.PlateMaterialsFolder,
                BaseMaterialName,
                color,
                ResourcesFolder);
            if (colorMaterial == null)
                return;

            renderer.sharedMaterial = colorMaterial;
        }
    }
}
