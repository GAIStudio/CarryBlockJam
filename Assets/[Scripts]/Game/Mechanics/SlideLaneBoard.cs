using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Slide Lane board layout. `laneCount` adet SlideLane spawn eder; her lane'de `depth` slot vardır.
    /// LevelData (rows × columns) → (depth × laneCount) olarak okur:
    ///   slot 0 (front)  = LevelData row (rows - 1)
    ///   slot depth-1 (back) = LevelData row 0
    /// Böylece Grid template ile aynı şekilde "yüksek row = ön" konvansiyonu korunur.
    /// </summary>
    [DisallowMultipleComponent]
    public class SlideLaneBoard : MonoBehaviour
    {
        [Header("Layout")]
        [Min(1)] public int laneCount = 3;
        [Min(1)] public int depth     = 5;
        public float laneSpacing  = 1.2f;
        public float depthSpacing = 0.8f;

        [Tooltip("Tüm board'a uygulanacak local-space offset.")]
        public Vector3 boardOffset;

        [Header("Data")]
        [Tooltip("Lane × depth renk verilerini tutar. LevelBase tarafından runtime'da set edilir.")]
        public LevelData levelData;

        [Tooltip("Spawn edilecek piece prefab'ı. Üzerinde GamePiece + PieceJumpEffect + Collider olmalı.")]
        public GameObject piecePrefab;

        [Tooltip("Renk None olan slotlar boş bırakılır.")]
        public bool skipNoneCells = true;

        [Header("Generated")]
        public Transform lanesRoot;

        public SlideLane[] Lanes { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Application.isPlaying)
                RebuildLayout();
        }

#if UNITY_EDITOR
        [ContextMenu("Rebuild Layout")]
        private void RebuildLayoutFromContextMenu() => RebuildLayout();
#endif

        public void RebuildLayout()
        {
            EnsureLanesRoot();
            ClearChildren(lanesRoot);

            Lanes = new SlideLane[laneCount];
            float laneHalfWidth = (laneCount - 1) * laneSpacing * 0.5f;

            for (int laneIdx = 0; laneIdx < laneCount; laneIdx++)
            {
                var laneGO = new GameObject($"Lane_{laneIdx}");
                laneGO.transform.SetParent(lanesRoot, false);
                laneGO.transform.localPosition =
                    new Vector3(laneIdx * laneSpacing - laneHalfWidth, 0f, 0f) + boardOffset;

                var lane = laneGO.AddComponent<SlideLane>();
                lane.depthSpacing = depthSpacing;
                Lanes[laneIdx] = lane;

                BuildLanePieces(laneIdx, lane);
            }
        }

        // ── Lane construction ─────────────────────────────────────────────────────────

        private void BuildLanePieces(int laneIdx, SlideLane lane)
        {
            GameObject prefab = ResolvePiecePrefab();
            if (prefab == null) return;

            int dataCol = laneIdx;

            for (int slot = 0; slot < depth; slot++)
            {
                // slot 0 = front = LevelData's highest row.
                int dataRow = depth - 1 - slot;

                PieceColorType color = levelData != null
                    ? levelData.GetCellColor(dataRow, dataCol)
                    : PieceColorType.None;

                if (skipNoneCells && color == PieceColorType.None)
                    continue;

                GameObject pieceGO = Instantiate(prefab, lane.transform);
                pieceGO.name = $"Lane{laneIdx}_Slot{slot}";
                // slot 0 (front) = z=0, slot N-1 (back) = z=-(N-1)*depthSpacing
                // Pieces arkaya doğru (-Z) yığılır, WaitingArea ön tarafta kalır.
                pieceGO.transform.localPosition = new Vector3(0f, 0f, -slot * depthSpacing);
                pieceGO.transform.localRotation = Quaternion.identity;

                GamePiece gp = pieceGO.GetComponent<GamePiece>();
                if (gp != null)
                {
                    gp.ApplyColor(color);
                    gp.ApplyFlags(
                        levelData.GetCellFlag(dataRow, dataCol),
                        levelData.GetCellFlagValue(dataRow, dataCol));
                }

                lane.RegisterPiece(pieceGO.transform);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private GameObject ResolvePiecePrefab()
        {
            if (piecePrefab != null) return piecePrefab;

            // Fallback: GameManager.PieceCatalog.defaultPiecePrefab
            GamePieceCatalog catalog = GameManager.instance != null
                ? GameManager.instance.PieceCatalog
                : null;
            return PieceCatalogResolver.ResolveCellPrefab(catalog);
        }

        private void EnsureLanesRoot()
        {
            if (lanesRoot != null && lanesRoot != transform) return;

            Transform existing = transform.Find("Lanes");
            if (existing != null) { lanesRoot = existing; return; }

            var go = new GameObject("Lanes");
            go.transform.SetParent(transform, false);
            lanesRoot = go.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying) { DestroyImmediate(child.gameObject, true); continue; }
#endif
                Destroy(child.gameObject);
            }
        }
    }
}
