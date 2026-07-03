using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Sahnedeki "WaitingAreas" objesi. Awake'de slotCount kadar WaitingArea child'ı spawn eder.
    /// </summary>
    [DisallowMultipleComponent]
    public class WaitingAreas : MonoBehaviour
    {
        public static WaitingAreas Instance { get; private set; }

        [Header("Slots")]
        [Tooltip("Her slot için spawn edilecek prefab. Boş bırakılırsa boş GameObject oluşturulur.")]
        public GameObject slotPrefab;

        [Tooltip("Kaç tane WaitingArea slotu oluşturulsun.")]
        [Min(1)]
        public int slotCount = 4;

        [Tooltip("Slotlar arası yatay mesafe.")]
        public float slotSpacing = 1.2f;

        // ─────────────────────────────────────────────────────────────────────────────

        private WaitingArea[] _slots;

        private void Awake()
        {
            Instance = this;
            BuildSlots();
        }

        private void BuildSlots()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _slots = new WaitingArea[slotCount];
            float halfWidth = (slotCount - 1) * slotSpacing * 0.5f;

            for (int i = 0; i < slotCount; i++)
            {
                GameObject go = slotPrefab != null
                    ? Instantiate(slotPrefab, transform)
                    : new GameObject($"WaitingArea_{i}");

                go.name = $"WaitingArea_{i}";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(i * slotSpacing - halfWidth, 0f, 0f);

                _slots[i] = go.GetComponent<WaitingArea>() ?? go.AddComponent<WaitingArea>();
            }
        }

        /// <summary>Sıradaki boş slotu döner. Boş slot yoksa null.</summary>
        public WaitingArea GetNextFreeSlot()
        {
            foreach (WaitingArea slot in _slots)
                if (!slot.IsOccupied) return slot;
            return null;
        }
    }
}
