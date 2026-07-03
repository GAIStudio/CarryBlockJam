using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Tunnel cell'e BuildGrid sonrası eklenir. Queue'daki piece'leri tek tek tunnel
    /// pozisyonunda spawn edip exit cell pozisyonuna DOLocalMove ile getirir.
    /// </summary>
    [DisallowMultipleComponent]
    public class TunnelSpawner : MonoBehaviour
    {
        [Header("Spawn Animation")]
        public float moveDuration = 0.35f;
        public Ease  moveEase     = Ease.OutQuad;

        private readonly Queue<PieceColorType> _queue = new Queue<PieceColorType>();

        private GameObject _piecePrefab;
        private Transform  _exitParent;
        private PuzzleGrid _grid;
        private int        _exitRow;
        private int        _exitCol;
        private Vector3    _tunnelLocalPos;
        private Vector3    _exitLocalPos;

        public int  RemainingCount => _queue.Count;
        public bool HasMore        => _queue.Count > 0;

        public void Init(
            IEnumerable<PieceColorType> queueColors,
            GameObject piecePrefab,
            Transform exitParent,
            PuzzleGrid grid,
            int exitRow,
            int exitCol,
            Vector3 tunnelLocalPos,
            Vector3 exitLocalPos)
        {
            _queue.Clear();
            if (queueColors != null)
                foreach (var c in queueColors)
                    _queue.Enqueue(c);

            _piecePrefab    = piecePrefab;
            _exitParent     = exitParent;
            _grid           = grid;
            _exitRow        = exitRow;
            _exitCol        = exitCol;
            _tunnelLocalPos = tunnelLocalPos;
            _exitLocalPos   = exitLocalPos;
        }

        public PieceColorType PeekNext() =>
            _queue.Count > 0 ? _queue.Peek() : PieceColorType.None;

        /// <summary>
        /// Queue'dan bir piece çıkarır, tunnel pozisyonunda spawn eder ve exit cell'e DOLocalMove yapar.
        /// </summary>
        public Transform SpawnNextPiece()
        {
            if (!HasMore || _piecePrefab == null || _exitParent == null)
                return null;

            PieceColorType color = _queue.Dequeue();

            GameObject pieceGO = Instantiate(_piecePrefab, _exitParent);
            pieceGO.name = $"Cell_{_exitRow}_{_exitCol}";
            pieceGO.transform.localPosition = _tunnelLocalPos;   // tunnel pozisyonunda başla
            pieceGO.transform.localRotation = Quaternion.identity;

            GamePiece gp = pieceGO.GetComponent<GamePiece>();
            if (gp != null)
            {
                gp.ApplyColor(color);
                gp.sourceTunnel = this;
            }

            // Exit cell'e doğru kay.
            pieceGO.transform.DOLocalMove(_exitLocalPos, moveDuration).SetEase(moveEase);

            // Grid'i hemen update et — flow field bu cell'in dolu olduğunu bilir.
            if (_grid != null)
                _grid.RegisterCell(_exitRow, _exitCol, pieceGO.transform, false);

            return pieceGO.transform;
        }
    }
}
