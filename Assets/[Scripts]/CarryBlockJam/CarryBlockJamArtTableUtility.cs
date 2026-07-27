using GAITemplate;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    /// <summary>
    /// Applies art table materials (base + color slot) onto M_Table visuals.
    /// Shared table color is authored on the board/spawner prefab.
    /// </summary>
    public static class CarryBlockJamArtTableUtility
    {
        private const string BaseMaterialName = "Mat_Table";
        private const string ResourcesFolder = "Materials/Tables";
        private const string HiddenTableMaterialPath = "Assets/[Materials]/Mat_HiddenTable.mat";

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

            // Default / Grey tables use baked Mat_Table (TexBake_Table), not Mat_Table-Grey.
            bool useBakedDefault =
                !PieceColorPalette.IsPaintable(color) || color == PieceColorType.Grey;
            Material colorMaterial = useBakedDefault
                ? baseMaterial
                : CarryBlockJamArtMaterialUtility.LoadColoredMaterial(
                    CarryBlockJamArtMaterialUtility.TableMaterialsFolder,
                    BaseMaterialName,
                    color,
                    ResourcesFolder);

            if (colorMaterial == null)
                colorMaterial = baseMaterial;
            if (baseMaterial == null)
                baseMaterial = colorMaterial;
            if (baseMaterial == null && colorMaterial == null)
                return;

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = colorMaterial != null ? colorMaterial : baseMaterial;
                return;
            }

            if (useBakedDefault || materials.Length == 1)
            {
                // Single look: baked Mat_Table on every slot.
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = baseMaterial != null ? baseMaterial : colorMaterial;
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

        public static void ApplyHiddenTableMaterial(Transform tableRoot)
        {
            if (tableRoot == null)
                return;

            Material hiddenMaterial = ResolveHiddenTableMaterial();
            if (hiddenMaterial == null)
                return;

            Renderer[] renderers = tableRoot.GetComponentsInChildren<Renderer>(true);
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

        private static Material ResolveHiddenTableMaterial()
        {
            // Build + Play Mode: Resources first so editor/build match.
            Material material = Resources.Load<Material>("Materials/Mat_HiddenTable");
            if (material == null)
                material = Resources.Load<Material>("Materials/Tables/Mat_HiddenTable");
#if UNITY_EDITOR
            if (material == null)
                material = AssetDatabase.LoadAssetAtPath<Material>(HiddenTableMaterialPath);
#endif
            return material;
        }
    }
}
