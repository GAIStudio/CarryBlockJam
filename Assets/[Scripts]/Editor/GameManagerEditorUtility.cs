using UnityEngine;
using UnityEngine.SceneManagement;

namespace GAITemplate.Editor
{
    internal static class GameManagerEditorUtility
    {
        public static GameManager FindInOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    GameManager manager = root.GetComponentInChildren<GameManager>(true);
                    if (manager != null)
                        return manager;
                }
            }

            return null;
        }

        public static GameData GetGameDataFromOpenScenes()
        {
            GameManager manager = FindInOpenScenes();
            return manager != null ? manager.Data : null;
        }

        public static GamePieceCatalog GetPieceCatalogFromOpenScenes()
        {
            GameManager manager = FindInOpenScenes();
            return manager != null ? manager.PieceCatalog : null;
        }
    }
}
