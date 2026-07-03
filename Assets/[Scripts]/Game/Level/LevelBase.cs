using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace GAITemplate
{
    /// <summary>
    /// Tüm puzzle level base'lerinin runtime kurulum noktası.
    /// LevelData.mechanicType'a göre Grid veya SlideLane modunda kurar.
    /// </summary>
    public class LevelBase : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Runtime-spawned grid cells / lanes are parented here. Created under LevelBase if missing.")]
        [FormerlySerializedAs("gridRoot")]
        public Transform cellsRoot;

        [Header("Grid")]
        [Tooltip("Grid mekaniği için layout komponenti. Yoksa runtime'da eklenir.")]
        public PuzzleBoardLayout boardLayout;

        [Header("Slide Lane")]
        [Tooltip("SlideLane mekaniği için layout komponenti. Yoksa runtime'da eklenir.")]
        public SlideLaneBoard slideLaneBoard;

        [Header("Camera")]
        [Tooltip("Optional. Falls back to Camera.main when empty.")]
        public Camera levelCamera;

        public static LevelBase Instance { get; private set; }

        public LevelData ActiveLevelData { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Build ─────────────────────────────────────────────────────────────────────

        public void BuildLevel(LevelData levelData)
        {
            ActiveLevelData = levelData;

            if (levelData == null)
            {
                Debug.LogError("[LevelBase] LevelData is null.", this);
                return;
            }

            switch (levelData.mechanicType)
            {
                case PuzzleMechanicType.Grid:
                    BuildGridLevel(levelData);
                    break;
                case PuzzleMechanicType.SlideLane:
                    BuildSlideLaneLevel(levelData);
                    break;
                default:
                    Debug.LogError($"[LevelBase] Unsupported mechanic type: {levelData.mechanicType}", this);
                    return;
            }

            SetupCamera(levelData);

            // Tutorial'ı build + camera setup sonrası başlat (positions hazır olmalı).
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.StartTutorial(levelData);
        }

        private void BuildGridLevel(LevelData levelData)
        {
            if (boardLayout == null) boardLayout = GetComponent<PuzzleBoardLayout>();
            if (boardLayout == null) boardLayout = gameObject.AddComponent<PuzzleBoardLayout>();
            if (GetComponent<PuzzleGrid>() == null) gameObject.AddComponent<PuzzleGrid>();

            boardLayout.mechanicType = PuzzleMechanicType.Grid;
            boardLayout.rows         = levelData.gridRows;
            boardLayout.columns      = levelData.gridColumns;
            boardLayout.gridSpacingX = ResolveGridSpacingX();
            boardLayout.gridSpacingZ = ResolveGridSpacingZ();
            boardLayout.levelData    = levelData;
            boardLayout.slotsRoot    = EnsureCellsRoot();
            boardLayout.RebuildLayout();
        }

        private void BuildSlideLaneLevel(LevelData levelData)
        {
            if (slideLaneBoard == null) slideLaneBoard = GetComponent<SlideLaneBoard>();
            if (slideLaneBoard == null) slideLaneBoard = gameObject.AddComponent<SlideLaneBoard>();

            slideLaneBoard.laneCount = levelData.gridColumns;
            slideLaneBoard.depth     = levelData.gridRows;
            slideLaneBoard.levelData = levelData;
            slideLaneBoard.lanesRoot = EnsureCellsRoot();
            slideLaneBoard.RebuildLayout();
        }

        // ── Camera ────────────────────────────────────────────────────────────────────

        public void SetupCamera(LevelData levelData)
        {
            if (levelData == null)
                return;

            if (levelCamera == null)
                levelCamera = LevelCameraUtility.ResolveGameplayCamera();

            if (levelCamera == null)
            {
                Debug.LogWarning("[LevelBase] No camera found for level setup.", this);
                return;
            }

            LevelCameraUtility.ApplyToCamera(levelCamera, levelData);
            StartCoroutine(ForceCameraSettingsAfterFrame(levelData));
        }

        private IEnumerator ForceCameraSettingsAfterFrame(LevelData levelData)
        {
            yield return new WaitForEndOfFrame();

            if (levelData == null)
                yield break;

            if (levelCamera == null)
                levelCamera = LevelCameraUtility.ResolveGameplayCamera();

            if (levelCamera != null)
                LevelCameraUtility.ApplyToCamera(levelCamera, levelData);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static float ResolveGridSpacingX()
        {
            return GameManager.instance != null ? GameManager.instance.GridSpacingX : 1f;
        }

        private static float ResolveGridSpacingZ()
        {
            return GameManager.instance != null ? GameManager.instance.GridSpacingZ : 1f;
        }

        private Transform EnsureCellsRoot()
        {
            if (cellsRoot != null)
                return cellsRoot;

            Transform existing = transform.Find("Cells");
            if (existing == null) existing = transform.Find("Lanes");

            if (existing != null)
            {
                cellsRoot = existing;
                return cellsRoot;
            }

            var cellsObject = new GameObject("Cells");
            cellsObject.transform.SetParent(transform, false);
            cellsRoot = cellsObject.transform;
            return cellsRoot;
        }
    }
}
