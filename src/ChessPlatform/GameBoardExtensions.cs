using System;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class GameBoardExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNonPawnMaterial([NotNull] this GameBoard board, GameSide side)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var nonPawnBitboard = board.GetBitboard(side)
                & ~board.GetBitboard(side.ToPiece(PieceType.King))
                & ~board.GetBitboard(side.ToPiece(PieceType.Pawn));

            return nonPawnBitboard.IsAny;
        }
    }
}