using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class GamePositionExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyUnderAttack(
            [NotNull] this GamePosition gamePosition,
            [NotNull] ICollection<Square> targetSquares,
            GameSide attackingSide)
        {
            if (gamePosition == null)
            {
                throw new ArgumentNullException(nameof(gamePosition));
            }

            if (targetSquares == null)
            {
                throw new ArgumentNullException(nameof(targetSquares));
            }

            var result = targetSquares.Count != 0
                && targetSquares.Any(targetSquare => gamePosition.IsUnderAttack(targetSquare, attackingSide));

            return result;
        }

        public static bool IsInCheck([NotNull] this GamePosition gamePosition, GameSide side)
        {
            if (gamePosition == null)
            {
                throw new ArgumentNullException(nameof(gamePosition));
            }

            var king = side.ToPiece(PieceType.King);
            var attackingSide = side.Invert();
            var kingSquares = gamePosition.PiecePosition[king].GetSquares();
            return gamePosition.IsAnyUnderAttack(kingSquares, attackingSide);
        }

        #endregion
    }
}