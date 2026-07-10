using UnityEngine;

namespace GAITemplate
{
    public static class LevelCameraUtility
    {
        public static Camera ResolveGameplayCamera()
        {
            Camera camera = Camera.main;
            if (camera != null)
                return camera;

            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].enabled && cameras[i].gameObject.activeInHierarchy)
                    return cameras[i];
            }

            return null;
        }

        /// <summary>
        /// LevelData camera fields are ignored. Gameplay uses the scene hierarchy camera as authored.
        /// </summary>
        public static void ApplyToMainCamera(LevelData levelData)
        {
        }

        /// <summary>
        /// LevelData camera fields are ignored. Gameplay uses the scene hierarchy camera as authored.
        /// </summary>
        public static void ApplyToCamera(Camera camera, LevelData levelData)
        {
        }
    }
}
