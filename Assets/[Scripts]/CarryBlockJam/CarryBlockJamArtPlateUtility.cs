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
        private const string BaseMaterialName = "Mat_Plate";
        private const string ResourcesFolder = "Materials/Plates";
        private const string HiddenPlateMaterialPath = "Assets/[Materials]/Mat_HiddenPlate.mat";

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

        /// <summary>
        /// Applies the shared hidden texture material (same HiddenTexture look as tables).
        /// </summary>
        public static void ApplyHiddenPlateMaterial(Transform plateRoot)
        {
            if (plateRoot == null)
                return;

            Material hiddenMaterial = ResolveHiddenPlateMaterial();
            if (hiddenMaterial == null)
                return;

            Renderer[] renderers = plateRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = hiddenMaterial;
                    continue;
                }

                for (int slot = 0; slot < materials.Length; slot++)
                    materials[slot] = hiddenMaterial;

                renderer.sharedMaterials = materials;
            }
        }

        private static Material ResolveHiddenPlateMaterial()
        {
#if UNITY_EDITOR
            Material editorMaterial = AssetDatabase.LoadAssetAtPath<Material>(HiddenPlateMaterialPath);
            if (editorMaterial != null)
                return editorMaterial;
#endif
            return Resources.Load<Material>("Materials/Mat_HiddenPlate");
        }
    }
}
