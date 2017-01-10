using System.Diagnostics;

namespace ChessPlatform.Internal
{
    internal struct InternalCastlingInfo2
    {
        #region Constructors

        public InternalCastlingInfo2(GameMove2 kingMove, Bitboard expectedEmptySquares)
        {
            KingMove = kingMove;
            ExpectedEmptySquares = expectedEmptySquares;
        }

        #endregion

        #region Public Properties

        public GameMove2 KingMove
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