using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform
{
    internal struct PositionBridge
    {
        #region Constants and Fields

        private readonly long _bitboard;

        #endregion

        #region Constructors

        internal PositionBridge(IEnumerable<Position> positions)
        {
            #region Argument Check

            if (positions == null)
            {
                throw new ArgumentNullException("positions");
            }

            #endregion

            _bitboard = ChessHelper.GetBitboard(positions);
        }

        #endregion

        #region Public Properties

        public long Bitboard
        {
            [DebuggerStepThrough]
            get
            {
                return _bitboard;
            }
        }

        #endregion
    }
}