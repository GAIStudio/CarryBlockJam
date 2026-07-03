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

        public static void ApplyToMainCamera(LevelData levelData)
        {
            if (levelData == null)
                return;

            Camera camera = ResolveGameplayCamera();
            if (camera == null)
            {
                Debug.LogWarning("[GAITemplate] No active camera found. Level camera settings were not applied.");
                return;
            }

            ApplyToCamera(camera, levelData);
        }

        public static void ApplyToCamera(Camera camera, LevelData levelData)
        {
            if (camera == null || levelData == null)
                return;

            Transform cameraTransform = camera.transform;
            cameraTransform.SetPositionAndRotation(
                levelData.cameraPosition,
                Quaternion.Euler(levelData.cameraRotation));

            camera.orthographic = levelData.cameraOrthographic;

            if (levelData.cameraOrthographic)
                camera.orthographicSize = levelData.cameraOrthographicSize;
            else
                camera.fieldOfView = levelData.cameraFieldOfView;
        }
    }
}
