using System.IO;
using UnityEditor;
using UnityEngine;

namespace CarryBlockJam.Editor
{
    /// <summary>
    /// Copies authored materials under Assets/[Materials] into Assets/Resources/Materials
    /// so player builds resolve the same mats (and their textures) as the editor.
    /// </summary>
    public static class ArtMaterialsResourcesSync
    {
        private const string MaterialsRoot = "Assets/[Materials]";
        private const string ResourcesMaterialsRoot = "Assets/Resources/Materials";

        private static readonly (string sourceFolder, string destFolder)[] ColoredFolders =
        {
            ("-Table Materials", "Tables"),
            ("-Plate Materials", "Plates"),
            ("-GateUp Materials", "Gates"),
            ("-GateBottom Materials", "Gates"),
            ("-GateLeft Materials", "Gates"),
            ("-GateRight Materials", "Gates"),
        };

        private static readonly string[] FlatMaterialNames =
        {
            "Mat_Box",
            "Mat_Ice",
            "Mat_Stickman",
            "Mat_Bowtie",
            "Mat_CharTable",
            "Mat_HiddenTable",
            "T_Ice",
            "T_IceV2",
        };

        private static readonly string[] SpriteNames =
        {
            "ColorSprite_Cricle.png",
            "ColorSprite_Plate.png",
            "ColorSprite_Rectangle.png",
        };

        [MenuItem("CarryBlockJam/Sync All Art Materials To Resources")]
        public static void SyncMenu()
        {
            int copied = SyncAll();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[CarryBlockJam] Synced {copied} art asset(s) into Resources for builds " +
                "(materials + color sprites; textures follow material dependencies).");
        }

        // Keep old menu path as alias.
        [MenuItem("CarryBlockJam/Sync Table Materials To Resources")]
        public static void SyncTablesMenu() => SyncMenu();

        public static int SyncAll()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(ResourcesMaterialsRoot);
            EnsureFolder("Assets/Resources/Sprites");

            int copied = 0;
            for (int i = 0; i < ColoredFolders.Length; i++)
            {
                string sourceFolder = $"{MaterialsRoot}/{ColoredFolders[i].sourceFolder}";
                string destFolder = $"{ResourcesMaterialsRoot}/{ColoredFolders[i].destFolder}";
                EnsureFolder(destFolder);
                copied += SyncFolder(sourceFolder, destFolder);
            }

            for (int i = 0; i < FlatMaterialNames.Length; i++)
            {
                string name = FlatMaterialNames[i];
                string sourcePath = $"{MaterialsRoot}/{name}.mat";
                string destPath = $"{ResourcesMaterialsRoot}/{name}.mat";
                if (SyncAssetFile(sourcePath, destPath))
                    copied++;
            }

            for (int i = 0; i < SpriteNames.Length; i++)
            {
                string fileName = SpriteNames[i];
                string sourcePath = $"Assets/[Sprites]/{fileName}";
                string destPath = $"Assets/Resources/Sprites/{fileName}";
                if (SyncAssetFile(sourcePath, destPath))
                    copied++;
            }

            return copied;
        }

        private static int SyncFolder(string sourceFolder, string destFolder)
        {
            if (!AssetDatabase.IsValidFolder(sourceFolder))
                return 0;

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { sourceFolder });
            int copied = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".mat"))
                    continue;

                string destPath = $"{destFolder}/{Path.GetFileName(sourcePath)}";
                if (SyncAssetFile(sourcePath, destPath))
                    copied++;
            }

            return copied;
        }

        private static bool SyncAssetFile(string sourcePath, string destPath)
        {
            if (!File.Exists(ToAbsolute(sourcePath)))
                return false;

            if (!File.Exists(ToAbsolute(destPath)))
            {
                // New Resources copy — Unity generates a unique GUID meta.
                return AssetDatabase.CopyAsset(sourcePath, destPath);
            }

            // Overwrite content only so existing Resources GUIDs stay stable.
            File.Copy(ToAbsolute(sourcePath), ToAbsolute(destPath), overwrite: true);
            return true;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string name = Path.GetFileName(assetFolder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string ToAbsolute(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
