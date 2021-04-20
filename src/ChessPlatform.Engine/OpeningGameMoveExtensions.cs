using System;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public static class OpeningGameMoveExtensions
    {
        public static string ToStandardAlgebraicNotation(
            [NotNull] this OpeningGameMove move,
            [NotNull] GameBoard board)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var notation = move.Move.ToStandardAlgebraicNotation(board);
            return $@"{notation} (W:{move.Weight})";
        }
    }
}