using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public static class OpeningGameMoveExtensions
    {
        #region Public Methods

        public static string ToStandardAlgebraicNotation(
            [NotNull] this OpeningGameMove move,
            [NotNull] GameBoard board)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            var notation = move.Move.ToStandardAlgebraicNotation(board);
            return $@"{notation} (W:{move.Weight})";
        }

        #endregion
    }
}