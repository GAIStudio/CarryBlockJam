using System.Collections.Generic;
using GAITemplate;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarryBlockJam
{
    [DisallowMultipleComponent]
    public sealed class CarryBlockJamFrozenBox : MonoBehaviour
    {
        private const string FrozenBoxModelPath = "Assets/[Models]/IceV01.fbx";
        private const string FrozenBoxPrefabPath = "Assets/[Prefabs]/IceV01.prefab";
        private const string FrozenBoxMaterialPath = "Assets/[Materials]/T_Ice.mat";

        private CarryBlockJamBoardPiece _boxPiece;
        private Transform _boxVisualRoot;
        private readonly List<Renderer> _hiddenBoxRenderers = new List<Renderer>();
        private GameObject _iceOverlay;
        private TMP_Text _unlockLabel;
        private BoardFrozenBoxVisualSettings _visualSettings;
        private int _remainingUnlockMoves;
        private bool _isUnfrozen;

        public bool IsUnfrozen => _isUnfrozen;
        public int RemainingUnlockMoves => _remainingUnlockMoves;

        public void Bind(
            CarryBlockJamBoardPiece boxPiece,
            Transform boxVisualRoot,
            int unlockMoves,
            BoardFrozenBoxVisualSettings visualSettings)
        {
            _boxPiece = boxPiece;
            _boxVisualRoot = boxVisualRoot;
            _visualSettings = visualSettings ?? BoardFrozenBoxVisualSettings.CreateDefault();
            _remainingUnlockMoves = Mathf.Max(1, unlockMoves);
            _isUnfrozen = false;

            CreateIceOverlay();
            HideBoxVisual();
            RefreshUnlockLabel();
        }

        private void LateUpdate()
        {
            if (_isUnfrozen || _unlockLabel == null || _visualSettings == null)
                return;

            UpdateUnlockLabelPosition();
        }

        public static void NotifyPlateCollected(CarryBlockJamBoardPiece plate)
        {
            if (plate == null)
                return;

            CarryBlockJamFrozenBox[] frozenBoxes = FindObjectsOfType<CarryBlockJamFrozenBox>();
            for (int i = 0; i < frozenBoxes.Length; i++)
                frozenBoxes[i].HandlePlateCollected();
        }

        private void HandlePlateCollected()
        {
            if (_isUnfrozen)
                return;

            _remainingUnlockMoves = Mathf.Max(0, _remainingUnlockMoves - 1);
            RefreshUnlockLabel();

            if (_remainingUnlockMoves <= 0)
                Unfreeze();
        }

        private void Unfreeze()
        {
            if (_isUnfrozen || _boxPiece == null)
                return;

            _isUnfrozen = true;
            _boxPiece.Unfreeze();
            ShowBoxVisual();

            if (_iceOverlay != null)
                Destroy(_iceOverlay);

            if (_unlockLabel != null)
                Destroy(_unlockLabel.gameObject);

            enabled = false;
        }

        private void CreateIceOverlay()
        {
            GameObject modelPrefab = ResolveModelPrefab(_visualSettings);
            if (modelPrefab == null)
            {
                Debug.LogWarning("[CarryBlockJam] Frozen box ice model is missing.");
                CreateUnlockLabel();
                return;
            }

            _iceOverlay = Instantiate(modelPrefab, transform, false);
            _iceOverlay.name = "FrozenOverlay";
            _iceOverlay.transform.localRotation = Quaternion.identity;
            _iceOverlay.transform.localScale = Vector3.one;
            _iceOverlay.transform.localPosition = Vector3.zero;

            DisableParticles(_iceOverlay);
            ApplyIceMaterial(_iceOverlay, ResolveMaterial(_visualSettings));

            if (_visualSettings.autoFitToTable)
                FitIceOverlayToBoxVisual();
            else
            {
                _iceOverlay.transform.localScale = _visualSettings.scale;
                _iceOverlay.transform.localPosition = _visualSettings.offset;
            }

            Collider[] colliders = _iceOverlay.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Destroy(colliders[i]);

            CreateUnlockLabel();
        }

        private void CreateUnlockLabel()
        {
            TMP_FontAsset font = CarryBlockJamExitLabelUtility.ResolveFont(_visualSettings.font);
            if (font == null)
            {
                Debug.LogWarning(
                    "[CarryBlockJam] Skipping frozen unlock label: no TMP font available.",
                    this);
                return;
            }

            if (TMP_Settings.LoadDefaultSettings() == null)
            {
                Debug.LogWarning("[CarryBlockJam] Skipping frozen unlock label: TMP Settings missing.", this);
                return;
            }

            var labelObject = new GameObject("UnlockMoves");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = GetUnlockLabelLocalPosition();
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = _visualSettings.textScale;

            _unlockLabel = labelObject.AddComponent<TextMeshPro>();
            _unlockLabel.font = font;
            _unlockLabel.alignment = TextAlignmentOptions.Center;
            _unlockLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            _unlockLabel.fontSize = _visualSettings.fontSize;
            _unlockLabel.fontStyle = _visualSettings.bold ? FontStyles.Bold : FontStyles.Normal;
            _unlockLabel.color = _visualSettings.textColor;
            _unlockLabel.enableWordWrapping = false;
            _unlockLabel.isTextObjectScaleStatic = true;

            ApplyUnlockLabelOutline();
            EnsureUnlockLabelRenderOrder();

            _unlockLabel.text = _remainingUnlockMoves.ToString();
            if (_unlockLabel.font != null && _unlockLabel.font.material != null)
                _unlockLabel.ForceMeshUpdate(true);

            labelObject.AddComponent<CarryBlockJamExitLabelBillboard>();
        }

        private void ApplyUnlockLabelOutline()
        {
            if (_unlockLabel == null || _visualSettings == null)
                return;

            CarryBlockJamExitLabelUtility.ApplyRuntimeOutline(_unlockLabel, _visualSettings.useOutline);
        }

        private void EnsureUnlockLabelRenderOrder()
        {
            if (_unlockLabel == null)
                return;

            Renderer renderer = _unlockLabel.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                return;

            const int labelRenderQueue = 3010;

            if (Application.isPlaying)
            {
                renderer.material.renderQueue = labelRenderQueue;
                return;
            }

#if UNITY_EDITOR
            Material sceneMaterial = new Material(renderer.sharedMaterial);
            sceneMaterial.renderQueue = labelRenderQueue;
            renderer.sharedMaterial = sceneMaterial;
#endif
        }

        private void UpdateUnlockLabelPosition()
        {
            if (_unlockLabel == null)
                return;

            _unlockLabel.transform.localPosition = GetUnlockLabelLocalPosition();
        }

        private Vector3 GetUnlockLabelLocalPosition()
        {
            BoardFrozenBoxVisualSettings settings = _visualSettings ?? BoardFrozenBoxVisualSettings.CreateDefault();

            if (_iceOverlay != null &&
                TryGetLocalRendererBounds(_iceOverlay.transform, transform, out Bounds iceBounds))
            {
                return GetLabelPositionOnBoundsSurface(iceBounds, settings);
            }

            if (_boxVisualRoot != null &&
                TryGetLocalRendererBounds(_boxVisualRoot, transform, out Bounds boxBounds, includeDisabled: true))
            {
                return GetLabelPositionOnBoundsSurface(boxBounds, settings);
            }

            return settings.textOffset;
        }

        private Vector3 GetLabelPositionOnBoundsSurface(Bounds localBounds, BoardFrozenBoxVisualSettings settings)
        {
            Vector3 position = localBounds.center + settings.textOffset;
            if (!TryGetReferenceCameraWorldPosition(out Vector3 cameraWorldPosition))
                return position;

            Vector3 worldCenter = transform.TransformPoint(localBounds.center);
            Vector3 toCamera = cameraWorldPosition - worldCenter;
            if (toCamera.sqrMagnitude < 0.0001f)
                return position;

            Vector3 localCameraDirection = transform.InverseTransformDirection(toCamera.normalized);
            float surfaceDistance = GetBoundsSurfaceDistance(localBounds, localCameraDirection);
            const float surfacePadding = 0.05f;
            return position + localCameraDirection * (surfaceDistance + surfacePadding);
        }

        private static float GetBoundsSurfaceDistance(Bounds localBounds, Vector3 localDirectionNormalized)
        {
            Vector3 absDirection = new Vector3(
                Mathf.Abs(localDirectionNormalized.x),
                Mathf.Abs(localDirectionNormalized.y),
                Mathf.Abs(localDirectionNormalized.z));
            return Vector3.Dot(localBounds.extents, absDirection);
        }

        private static bool TryGetReferenceCameraWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = default;
            Camera camera = Camera.main;
#if UNITY_EDITOR
            if (camera == null && SceneView.lastActiveSceneView != null)
                camera = SceneView.lastActiveSceneView.camera;
#endif
            if (camera == null)
                return false;

            worldPosition = camera.transform.position;
            return true;
        }

        private void HideBoxVisual()
        {
            _hiddenBoxRenderers.Clear();
            if (_boxVisualRoot == null)
                return;

            Renderer[] renderers = _boxVisualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                _hiddenBoxRenderers.Add(renderer);
                renderer.enabled = false;
            }
        }

        private void ShowBoxVisual()
        {
            for (int i = 0; i < _hiddenBoxRenderers.Count; i++)
            {
                Renderer renderer = _hiddenBoxRenderers[i];
                if (renderer != null)
                    renderer.enabled = true;
            }

            _hiddenBoxRenderers.Clear();
        }

        private void FitIceOverlayToBoxVisual()
        {
            if (_iceOverlay == null || _boxVisualRoot == null)
                return;

            if (!TryGetLocalRendererBounds(_boxVisualRoot, transform, out Bounds boxBounds, includeDisabled: true))
                return;

            if (!TryGetLocalRendererBounds(_iceOverlay.transform, transform, out Bounds iceBounds))
                return;

            Vector3 iceSize = iceBounds.size;
            if (iceSize.x <= 0.0001f || iceSize.y <= 0.0001f || iceSize.z <= 0.0001f)
                return;

            float padding = Mathf.Max(0.1f, _visualSettings.coverPadding);
            Vector3 targetSize = boxBounds.size * padding;
            Vector3 fitScale = new Vector3(
                targetSize.x / iceSize.x,
                targetSize.y / iceSize.y,
                targetSize.z / iceSize.z);

            Vector3 resolvedScale = Vector3.Scale(fitScale, _visualSettings.scale);
            _iceOverlay.transform.localScale = resolvedScale;

            if (!TryGetLocalRendererBounds(_iceOverlay.transform, transform, out Bounds fittedIceBounds))
                fittedIceBounds = iceBounds;

            Vector3 positionDelta = boxBounds.center - fittedIceBounds.center;
            _iceOverlay.transform.localPosition = positionDelta + _visualSettings.offset;
        }

        private static bool TryGetLocalRendererBounds(
            Transform target,
            Transform root,
            out Bounds bounds,
            bool includeDisabled = false)
        {
            bounds = default;
            if (target == null || root == null)
                return false;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer is ParticleSystemRenderer)
                    continue;

                if (!renderer.enabled && !includeDisabled)
                    continue;

                Bounds worldBounds = renderer.bounds;
                Vector3 localMin = root.InverseTransformPoint(worldBounds.min);
                Vector3 localMax = root.InverseTransformPoint(worldBounds.max);
                var localBounds = new Bounds((localMin + localMax) * 0.5f, localMax - localMin);

                if (!hasBounds)
                {
                    bounds = localBounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(localBounds.min);
                bounds.Encapsulate(localBounds.max);
            }

            return hasBounds;
        }

        private void RefreshUnlockLabel()
        {
            if (_unlockLabel == null)
                return;

            _unlockLabel.text = _remainingUnlockMoves.ToString();
            if (_unlockLabel.font != null && _unlockLabel.font.material != null)
                _unlockLabel.ForceMeshUpdate(true);
        }

        private static GameObject ResolveModelPrefab(BoardFrozenBoxVisualSettings settings)
        {
            if (settings?.model != null)
                return settings.model;

#if UNITY_EDITOR
            GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBoxModelPath);
            if (fbxModel != null)
                return fbxModel;

            return AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBoxPrefabPath);
#else
            return Resources.Load<GameObject>("IceV01");
#endif
        }

        private static Material ResolveMaterial(BoardFrozenBoxVisualSettings settings)
        {
            if (settings?.material != null)
                return settings.material;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Material>(FrozenBoxMaterialPath);
#else
            return null;
#endif
        }

        private static void DisableParticles(GameObject overlay)
        {
            ParticleSystem[] particleSystems = overlay.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
                particleSystems[i].gameObject.SetActive(false);
        }

        private static void ApplyIceMaterial(GameObject overlay, Material material)
        {
            if (overlay == null || material == null)
                return;

            Renderer[] renderers = overlay.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer)
                    continue;

                renderers[i].enabled = true;
                renderers[i].sharedMaterial = material;
            }
        }
    }
}
