using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// GamePiece'e tıklanınca onu WaitingAreas'taki ilk boş slota zıplatır.
    ///
    /// Flow field tarzı erişim kontrolü:
    ///  - Grid'in ön kenarından (en yüksek row) BFS başlatılır
    ///  - Sadece boş veya wall (None) hücrelerden geçer
    ///  - Bir parça tıklanabilir = ön kenar veya BFS ile ulaşılan boş bir hücreye komşu
    ///
    /// Row/col bilgisi GameObject adından okunur ("Cell_{row}_{col}").
    /// Aynı GameObject'te Collider gereklidir (OnMouseDown için).
    /// </summary>
    [DisallowMultipleComponent]
    public class PieceJumpEffect : MonoBehaviour
    {
        [Header("Jump")]
        [Tooltip("DOJump güç değeri — ne kadar yüksek bir yay çizsin.")]
        public float jumpPower = 1.5f;

        [Tooltip("Zıplama animasyonunun toplam süresi.")]
        public float jumpDuration = 0.45f;

        [Tooltip("Zıplama easing tipi.")]
        public Ease ease = Ease.OutQuad;

        [Tooltip("Tıklama → zıplama anında çalınacak ses adı. Boş bırakılırsa ses çalmaz.")]
        public string jumpSound = "Jump";

        // ── State ─────────────────────────────────────────────────────────────────────

        private GamePiece  _piece;
        private PuzzleGrid _grid;
        private SlideLane  _lane;   // parent SlideLane varsa SlideLane modu, yoksa Grid modu
        private bool       _sent;

        /// <summary>Parça WaitingArea'ya gönderildi mi.</summary>
        public bool IsSent => _sent;

        private void Awake()
        {
            _piece = GetComponent<GamePiece>();
            _lane  = GetComponentInParent<SlideLane>();
        }

        private void Start()
        {
            if (_lane != null) return; // SlideLane modunda grid'e ihtiyaç yok

            if (LevelBase.Instance != null)
                _grid = LevelBase.Instance.GetComponent<PuzzleGrid>();
            if (_grid == null)
                _grid = FindObjectOfType<PuzzleGrid>();
        }

        // ── Click ─────────────────────────────────────────────────────────────────────

        private void OnMouseDown()
        {
            if (_sent) return;
            if (_piece == null) return;
            if (WaitingAreas.Instance == null) return;

            if (!IsClickable()) return;

            WaitingArea slot = WaitingAreas.Instance.GetNextFreeSlot();
            if (slot == null) return;
            if (!slot.TryReserve(_piece)) return;

            _sent = true;

            // SlideLane modunda parçayı lane'den ��ıkar (arkadakiler öne kayar).
            if (_lane != null)
                _lane.Remove(transform);

            // Bu zıplamadan etkilenen tüm donmuş piece'lerin buzunu 1 azalt.
            GamePiece.DamageAllFrozen(exclude: _piece);

            // Eğer bu piece bir tunnel'dan ��ıkmışsa, tunnel'a haber ver → sıradakini spawn et.
            if (_piece != null && _piece.sourceTunnel != null)
            {
                TunnelSpawner src = _piece.sourceTunnel;
                _piece.sourceTunnel = null; // bu piece artık queue dışı
                src.SpawnNextPiece();
            }

            // Tutorial varsa bir sonraki stage'e geç.
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
            {
                if (TryGetCellRowCol(out int tRow, out int tCol))
                    TutorialManager.Instance.NotifyCellSent(tRow, tCol);
            }

            // Jump sesi (PlayOneShot — üst üste tıklamalarda kesintisiz)
            if (!string.IsNullOrEmpty(jumpSound) && SoundManager.instance != null)
                SoundManager.instance.PlayOneShot(jumpSound);

            transform
                .DOJump(slot.transform.position, jumpPower, 1, jumpDuration)
                .SetEase(ease)
                .OnComplete(() => transform.position = slot.transform.position);
        }

        // ── Clickability ──────────────────────────────────────────────────────────────

        private bool IsClickable()
        {
            // Ice ile donmuş piece tıklanamaz.
            if (_piece != null && _piece.IsFrozen)
                return false;

            // Tutorial aktifse: clickableCells listesi kararı verir, flow field/lane atlanır.
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
            {
                if (TryGetCellRowCol(out int tRow, out int tCol))
                    return TutorialManager.Instance.IsCellClickable(tRow, tCol);
                // Grid cell adı parse edilemiyorsa (örn. SlideLane): SlideLane'in kendi kuralı.
                if (_lane != null) return _lane.IsFront(transform);
                return true;
            }

            // SlideLane modu: sadece lane'in front'undaki parça tıklanabilir.
            if (_lane != null)
                return _lane.IsFront(transform);

            if (_grid == null || !_grid.IsBuilt)
                return true; // grid yoksa kısıtlama yok

            if (!TryGetCellRowCol(out int myRow, out int myCol))
                return false;

            int rows = _grid.Rows;
            int cols = _grid.Columns;
            // Editör [0,0] top-left konvansiyonu: row 0 = top = front.
            int frontRow = 0;

            // Front row'daki parça her zaman tıklanabilir (doğrudan exit).
            if (myRow == frontRow) return true;

            // BFS: front row'dan başlayıp boş hücreler üzerinden yayıl.
            bool[,] reachable = new bool[rows, cols];
            var queue = new Queue<(int r, int c)>();

            for (int c = 0; c < cols; c++)
            {
                if (IsCellEmpty(frontRow, c))
                {
                    reachable[frontRow, c] = true;
                    queue.Enqueue((frontRow, c));
                }
            }

            int[] dr = { -1, 1,  0, 0 };
            int[] dc = {  0, 0, -1, 1 };

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nr = r + dr[i];
                    int nc = c + dc[i];
                    if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                    if (reachable[nr, nc]) continue;
                    if (!IsCellEmpty(nr, nc)) continue;
                    reachable[nr, nc] = true;
                    queue.Enqueue((nr, nc));
                }
            }

            // Komşularımdan biri reachable empty ise tıklanabilirim.
            for (int i = 0; i < 4; i++)
            {
                int nr = myRow + dr[i];
                int nc = myCol + dc[i];
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                if (reachable[nr, nc]) return true;
            }

            return false;
        }

        private bool IsCellEmpty(int row, int col)
        {
            if (!_grid.TryGetCell(row, col, out PuzzleCell cell))
                return false; // bulunamayan/null cell = bariyer
            if (cell == null) return false;
            if (cell.IsWall) return false; // wall (None) hücreleri bariyer
            if (cell.Slot == null) return true;

            // Slotun üzerindeki parça gönderilmişse boş sayılır.
            PieceJumpEffect pje = cell.Slot.GetComponent<PieceJumpEffect>();
            return pje != null && pje._sent;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        // GameObject adı PuzzleBoardLayout tarafından "Cell_{row}_{col}" olarak atanır.
        private bool TryGetCellRowCol(out int row, out int col)
        {
            row = -1; col = -1;
            string n = gameObject.name;
            const string prefix = "Cell_";
            if (!n.StartsWith(prefix)) return false;

            int sep = n.IndexOf('_', prefix.Length);
            if (sep <= prefix.Length) return false;

            return int.TryParse(n.Substring(prefix.Length, sep - prefix.Length), out row) &&
                   int.TryParse(n.Substring(sep + 1), out col);
        }
    }
}
