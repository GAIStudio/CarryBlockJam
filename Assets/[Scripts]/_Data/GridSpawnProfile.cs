using UnityEngine;

namespace GAITemplate
{
    [System.Serializable]
    public struct GridSpawnEntry
    {
        public int row;
        public int column;

        public GameObject prefab;

        [Tooltip("Non-playable cell (no prefab spawn).")]
        public bool isWall;
    }

    [CreateAssetMenu(fileName = "GridSpawnProfile", menuName = "GAITemplate/Grid Spawn Profile")]
    public class GridSpawnProfile : ScriptableObject
    {
        [Tooltip("Baked prefab override for cells with no explicit entry.")]
        public GameObject defaultPrefab;

        public GridSpawnEntry[] spawns;

        public bool TryGetSpawnEntry(int row, int column, out GridSpawnEntry entry)
        {
            entry = default;
            if (spawns == null)
                return false;

            for (int i = 0; i < spawns.Length; i++)
            {
                GridSpawnEntry candidate = spawns[i];
                if (candidate.row == row && candidate.column == column)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        public GameObject GetPrefabForCell(int row, int column, GamePieceCatalog catalog = null)
        {
            if (IsWallCell(row, column))
                return null;

            if (TryGetSpawnEntry(row, column, out GridSpawnEntry entry) && entry.prefab != null)
                return entry.prefab;

            if (catalog != null && catalog.defaultPiecePrefab != null)
                return catalog.defaultPiecePrefab;

            return defaultPrefab;
        }

        public bool IsWallCell(int row, int column)
        {
            if (spawns == null)
                return false;

            for (int i = 0; i < spawns.Length; i++)
            {
                GridSpawnEntry entry = spawns[i];
                if (entry.row == row && entry.column == column && entry.isWall)
                    return true;
            }

            return false;
        }
    }
}
