using UnityEngine;

namespace GAITemplate
{
    /// <summary>
    /// Tek bir WaitingArea slotu. Dolu/boş durumunu tutar.
    /// Animasyon PieceJumpEffect tarafından yönetilir.
    /// </summary>
    public class WaitingArea : MonoBehaviour
    {
        public bool IsOccupied { get; private set; }
        public GamePiece Occupant { get; private set; }

        /// <summary>Slotu verilen parça için rezerve eder. Doluysa false döner.</summary>
        public bool TryReserve(GamePiece piece)
        {
            if (IsOccupied) return false;
            IsOccupied = true;
            Occupant   = piece;
            return true;
        }

        /// <summary>Slotu boşaltır.</summary>
        public void Release()
        {
            IsOccupied = false;
            Occupant   = null;
        }
    }
}
