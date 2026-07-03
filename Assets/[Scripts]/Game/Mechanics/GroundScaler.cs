using System.Collections;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Scales this GameObject's X and Z so it covers the full screen on Awake,
    /// based on the assigned camera's projection (orthographic or perspective).
    ///
    /// Assumptions:
    ///   - The mesh is 1 × 1 in local XZ (Quad, Cube).
    ///   - For a Unity Plane (10 × 10 local units) set Mesh Unit Size to 10.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundScaler : MonoBehaviour
    {
        [Tooltip("Camera used to determine screen dimensions. Uses Camera.main if left empty.")]
        public Camera targetCamera;

        [Tooltip("Extra multiplier so the ground extends slightly beyond screen edges. " +
                 "1.0 = exact fit, 1.1 = 10% bigger on each axis.")]
        [Min(0.01f)]
        public float oversize = 1.05f;

        [Tooltip("Local-space size of the mesh on X and Z. " +
                 "Quad / Cube = 1  |  Unity Plane = 10")]
        [Min(0.001f)]
        public float meshUnitSize = 1f;

        [Tooltip("Keep the Y (vertical) scale unchanged.")]
        public bool preserveYScale = true;

        private void Awake() => StartCoroutine(ApplyEndOfFrame());

        private IEnumerator ApplyEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            Apply();
        }

        [ContextMenu("Apply")]
        public void Apply()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null && Camera.allCameras.Length > 0)
                cam = Camera.allCameras[0];
            if (cam == null)
            {
                Debug.LogWarning("[GroundScaler] No camera found.", this);
                return;
            }

            float worldW, worldH;

            if (cam.orthographic)
            {
                worldH = cam.orthographicSize * 2f;
                worldW = worldH * cam.aspect;
            }
            else
            {
                float dist = Mathf.Abs(cam.transform.position.y - transform.position.y);
                if (dist < 0.001f) dist = 0.001f;
                worldH = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                worldW = worldH * cam.aspect;
            }

            float scaleY = preserveYScale ? transform.localScale.y : 1f;
            transform.localScale = new Vector3(
                worldW * oversize / meshUnitSize,
                scaleY,
                worldH * oversize / meshUnitSize);
        }
    }
}
