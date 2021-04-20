using System;
using System.Linq;
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
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (gameControl == null)
            {
                throw new ArgumentNullException(nameof(gameControl));
            }

            Board = board;
            CancellationToken = cancellationToken;
            GameControl = gameControl;
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