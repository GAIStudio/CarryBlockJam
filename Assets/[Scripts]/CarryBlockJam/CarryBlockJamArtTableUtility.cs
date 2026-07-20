using GAITemplate;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    /// <summary>
    /// Applies art table materials (base + color slot) onto M_Table visuals.
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
#if UNITY_EDITOR
            Material editorMaterial = AssetDatabase.LoadAssetAtPath<Material>(HiddenTableMaterialPath);
            if (editorMaterial != null)
                return editorMaterial;
#endif
            return Resources.Load<Material>("Materials/Mat_HiddenTable");
        }
    }
}
