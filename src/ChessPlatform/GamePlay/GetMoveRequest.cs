using System;
using System.Linq;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public sealed class GetMoveRequest
    {
        #region Constructors

        public GetMoveRequest(
            [NotNull] GameBoard board,
            CancellationToken cancellationToken,
            [NotNull] IGameControl gameControl)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (gameControl == null)
            {
                throw new ArgumentNullException(nameof(gameControl));
            }

            #endregion

            Board = board;
            CancellationToken = cancellationToken;
            GameControl = gameControl;
        }

        #endregion

        #region Public Properties

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

        #endregion
    }
}