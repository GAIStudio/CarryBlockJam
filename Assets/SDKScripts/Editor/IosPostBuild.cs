#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class IosPostBuild
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = path + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        PlistElementDict rootDict = plist.root;

        // [TEMPLATE AYARI] Burayı oyunun diline veya stratejisine göre güncelleyin.
        // Kullanıcıya neden takip izni istediğimizi açıklayan metin.
        string trackingDesc = "We will use your data to provide a better and personalized ad experience.";
        
        rootDict.SetString("NSUserTrackingUsageDescription", trackingDesc);

        File.WriteAllText(plistPath, plist.WriteToString());
        UnityEngine.Debug.Log("✅ [iOS Setup] Info.plist otomatik yapılandırıldı.");
    }
}
#endif