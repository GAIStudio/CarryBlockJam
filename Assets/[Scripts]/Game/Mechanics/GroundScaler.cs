using System.Collections;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Scales a ground / environment root so it covers the full camera view on the
    /// horizontal ground plane. Supports tilted perspective cameras by projecting
    /// viewport corners onto the ground (avoids top/bottom gaps after FOV changes).
    ///
    /// Prefer attaching this to Art-Environment and leaving scaleTarget empty so the
    /// first child (M_BG) is scaled from its centered pivot.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundScaler : MonoBehaviour
    {
        [Tooltip("Camera used to determine screen coverage. Uses Camera.main if empty.")]
        public Camera targetCamera;

        [Tooltip("Optional transform to scale. Defaults to the first child, then this object.")]
        public Transform scaleTarget;

        [Tooltip("Extra multiplier beyond exact frustum fit.")]
        [Min(0.01f)]
        public float oversize = 1.08f;

        [Tooltip("Local-space mesh size used when Use Renderer Bounds is off. Quad/Cube = 1, Unity Plane = 10.")]
        [Min(0.001f)]
        public float meshUnitSize = 1f;

        [Tooltip("Keep the Y (vertical) scale unchanged.")]
        public bool preserveYScale = true;

        [Tooltip("Measure current size from child renderers (recommended for FBX environments).")]
        public bool useRendererBounds = true;

        [Tooltip("Project camera viewport corners onto the ground plane (needed for tilted cameras).")]
        public bool fitTiltedFrustum = true;

        [Tooltip("Ground plane height used for frustum projection. If < -900, uses the scale target position.")]
        public float groundY = -1000f;

        [Tooltip("Re-apply every frame while playing (useful while tuning FOV).")]
        public bool applyContinuously;

        private Vector3 _baseLocalScale = Vector3.one;
        private bool _baseScaleCaptured;
        private bool _applied;

        private void Awake() => StartCoroutine(ApplyEndOfFrame());

        private void LateUpdate()
        {
            if (applyContinuously)
                Apply();
        }

        private IEnumerator ApplyEndOfFrame()
        {
            // Wait until camera aspect / FOV settle for the current device.
            yield return null;
            yield return new WaitForEndOfFrame();
            Apply();
        }

        [ContextMenu("Apply")]
        public void Apply()
        {
            Camera cam = ResolveCamera();
            if (cam == null)
            {
                Debug.LogWarning("[GroundScaler] No camera found.", this);
                return;
            }

            Transform target = ResolveScaleTarget();
            if (target == null)
                return;

            if (!_baseScaleCaptured)
            {
                _baseLocalScale = target.localScale;
                _baseScaleCaptured = true;
            }

            // Always start from the authored base scale so repeated Apply calls stay stable.
            target.localScale = _baseLocalScale;

            float planeY = groundY > -900f ? groundY : target.position.y;
            if (!TryGetRequiredGroundSize(cam, planeY, out float needWidth, out float needDepth))
                return;

            needWidth *= Mathf.Max(0.01f, oversize);
            needDepth *= Mathf.Max(0.01f, oversize);

            if (!TryGetCurrentGroundSize(target, out float currentWidth, out float currentDepth))
                return;

            if (currentWidth < 0.0001f || currentDepth < 0.0001f)
                return;

            // Uniform XZ scale so cafe props / floor texture keep their proportions.
            float uniform = Mathf.Max(needWidth / currentWidth, needDepth / currentDepth);
            float scaleY = preserveYScale ? _baseLocalScale.y : _baseLocalScale.y * uniform;
            target.localScale = new Vector3(
                _baseLocalScale.x * uniform,
                scaleY,
                _baseLocalScale.z * uniform);
            _applied = true;
        }

        private Camera ResolveCamera()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null && Camera.allCamerasCount > 0)
                cam = Camera.allCameras[0];
            return cam;
        }

        private Transform ResolveScaleTarget()
        {
            if (scaleTarget != null)
                return scaleTarget;

            if (transform.childCount > 0)
                return transform.GetChild(0);

            return transform;
        }

        private bool TryGetCurrentGroundSize(Transform target, out float width, out float depth)
        {
            width = 0f;
            depth = 0f;

            if (useRendererBounds)
            {
                Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
                bool hasBounds = false;
                Bounds bounds = default;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || renderer is ParticleSystemRenderer)
                        continue;

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                if (!hasBounds)
                    return false;

                width = Mathf.Max(0.0001f, bounds.size.x);
                depth = Mathf.Max(0.0001f, bounds.size.z);
                return true;
            }

            width = Mathf.Max(0.0001f, meshUnitSize * Mathf.Abs(target.lossyScale.x));
            depth = Mathf.Max(0.0001f, meshUnitSize * Mathf.Abs(target.lossyScale.z));
            return true;
        }

        private bool TryGetRequiredGroundSize(
            Camera cam,
            float planeY,
            out float width,
            out float depth)
        {
            width = 0f;
            depth = 0f;

            if (fitTiltedFrustum &&
                TryGetFrustumGroundAabb(cam, planeY, out float minX, out float maxX, out float minZ, out float maxZ))
            {
                width = Mathf.Max(0.0001f, maxX - minX);
                depth = Mathf.Max(0.0001f, maxZ - minZ);
                return true;
            }

            // Fallback: simple frustum size at camera-to-plane distance.
            if (cam.orthographic)
            {
                float worldH = cam.orthographicSize * 2f;
                float worldW = worldH * cam.aspect;
                width = worldW;
                depth = worldH;
                return true;
            }

            float dist = Mathf.Abs(cam.transform.position.y - planeY);
            if (dist < 0.001f)
                dist = 0.001f;
            float height = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            width = height * cam.aspect;
            depth = height;
            return true;
        }

        private static bool TryGetFrustumGroundAabb(
            Camera cam,
            float planeY,
            out float minX,
            out float maxX,
            out float minZ,
            out float maxZ)
        {
            minX = minZ = float.PositiveInfinity;
            maxX = maxZ = float.NegativeInfinity;

            var ground = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            Vector3[] corners =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 1f, 0f),
            };

            int hits = 0;
            for (int i = 0; i < corners.Length; i++)
            {
                Ray ray = cam.ViewportPointToRay(corners[i]);
                if (!ground.Raycast(ray, out float enter) || enter < 0f)
                    continue;

                Vector3 point = ray.GetPoint(enter);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
                hits++;
            }

            return hits >= 3 &&
                   !float.IsInfinity(minX) &&
                   !float.IsInfinity(maxX) &&
                   !float.IsInfinity(minZ) &&
                   !float.IsInfinity(maxZ);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && _applied)
                _baseScaleCaptured = false;
        }
#endif
    }
}
