using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class SdkDependencyInstaller
{
    static SdkDependencyInstaller()
    {
        InstallDependencies();
    }

    public static void InstallDependencies()
    {
        try
        {
            string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[SDK Installer] manifest.json not found at: " + manifestPath);
                return;
            }

            string manifestText = File.ReadAllText(manifestPath);
            bool modified = false;

            // 1. Install iOS 14 Advertising Support package if missing (using exact dependency match with colon)
            if (!manifestText.Contains("\"com.unity.ads.ios-support\":"))
            {
                manifestText = AddDependency(manifestText, "com.unity.ads.ios-support", "1.2.0");
                modified = true;
                Debug.Log("[SDK Installer] Adding com.unity.ads.ios-support to dependencies...");
            }

            // 2. Install AppLovin Scoped Registry if missing
            if (!manifestText.Contains("https://unity.packages.applovin.com/"))
            {
                manifestText = AddScopedRegistry(manifestText);
                modified = true;
                Debug.Log("[SDK Installer] Adding AppLovin Scoped Registry to manifest...");
            }

            // 3. Install OpenUPM Scoped Registry if missing (required for Google External Dependency Manager)
            if (!manifestText.Contains("https://package.openupm.com"))
            {
                manifestText = AddOpenUpmRegistry(manifestText);
                modified = true;
                Debug.Log("[SDK Installer] Adding OpenUPM Scoped Registry to manifest...");
            }

            // 4. Install AppLovin MAX SDK package if missing (using exact dependency match with colon)
            if (!manifestText.Contains("\"com.applovin.mediation.ads\":"))
            {
                manifestText = AddDependency(manifestText, "com.applovin.mediation.ads", "8.6.4");
                modified = true;
                Debug.Log("[SDK Installer] Adding com.applovin.mediation.ads to dependencies...");
            }

            if (modified)
            {
                File.WriteAllText(manifestPath, manifestText);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log("[SDK Installer] manifest.json successfully updated! Unity should start downloading the packages.");
            }
            else
            {
                Debug.Log("[SDK Installer] All dependencies (AppLovin MAX, iOS Support, OpenUPM) are already configured in manifest.json.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SDK Installer] Exception occurred during dependency installation: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private static string AddDependency(string manifestText, string packageName, string version)
    {
        string searchStr = "\"dependencies\": {";
        int index = manifestText.IndexOf(searchStr);
        if (index != -1)
        {
            int insertIndex = index + searchStr.Length;
            string insertText = $"\n    \"{packageName}\": \"{version}\",";
            return manifestText.Insert(insertIndex, insertText);
        }
        return manifestText;
    }

    private static string AddScopedRegistry(string manifestText)
    {
        string registryJson = @"{
      ""name"": ""AppLovin MAX Unity"",
      ""url"": ""https://unity.packages.applovin.com/"",
      ""scopes"": [
        ""com.applovin.mediation.ads"",
        ""com.applovin.mediation.adapters"",
        ""com.applovin.mediation.dsp""
      ]
    }";

        if (manifestText.Contains("\"scopedRegistries\""))
        {
            string searchStr = "\"scopedRegistries\": [";
            int index = manifestText.IndexOf(searchStr);
            if (index != -1)
            {
                int insertIndex = index + searchStr.Length;
                return manifestText.Insert(insertIndex, $"\n    {registryJson},");
            }
        }
        else
        {
            int lastBrace = manifestText.LastIndexOf('}');
            if (lastBrace != -1)
            {
                string insertText = $",\n  \"scopedRegistries\": [\n    {registryJson}\n  ]\n";
                return manifestText.Insert(lastBrace, insertText);
            }
        }
        return manifestText;
    }

    private static string AddOpenUpmRegistry(string manifestText)
    {
        string registryJson = @"{
      ""name"": ""package.openupm.com"",
      ""url"": ""https://package.openupm.com"",
      ""scopes"": [
        ""com.google.external-dependency-manager""
      ]
    }";

        if (manifestText.Contains("\"scopedRegistries\""))
        {
            string searchStr = "\"scopedRegistries\": [";
            int index = manifestText.IndexOf(searchStr);
            if (index != -1)
            {
                int insertIndex = index + searchStr.Length;
                return manifestText.Insert(insertIndex, $"\n    {registryJson},");
            }
        }
        else
        {
            int lastBrace = manifestText.LastIndexOf('}');
            if (lastBrace != -1)
            {
                string insertText = $",\n  \"scopedRegistries\": [\n    {registryJson}\n  ]\n";
                return manifestText.Insert(lastBrace, insertText);
            }
        }
        return manifestText;
    }
}
