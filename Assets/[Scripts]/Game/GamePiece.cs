using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GAITemplate
{
    [DisallowMultipleComponent]
    public class GamePiece : MonoBehaviour
    {
        // ── Static registry (sahnedeki tüm aktif piece'ler) ──────────────────────────

        private static readonly HashSet<GamePiece> _all = new HashSet<GamePiece>();

        private void OnEnable()  => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>Sahnedeki tüm donmuş piece'lerin ice count'unu 1 azaltır.</summary>
        public static void DamageAllFrozen(GamePiece exclude = null)
        {
            foreach (GamePiece p in _all)
            {
                if (p == null || p == exclude) continue;
                if (p.IsFrozen) p.DamageIce();
            }
        }

        [SerializeField] private PieceColorType colorType = PieceColorType.None;
        [SerializeField] private Renderer colorRenderer;

        [Header("Flag Overlays")]
        [Tooltip("Hidden flag set olunca aktif olur (piece üstüne ?/fog gibi overlay).")]
        public GameObject hiddenOverlay;

        [Tooltip("Ice flag set olunca aktif olur (piece üstüne buz blok overlay).")]
        public GameObject iceOverlay;

        [Tooltip("Ice count'u gösteren TMP text (iceOverlay'in child'ı). Boş olabilir.")]
        public TMP_Text iceCountText;

        public PieceColorType ColorType => colorType;
        public bool IsHidden { get; private set; }
        public int IceCount  { get; private set; }
        public bool IsFrozen => IceCount > 0;

        /// <summary>Bu piece bir tunnel'dan ��ıktıysa kaynak spawner. Gönderilince yenisi spawn olur.</summary>
        [System.NonSerialized] public TunnelSpawner sourceTunnel;

        private void Awake()
        {
            if (colorRenderer == null)
                colorRenderer = GetComponentInChildren<Renderer>();
        }

        // ── Color ─────────────────────────────────────────────────────────────────────

        public void ApplyColor(PieceColorType type)
        {
            colorType = type;
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (!PieceColorPalette.IsPaintable(colorType))
                return;

            if (colorRenderer == null)
                colorRenderer = GetComponentInChildren<Renderer>();

            if (colorRenderer == null)
                return;

            Material material = PieceColorPalette.GetMaterial(colorType);
            if (material == null)
                return;

            colorRenderer.sharedMaterial = material;
        }

        // ── Flag setters ──────────────────────────────────────────────────────────────

        public void ApplyHidden(bool hidden)
        {
            IsHidden = hidden;
            if (hiddenOverlay != null && hiddenOverlay.activeSelf != hidden)
                hiddenOverlay.SetActive(hidden);
        }

        public void ApplyIce(int count)
        {
            IceCount = Mathf.Max(0, count);
            bool show = IceCount > 0;

            if (iceOverlay != null && iceOverlay.activeSelf != show)
                iceOverlay.SetActive(show);

            if (iceCountText != null && show)
                iceCountText.text = IceCount.ToString();
        }

        /// <summary>Buzdan bir vuruş düşürür. 0'a inince buz kırılır.</summary>
        public void DamageIce(int amount = 1)
        {
            if (IceCount <= 0) return;
            ApplyIce(IceCount - Mathf.Max(1, amount));
        }

        /// <summary>LevelData'dan okunan flag'leri tek seferde uygular.</summary>
        public void ApplyFlags(LevelCellFlag flags, int flagValue)
        {
            ApplyHidden((flags & LevelCellFlag.Hidden) == LevelCellFlag.Hidden);

            bool hasIce = (flags & LevelCellFlag.Ice) == LevelCellFlag.Ice;
            ApplyIce(hasIce ? Mathf.Max(1, flagValue) : 0);
        }
    }
}
