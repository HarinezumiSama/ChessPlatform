using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChessPlatform.UI.Desktop
{
    internal sealed class DummyComputerChessPlayer : ChessPlayerBase
    {
        #region Constructors

        public DummyComputerChessPlayer(PieceColor color)
            : base(color)
        {
            // Nothing to do
        }

        #endregion

        #region Protected Methods

        protected override Task<PieceMove> DoGetMove(IGameBoard board, CancellationToken cancellationToken)
        {
            var result = new Task<PieceMove>(
                () =>
                    board
                        .ValidMoves
                        .OrderBy(move => move.From.X88Value)
                        .ThenBy(move => move.To.X88Value)
                        .ThenBy(move => move.PromotionResult)
                        .First());

            return result;
        }

        #endregion
    }
}