using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal struct InternalCastlingInfo
    {
        #region Constructors

        public InternalCastlingInfo([NotNull] GameMove kingMove, Bitboard expectedEmptySquares)
        {
            if (kingMove == null)
            {
                throw new ArgumentNullException(nameof(kingMove));
            }

            KingMove = kingMove;
            ExpectedEmptySquares = expectedEmptySquares;
        }

        #endregion

        #region Public Properties

        public GameMove KingMove
        {
            [DebuggerStepThrough]
            get;
        }

        public Bitboard ExpectedEmptySquares
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion
    }
}