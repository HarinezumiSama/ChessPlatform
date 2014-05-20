using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class GameBoardExtensions
    {
        #region Public Methods

        public static int GetPieceCount([NotNull] this IGameBoard board, Piece piece)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            return board.GetPiecePositions(piece).Length;
        }

        #endregion
    }
}