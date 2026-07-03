using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GAITemplate.Editor
{
    /// <summary>
    /// Build başlamadan önce Development Build durumuna göre DISABLE_SRDEBUGGER define'ını
    /// ayarlar:
    ///   Development Build = ON  → DISABLE_SRDEBUGGER kaldırılır (SRDebugger aktif)
    ///   Development Build = OFF → DISABLE_SRDEBUGGER eklenir   (SRDebugger derleme dışı)
    ///
    /// Editor'da hiç bir şey değişmez — SRDebugger Play Mode'da her zaman aktif.
    /// </summary>
    public class SRDebuggerBuildProcessor : IPreprocessBuildWithReport
    {
        private const string DisableSymbol = "DISABLE_SRDEBUGGER";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(
                BuildPipeline.GetBuildTargetGroup(report.summary.platform));

            PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
            bool hasDisable = System.Array.IndexOf(defines, DisableSymbol) >= 0;
            bool wantsDisable = !EditorUserBuildSettings.development;

            if (wantsDisable && !hasDisable)
            {
                var list = new System.Collections.Generic.List<string>(defines) { DisableSymbol };
                PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
                UnityEngine.Debug.Log($"[SRDebugger] Release build → '{DisableSymbol}' eklendi.");
            }
            else if (!wantsDisable && hasDisable)
            {
                var list = new System.Collections.Generic.List<string>(defines);
                list.Remove(DisableSymbol);
                PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
                UnityEngine.Debug.Log($"[SRDebugger] Development build → '{DisableSymbol}' kaldırıldı.");
            }
        }
    }
}
