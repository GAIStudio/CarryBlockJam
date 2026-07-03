using UnityEngine;

namespace GAITemplate
{
    [CreateAssetMenu(fileName = "GamePieceCatalog", menuName = "GAITemplate/Game Piece Catalog")]
    public class GamePieceCatalog : ScriptableObject
    {
        [Tooltip("Spawned for every level.")]
        public GameObject levelBasePrefab;

        [Tooltip("Spawned on each grid cell.")]
        public GameObject defaultPiecePrefab;

        [Tooltip("Tunnel flag'i set olan cell'lerde piece yerine spawn edilir.")]
        public GameObject tunnelPrefab;
    }
}
