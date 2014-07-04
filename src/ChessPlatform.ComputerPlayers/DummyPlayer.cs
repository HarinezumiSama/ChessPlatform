using System;
using System.Linq;
using System.Threading;
using ChessPlatform.GamePlay;

namespace ChessPlatform.ComputerPlayers
{
    public sealed class DummyPlayer : ChessPlayerBase
    {
        #region Constructors

        public DummyPlayer(PieceColor color)
            : base(color)
        {
            // Nothing to do
        }

        #endregion

        #region Protected Methods

        protected override GameMove DoGetMove(GetMoveRequest request)
        {
            var result = request.Board
                .ValidMoves
                .Keys
                .OrderBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ThenBy(move => move.PromotionResult)
                .First();

            return result;
        }

        #endregion
    }
}