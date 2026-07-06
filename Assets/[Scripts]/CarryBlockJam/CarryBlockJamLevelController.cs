using GAITemplate;
using UnityEngine;

namespace CarryBlockJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CarryBlockJamSimpleBoard))]
    public class CarryBlockJamLevelController : MonoBehaviour
    {
        [SerializeField] private CarryBlockJamSimpleBoard board;
        [SerializeField] private CarryBlockJamPrefabSettings prefabSettings;
        [SerializeField] private Transform runtimeRoot;
        [SerializeField] private Transform piecesRoot;
        [SerializeField] private Transform exitsVisualRoot;

        public CarryBlockJamSimpleBoard Board => board;
        public CarryBlockJamPrefabSettings PrefabSettings => prefabSettings;
        public Transform RuntimeRoot => runtimeRoot;
        public Transform PiecesRoot => piecesRoot;
        public Transform ExitsVisualRoot => exitsVisualRoot;

        private void Awake()
        {
            if (board == null)
                board = GetComponent<CarryBlockJamSimpleBoard>();

            EnsureRoots();
        }

        public void ApplyLevel(LevelData levelData)
        {
            if (board == null || levelData == null)
                return;

            board.ApplyLevelData(levelData);
            EnsureRoots();
        }

        private void EnsureRoots()
        {
            if (runtimeRoot == null)
                runtimeRoot = EnsureChild("Runtime");

            if (piecesRoot == null)
                piecesRoot = EnsureChild("Pieces", runtimeRoot);

            if (exitsVisualRoot == null)
                exitsVisualRoot = EnsureChild("Exits", runtimeRoot);
        }

        private Transform EnsureChild(string name) => EnsureChild(name, transform);

        private static Transform EnsureChild(string name, Transform parent)
        {
            if (parent == null)
                return null;

            string[] parts = name.Split('/');
            Transform current = parent;
            for (int i = 0; i < parts.Length; i++)
            {
                Transform child = current.Find(parts[i]);
                if (child == null)
                {
                    var childObject = new GameObject(parts[i]);
                    child = childObject.transform;
                    child.SetParent(current, false);
                }

                current = child;
            }

            return current;
        }
    }
}
