using UnityEngine;

namespace GAITemplate
{
    public static class PieceCatalogResolver
    {
        public static GameObject ResolveCellPrefab(GamePieceCatalog catalog)
        {
            return catalog != null ? catalog.defaultPiecePrefab : null;
        }
    }
}
