using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct PieceAttackInfo
    {
        #region Constants and Fields

        private readonly Bitboard _bitboard;
        private readonly bool _isDirectAttack;

        #endregion

        #region Constructors

        internal PieceAttackInfo(ICollection<Position> positions, bool isDirectAttack)
        {
            #region Argument Check

            if (positions == null)
            {
                throw new ArgumentNullException("positions");
            }

            if (positions.Count == 0)
            {
                throw new ArgumentException("No positions.", "positions");
            }

            #endregion

            _bitboard = ChessHelper.GetBitboard(positions);
            _isDirectAttack = isDirectAttack;
        }

        #endregion

        #region Public Properties

        public Bitboard Bitboard
        {
            [DebuggerStepThrough]
            get
            {
                return _bitboard;
            }
        }

        public bool IsDirectAttack
        {
            [DebuggerStepThrough]
            get
            {
                return _isDirectAttack;
            }
        }

        #endregion
    }
}