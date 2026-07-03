using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Geliştirme amaçlı klavye kısayolları. Sadece Editor + Development Build'de derlenir.
    ///
    /// Tuşlar:
    ///   →    Sonraki level
    ///   ←    Önceki level
    ///   R    Mevcut level'ı yeniden başlat
    ///   F    Level'ı Fail et
    ///   S    Level'ı Success et
    ///
    /// Not: SRDebugger paneli kendi trigger'ına (default: 3 finger tap / Ctrl+Shift+D) sahiptir.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeyboardManager : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) LevelNavigator.Next();
            if (Input.GetKeyDown(KeyCode.LeftArrow))  LevelNavigator.Previous();
            if (Input.GetKeyDown(KeyCode.R))          LevelNavigator.Reload();
            if (Input.GetKeyDown(KeyCode.F))          LevelNavigator.Fail();
            if (Input.GetKeyDown(KeyCode.S))          LevelNavigator.Success();
        }
#endif
    }
}
