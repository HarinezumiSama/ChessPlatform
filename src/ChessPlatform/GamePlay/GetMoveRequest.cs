using System;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public sealed class GetMoveRequest
    {
        public GetMoveRequest(
            [NotNull] GameBoard board,
            CancellationToken cancellationToken,
            [NotNull] IGameControl gameControl)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            CancellationToken = cancellationToken;
            GameControl = gameControl ?? throw new ArgumentNullException(nameof(gameControl));
        }

        [NotNull]
        public GameBoard Board
        {
            get;
        }

        public CancellationToken CancellationToken
        {
            get;
        }

        [NotNull]
        public IGameControl GameControl
        {
            get;
        }
    }
}