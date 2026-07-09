using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    [ExecuteAlways]
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CarryBlockJamExitLabelBillboard : MonoBehaviour
    {
        public void Refresh()
        {
            ApplyFacing();
        }

        private void OnEnable()
        {
            ApplyFacing();
        }

        private void LateUpdate()
        {
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (!TryGetReferenceCamera(out Camera camera))
                return;

            Vector3 toCamera = camera.transform.position - transform.position;
            if (toCamera.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(toCamera.normalized, camera.transform.up)
                * Quaternion.Euler(0f, 180f, 0f);
        }

        private static bool TryGetReferenceCamera(out Camera camera)
        {
            camera = Camera.main;
            if (camera != null)
                return true;

#if UNITY_EDITOR
            if (SceneView.lastActiveSceneView != null)
            {
                camera = SceneView.lastActiveSceneView.camera;
                return camera != null;
            }
#endif
            return false;
        }
    }
}
