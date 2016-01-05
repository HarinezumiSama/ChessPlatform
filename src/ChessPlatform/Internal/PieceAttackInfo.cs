using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct PieceAttackInfo
    {
        #region Constructors

        internal PieceAttackInfo(ICollection<Position> positions, bool isDirectAttack)
        {
            #region Argument Check

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (positions.Count == 0)
            {
                throw new ArgumentException("No positions specified.", nameof(positions));
            }

            #endregion

            Bitboard = new Bitboard(positions);
            IsDirectAttack = isDirectAttack;
        }

        #endregion

        #region Public Properties

        public Bitboard Bitboard
        {
            [DebuggerStepThrough]
            get;
        }

        public bool IsDirectAttack
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion
    }
}