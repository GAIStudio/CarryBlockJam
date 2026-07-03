using UnityEngine;
using UnityEngine.SceneManagement;

namespace GAITemplate
{
    /// <summary>
    /// Level'lar arası geçişi kolaylaştıran helper. Hem AdminPanel hem KeyboardManager bunu kullanır.
    /// DataManager üzerinden level numarasını persist eder, sonra sahneyi yeniden yükler.
    /// </summary>
    public static class LevelNavigator
    {
        /// <summary>Sıradaki level'a geçer (mevcut + 1).</summary>
        public static void Next() => GoTo(Mathf.Max(0, GameManager.instance.level) + 1);

        /// <summary>Bir önceki level'a geçer (en az 0).</summary>
        public static void Previous() => GoTo(Mathf.Max(0, GameManager.instance.level - 1));

        /// <summary>Mevcut level'ı yeniden yükler.</summary>
        public static void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Belirli bir level numarasına atlar.</summary>
        public static void GoTo(int level)
        {
            level = Mathf.Max(0, level);
            if (DataManager.instance != null)
                DataManager.instance.SetLevel(level);
            if (GameManager.instance != null)
                GameManager.instance.level = level;

            Reload();
        }

        /// <summary>Mevcut sahnedeki LevelManager.Success() çağırır.</summary>
        public static void Success()
        {
            if (LevelManager.instance != null)
                LevelManager.instance.Success();
        }

        /// <summary>Mevcut sahnedeki LevelManager.Fail() çağırır.</summary>
        public static void Fail()
        {
            if (LevelManager.instance != null)
                LevelManager.instance.Fail();
        }
    }
}
