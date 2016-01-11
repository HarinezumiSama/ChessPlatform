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
        public static bool HasNonPawnMaterial([NotNull] this GameBoard board, GameSide side)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            var nonPawnBitboard = board.GetBitboard(side)
                & ~board.GetBitboard(side.ToPiece(PieceType.King))
                & ~board.GetBitboard(side.ToPiece(PieceType.Pawn));

            return nonPawnBitboard.IsAny;
        }

        #endregion
    }
}