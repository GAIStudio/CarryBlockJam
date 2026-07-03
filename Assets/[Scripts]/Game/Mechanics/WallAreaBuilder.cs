using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace GAITemplate
{
    /// <summary>
    /// Spawns a fixed grid of wall pieces, then on Start:
    ///   1. Carves (disables) any wall whose world position maps to a floor cell in the PuzzleGrid.
    ///   2. For every surviving wall, checks its four cardinal neighbours and activates the
    ///      matching child variant (Solid / Side / OuterCorner / etc.).
    ///
    /// Setup: create an empty prefab, add this component, assign fields, right-click → "Build Walls".
    /// Place the prefab in the scene. At runtime assign puzzleGrid (or leave empty to auto-resolve
    /// from LevelBase.Instance). Falls back to Physics.CheckBox if no grid is found.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class WallAreaBuilder : MonoBehaviour
    {
        [Header("Wall Generation")]
        [Tooltip("Single wall piece prefab repeated across the grid.")]
        public GameObject wallPrefab;

        [Tooltip("Columns (X) × rows (Z) of walls to spawn.")]
        public Vector2Int size = new Vector2Int(30, 30);

        [Tooltip("Distance between walls along X.")]
        public float spacingX = 1f;

        [Tooltip("Distance between walls along Z.")]
        public float spacingZ = 1f;

        [Tooltip("Local-space offset applied to the entire wall block.")]
        public Vector3 wallsPosition;

        [Tooltip("Local Y position for every spawned wall.")]
        public float wallHeight;

        [Tooltip("Generated walls are parented here. Created under this object if missing.")]
        public Transform wallsRoot;

        [Header("Carving (Runtime)")]
        [Tooltip("On Start, disable walls that sit on floor cells.")]
        public bool carveOnStart = true;

        [Tooltip("Primary: the PuzzleGrid used for exact grid-based carving. " +
                 "Leave empty to auto-resolve from LevelBase.Instance at runtime.")]
        public PuzzleGrid puzzleGrid;

        [Tooltip("Fallback (when no PuzzleGrid is found): half-extents for Physics.CheckBox.")]
        public Vector3 carveHalfExtents = new Vector3(0.4f, 0.5f, 0.4f);

        [Tooltip("Fallback: layers that count as floor.")]
        public LayerMask carveLayers = ~0;

        [Tooltip("Expands the carve detection zone around each wall centre. " +
                 "Increase X/Z to also destroy walls whose mesh visually enters the floor area. " +
                 "Y is unused for grid-based carving.")]
        public Vector3 carveExpand = Vector3.zero;

        [Header("Autotile Variants")]
        [Tooltip("Child activated when all four cardinals are walled and no diagonal is open.")]
        public string solidVariant = "Solid";

        [Tooltip("Child activated when all four cardinals are walled but one diagonal is open (concave corner).")]
        public string innerCornerVariant = "InnerCorner";

        [Tooltip("Child activated when exactly one cardinal side is open (straight wall facing open space).")]
        public string sideVariant = "Side";

        [Tooltip("Child activated when two adjacent sides are open (convex / outer corner).")]
        public string outerCornerVariant = "OuterCorner";

        [Tooltip("Child activated when two opposite sides are open (thin wall between two areas).")]
        public string straitVariant = "Strait";

        [Tooltip("Child activated when three sides are open (protrudes into open space).")]
        public string peninsulaVariant = "Peninsula";

        [Tooltip("Child activated when all four sides are open (isolated pillar).")]
        public string surroundedVariant = "Surrounded";

        [Tooltip("Extra yaw (degrees) added to every autotile rotation to match your prefab orientation. " +
                 "Reference: open side faces +Z (North) at yaw 0.")]
        public float yawOffset;

        // ── Build ─────────────────────────────────────────────────────────────────────

        [ContextMenu("Build Walls")]
        public void BuildWalls()
        {
            EnsureWallsRoot();
            ClearChildren(wallsRoot);

            if (wallPrefab == null)
            {
                Debug.LogWarning("[WallAreaBuilder] No wallPrefab assigned.", this);
                return;
            }

            int columns = Mathf.Max(1, size.x);
            int rows    = Mathf.Max(1, size.y);
            float sx    = Mathf.Max(0.01f, spacingX);
            float sz    = Mathf.Max(0.01f, spacingZ);
            float ox    = (columns - 1) * sx * 0.5f;
            float oz    = (rows    - 1) * sz * 0.5f;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Vector3 localPos = wallsPosition + new Vector3(col * sx - ox, wallHeight, row * sz - oz);

                    GameObject instance = SpawnWall();
                    instance.name = $"Wall_{col}_{row}";
                    Transform t = instance.transform;
                    t.SetParent(wallsRoot, false);
                    t.localPosition = localPos;
                    t.localRotation = Quaternion.identity;
                }
            }

            MarkDirty();
        }

        [ContextMenu("Clear Walls")]
        public void ClearWalls()
        {
            EnsureWallsRoot();
            ClearChildren(wallsRoot);
            MarkDirty();
        }

        // ── Runtime ───────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (puzzleGrid == null && LevelBase.Instance != null)
                puzzleGrid = LevelBase.Instance.GetComponent<PuzzleGrid>();

            ApplyWallHeight();

            if (!carveOnStart)
                return;

            if (puzzleGrid == null)
                Physics.SyncTransforms();

            CarveAndStyle();
        }

        private void ApplyWallHeight()
        {
            if (wallsRoot == null)
                return;

            for (int i = 0; i < wallsRoot.childCount; i++)
            {
                Transform wall = wallsRoot.GetChild(i);
                if (wall == null)
                    continue;

                Vector3 lp = wall.localPosition;
                lp.y = wallHeight;
                wall.localPosition = lp;
            }
        }

        public void CarveAndStyle()
        {
            if (wallsRoot == null)
                return;

            int childCount = wallsRoot.childCount;
            float sx = Mathf.Max(0.01f, spacingX);
            float sz = Mathf.Max(0.01f, spacingZ);

            // Pass 1 — carve: disable walls that land on floor cells.
            for (int i = 0; i < childCount; i++)
            {
                Transform wall = wallsRoot.GetChild(i);
                if (wall == null || !wall.gameObject.activeSelf)
                    continue;

                if (IsFloor(wall.position))
                    wall.gameObject.SetActive(false);
            }

            // Pass 2 — index: collect world positions of all surviving walls.
            var activeSet = new HashSet<Vector3Int>();
            for (int i = 0; i < childCount; i++)
            {
                Transform wall = wallsRoot.GetChild(i);
                if (wall != null && wall.gameObject.activeSelf)
                    activeSet.Add(PosKey(wall.position));
            }

            // Pass 3 — style: resolve variant + rotation for each surviving wall.
            for (int i = 0; i < childCount; i++)
            {
                Transform wall = wallsRoot.GetChild(i);
                if (wall == null || !wall.gameObject.activeSelf)
                    continue;

                Vector3 p = wall.position;
                bool n  = activeSet.Contains(PosKey(p + new Vector3(  0, 0,  sz)));
                bool e  = activeSet.Contains(PosKey(p + new Vector3( sx, 0,   0)));
                bool s  = activeSet.Contains(PosKey(p + new Vector3(  0, 0, -sz)));
                bool w  = activeSet.Contains(PosKey(p + new Vector3(-sx, 0,   0)));
                bool ne = activeSet.Contains(PosKey(p + new Vector3( sx, 0,  sz)));
                bool se = activeSet.Contains(PosKey(p + new Vector3( sx, 0, -sz)));
                bool sw = activeSet.Contains(PosKey(p + new Vector3(-sx, 0, -sz)));
                bool nw = activeSet.Contains(PosKey(p + new Vector3(-sx, 0,  sz)));

                ResolveShape(n, e, s, w, ne, se, sw, nw, out string childName, out int yawSteps);
                ApplyVariant(wall, childName, yawSteps);
            }
        }

        private bool IsFloor(Vector3 worldPos)
        {
            if (puzzleGrid != null && puzzleGrid.IsBuilt)
                return IsFloorByGrid(worldPos);

            return Physics.CheckBox(worldPos, carveHalfExtents, Quaternion.identity,
                                    carveLayers, QueryTriggerInteraction.Collide);
        }

        private bool IsFloorByGrid(Vector3 worldPos)
        {
            if (GridPointIsFloor(worldPos)) return true;

            float ex = carveExpand.x;
            float ez = carveExpand.z;
            if (ex <= 0f && ez <= 0f) return false;

            return GridPointIsFloor(worldPos + new Vector3( ex, 0,  0)) ||
                   GridPointIsFloor(worldPos + new Vector3(-ex, 0,  0)) ||
                   GridPointIsFloor(worldPos + new Vector3(  0, 0,  ez)) ||
                   GridPointIsFloor(worldPos + new Vector3(  0, 0, -ez));
        }

        private bool GridPointIsFloor(Vector3 worldPos)
        {
            if (!puzzleGrid.TryGetGridIndices(worldPos, out int row, out int col))
                return false;

            if (!puzzleGrid.TryGetCell(row, col, out PuzzleCell cell))
                return false;

            return cell != null && !cell.IsWall;
        }

        // ── Shape resolution ──────────────────────────────────────────────────────────

        private void ResolveShape(bool n, bool e, bool s, bool w,
                                  bool ne, bool se, bool sw, bool nw,
                                  out string childName, out int yawSteps)
        {
            int wallCount = (n?1:0) + (e?1:0) + (s?1:0) + (w?1:0);

            switch (wallCount)
            {
                case 4:
                    if (!ne) { childName = innerCornerVariant; yawSteps = 0; return; }
                    if (!se) { childName = innerCornerVariant; yawSteps = 1; return; }
                    if (!sw) { childName = innerCornerVariant; yawSteps = 2; return; }
                    if (!nw) { childName = innerCornerVariant; yawSteps = 3; return; }
                    childName = solidVariant; yawSteps = 0;
                    return;

                case 3:
                    // 1 open side. Reference: open side faces +Z (North) at yaw 0.
                    childName = sideVariant;
                    yawSteps  = !n ? 0 : !e ? 1 : !s ? 2 : 3;
                    return;

                case 2:
                    // Two opposite open sides → Strait. Reference: open N+S at yaw 0.
                    if (!n && !s) { childName = straitVariant; yawSteps = 0; return; }
                    if (!e && !w) { childName = straitVariant; yawSteps = 1; return; }
                    // Two adjacent open sides → OuterCorner. Reference: open N+E at yaw 0.
                    childName = outerCornerVariant;
                    yawSteps  = (!n && !e) ? 0 : (!e && !s) ? 1 : (!s && !w) ? 2 : 3;
                    return;

                case 1:
                    // 3 open sides → Peninsula. Reference: single wall neighbour is North at yaw 0.
                    childName = peninsulaVariant;
                    yawSteps  = n ? 0 : e ? 1 : s ? 2 : 3;
                    return;

                default:
                    childName = surroundedVariant; yawSteps = 0;
                    return;
            }
        }

        private void ApplyVariant(Transform wall, string childName, int yawSteps)
        {
            wall.localRotation = Quaternion.Euler(0f, yawSteps * 90f + yawOffset, 0f);

            if (string.IsNullOrEmpty(childName))
                return;

            for (int i = 0; i < wall.childCount; i++)
            {
                Transform child = wall.GetChild(i);
                bool active = child.name == childName;
                if (child.gameObject.activeSelf != active)
                    child.gameObject.SetActive(active);
            }
        }

        // ── Utilities ─────────────────────────────────────────────────────────────────

        private static Vector3Int PosKey(Vector3 pos) => new Vector3Int(
            Mathf.RoundToInt(pos.x * 100f),
            Mathf.RoundToInt(pos.y * 100f),
            Mathf.RoundToInt(pos.z * 100f));

        private GameObject SpawnWall()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var inst = PrefabUtility.InstantiatePrefab(wallPrefab, wallsRoot) as GameObject;
                if (inst != null) return inst;
            }
#endif
            return Instantiate(wallPrefab, wallsRoot);
        }

        private void EnsureWallsRoot()
        {
            if (wallsRoot != null && wallsRoot != transform)
                return;

            Transform existing = transform.Find("Walls");
            if (existing != null) { wallsRoot = existing; return; }

            var go = new GameObject("Walls");
            go.transform.SetParent(transform, false);
            wallsRoot = go.transform;
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            EditorUtility.SetDirty(gameObject);
            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            int columns = Mathf.Max(1, size.x);
            int rows    = Mathf.Max(1, size.y);
            float sx    = Mathf.Max(0.01f, spacingX);
            float sz    = Mathf.Max(0.01f, spacingZ);

            Gizmos.color  = new Color(0.3f, 0.7f, 1f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(
                wallsPosition + new Vector3(0, wallHeight, 0),
                new Vector3((columns - 1) * sx + sx, 0.1f, (rows - 1) * sz + sz));
        }
#endif
    }
}
