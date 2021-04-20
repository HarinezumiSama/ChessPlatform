using System.Diagnostics;

namespace ChessPlatform.Internal
{
    internal readonly struct InternalCastlingInfo2
    {
        public InternalCastlingInfo2(GameMove2 kingMove, Bitboard expectedEmptySquares)
        {
            KingMove = kingMove;
            ExpectedEmptySquares = expectedEmptySquares;
        }

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
    }
}