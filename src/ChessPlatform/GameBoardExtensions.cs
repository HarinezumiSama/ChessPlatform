using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class GameBoardExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNonPawnMaterial([NotNull] this GameBoard board, PieceColor color)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            var nonPawnBitboard = board.GetBitboard(color)
                & ~board.GetBitboard(PieceType.King.ToPiece(color))
                & ~board.GetBitboard(PieceType.Pawn.ToPiece(color));

            return nonPawnBitboard.IsAny;
        }

        #endregion
    }
}