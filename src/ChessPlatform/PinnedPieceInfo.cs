using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform
{
    internal struct PinnedPieceInfo
    {
        #region Constants and Fields

        private readonly Position _position;
        private readonly Bitboard _allowedMoves;

        #endregion

        #region Constructors

        internal PinnedPieceInfo(Position position, Bitboard allowedMoves)
        {
            _position = position;
            _allowedMoves = allowedMoves;
        }

        #endregion

        #region Public Properties

        public Position Position
        {
            [DebuggerStepThrough]
            get
            {
                return _position;
            }
        }

        public Bitboard AllowedMoves
        {
            [DebuggerStepThrough]
            get
            {
                return _allowedMoves;
            }
        }

        #endregion
    }
}