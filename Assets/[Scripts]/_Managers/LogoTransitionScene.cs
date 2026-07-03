using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GAITemplate
{
    /// <summary>
    /// Logo transition sahnesi. Ayrı bir sahnedir ama oyuncuyu o sahneye GÖTÜRMEZ —
    /// mevcut sahnenin üstüne **additive** olarak yüklenir, logo fade in/out olur,
    /// arka planda hedef sahne yüklenir, sonra logo sahnesi temizlenir.
    ///
    /// Kullanım:
    ///   LogoTransitionScene.ReloadCurrent();
    ///   LogoTransitionScene.LoadScene(buildIndex);
    ///   LogoTransitionScene.LoadScene("SceneName");
    /// </summary>
    [DisallowMultipleComponent]
    public class LogoTransitionScene : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Logo & arka planı içeren CanvasGroup. Alpha fade için kullanılır.")]
        public CanvasGroup canvasGroup;

        [Header("Timing (toplam ~1s)")]
        [Min(0f)] public float fadeInDuration  = 0.30f;
        [Min(0f)] public float holdDuration    = 0.40f;
        [Min(0f)] public float fadeOutDuration = 0.30f;

        [Header("Setup")]
        [Tooltip("Bu logo sahnesinin Build Settings'teki adı.")]
        public string sceneName = "LogoTransition";

        // ── Transition trigger (static) ──────────────────────────────────────────────

        private static int    s_targetIndex = -1;
        private static string s_targetName  = null;
        private static bool   s_inProgress;

        public  static string LogoSceneName = "LogoTransition";

        public static void ReloadCurrent()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static void LoadScene(int buildIndex)
        {
            if (s_inProgress) return;

            // Logo sahnesi build'de yoksa direkt yükle (transition'ı atla).
            if (!IsLogoSceneInBuild())
            {
                SceneManager.LoadScene(buildIndex);
                return;
            }

            s_inProgress  = true;
            s_targetIndex = buildIndex;
            s_targetName  = null;
            SceneManager.LoadScene(LogoSceneName, LoadSceneMode.Additive);
        }

        public static void LoadScene(string targetSceneName)
        {
            if (s_inProgress) return;

            if (!IsLogoSceneInBuild())
            {
                SceneManager.LoadScene(targetSceneName);
                return;
            }

            s_inProgress  = true;
            s_targetIndex = -1;
            s_targetName  = targetSceneName;
            SceneManager.LoadScene(LogoSceneName, LoadSceneMode.Additive);
        }

        private static bool IsLogoSceneInBuild()
        {
            if (string.IsNullOrEmpty(LogoSceneName)) return false;

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == LogoSceneName) return true;
            }

            Debug.LogWarning($"[LogoTransitionScene] \"{LogoSceneName}\" sahnesi Build Settings'te yok, " +
                             "transition atlanıp direkt yükleme yapıldı.");
            return false;
        }

        // ── Lifecycle (Logo sahnesinde çalışır) ──────────────────────────────────────

        private void Awake()
        {
            if (!string.IsNullOrEmpty(sceneName))
                LogoSceneName = sceneName;
        }

        private void Start()
        {
            // Controller hiyerarşisini ve canvas hiyerarşisini sahne yüklemesinden koru.
            DontDestroyOnLoad(transform.root.gameObject);
            if (canvasGroup != null && canvasGroup.transform.root != transform.root)
                DontDestroyOnLoad(canvasGroup.transform.root.gameObject);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            // 1. Logo fade in
            if (canvasGroup != null)
                yield return canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.Linear)
                    .WaitForCompletion();

            // 2. Logo peak'te bekle
            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            // 3. Hedef sahneyi Single olarak yükle.
            //    Mevcut sahne ve logo sahnesi unload olur, ama DontDestroyOnLoad'a aldığımız
            //    canvas/controller objeleri yaşamaya devam eder → fade out yapabiliriz.
            AsyncOperation op = BeginLoadTarget();
            if (op != null)
            {
                while (!op.isDone) yield return null;
                yield return null;
            }

            // 4. Logo fade out
            if (canvasGroup != null)
                yield return canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.Linear)
                    .WaitForCompletion();

            // 5. Cleanup — DontDestroyOnLoad'taki tüm logo objelerini sil.
            if (canvasGroup != null && canvasGroup.transform.root != transform.root)
                Destroy(canvasGroup.transform.root.gameObject);
            s_inProgress = false;
            Destroy(transform.root.gameObject);
        }

        private AsyncOperation BeginLoadTarget()
        {
            if (s_targetIndex >= 0)
                return SceneManager.LoadSceneAsync(s_targetIndex, LoadSceneMode.Single);
            if (!string.IsNullOrEmpty(s_targetName))
                return SceneManager.LoadSceneAsync(s_targetName, LoadSceneMode.Single);

            Debug.LogWarning("[LogoTransitionScene] Hedef sahne set edilmemiş, transition atlanıyor.", this);
            s_inProgress = false;
            return null;
        }
    }
}
