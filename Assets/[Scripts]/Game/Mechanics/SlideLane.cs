using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Tek bir slide lane. Önden arkaya sıralı pieces tutar.
    /// PopFront() öndeki parçayı ��ıkarır ve arkadakileri öne kaydırır (DOTween).
    /// </summary>
    [DisallowMultipleComponent]
    public class SlideLane : MonoBehaviour
    {
        [Tooltip("Slot'lar arası z mesafesi (BuildSlideLaneBoard tarafından set edilir).")]
        public float depthSpacing = 0.8f;

        [Tooltip("Slide forward animasyonunun süresi.")]
        public float slideDuration = 0.25f;

        [Tooltip("Slide forward easing.")]
        public Ease slideEase = Ease.OutQuad;

        // Front (slot 0) at the head, back (slot depth-1) at the tail.
        private readonly List<Transform> _pieces = new List<Transform>();

        public int Count => _pieces.Count;

        public Transform Front => _pieces.Count > 0 ? _pieces[0] : null;

        /// <summary>Verilen parça lane'in front'unda mı.</summary>
        public bool IsFront(Transform piece) =>
            _pieces.Count > 0 && _pieces[0] == piece;

        /// <summary>Board tarafından spawn sırasında çağrılır (front'tan back'e doğru).</summary>
        public void RegisterPiece(Transform piece)
        {
            if (piece == null) return;
            _pieces.Add(piece);
        }

        /// <summary>Belirli bir parçayı ��ıkarır (ön olmasa da). Arkadakiler öne kayar.</summary>
        public bool Remove(Transform piece)
        {
            int idx = _pieces.IndexOf(piece);
            if (idx < 0) return false;

            _pieces.RemoveAt(idx);
            SlideForwardFrom(idx);
            return true;
        }

        /// <summary>Front'taki parçayı ��ıkarır ve döndürür. Arkadakiler öne kayar.</summary>
        public Transform PopFront()
        {
            if (_pieces.Count == 0) return null;
            Transform front = _pieces[0];
            _pieces.RemoveAt(0);
            SlideForwardFrom(0);
            return front;
        }

        // Kalan parçaları yeni indekslerine taşı. fromIdx = ��ıkarılan parçanın eski indeksi.
        private void SlideForwardFrom(int fromIdx)
        {
            for (int i = fromIdx; i < _pieces.Count; i++)
            {
                Transform p = _pieces[i];
                if (p == null) continue;

                Vector3 target = new Vector3(0f, p.localPosition.y, -i * depthSpacing);
                p.DOKill();
                p.DOLocalMove(target, slideDuration).SetEase(slideEase);
            }
        }
    }
}
