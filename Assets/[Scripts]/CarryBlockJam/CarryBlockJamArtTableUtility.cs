using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    /// <summary>
    /// Applies art table materials (base + color slot) onto M_Table visuals.
    /// </summary>
    public static class CarryBlockJamArtTableUtility
    {
        private const string BaseMaterialName = "Mat_Table";
        private const string ResourcesFolder = "Materials/Tables";

        public static void ApplyTableColor(Transform tableRoot, PieceColorType color)
        {
            if (tableRoot == null)
                return;

            Renderer renderer = tableRoot.GetComponentInChildren<Renderer>(true);
            if (renderer == null)
                return;

            Material baseMaterial = CarryBlockJamArtMaterialUtility.LoadMaterial(
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                BaseMaterialName,
                ResourcesFolder);
            Material colorMaterial = CarryBlockJamArtMaterialUtility.LoadColoredMaterial(
                CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                BaseMaterialName,
                color,
                ResourcesFolder);
            if (colorMaterial == null)
                colorMaterial = baseMaterial;
            if (baseMaterial == null)
                baseMaterial = colorMaterial;

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = colorMaterial != null ? colorMaterial : baseMaterial;
                return;
            }

            if (materials.Length == 1)
            {
                materials[0] = colorMaterial != null ? colorMaterial : baseMaterial;
            }
            else
            {
                if (baseMaterial != null)
                    materials[0] = baseMaterial;
                if (colorMaterial != null)
                    materials[1] = colorMaterial;
            }

            renderer.sharedMaterials = materials;
        }
    }
}
