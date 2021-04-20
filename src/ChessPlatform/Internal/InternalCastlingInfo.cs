using System;
using System.Diagnostics;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal struct InternalCastlingInfo
    {
        public InternalCastlingInfo([NotNull] GameMove kingMove, Bitboard expectedEmptySquares)
        {
            KingMove = kingMove ?? throw new ArgumentNullException(nameof(kingMove));
            ExpectedEmptySquares = expectedEmptySquares;
        }

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
    }
}