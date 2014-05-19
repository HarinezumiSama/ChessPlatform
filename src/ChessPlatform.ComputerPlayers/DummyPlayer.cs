using System;
using System.Linq;
using System.Threading;

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

        protected override PieceMove DoGetMove(IGameBoard board, CancellationToken cancellationToken)
        {
            var result = board
                .ValidMoves
                .OrderBy(move => move.From.X88Value)
                .ThenBy(move => move.To.X88Value)
                .ThenBy(move => move.PromotionResult)
                .First();

            return result;
        }

        #endregion
    }
}