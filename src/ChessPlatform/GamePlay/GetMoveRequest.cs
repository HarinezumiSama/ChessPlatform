using System;
using System.Linq;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public sealed class GetMoveRequest
    {
        #region Constructors

        public GetMoveRequest([NotNull] GameBoard board, CancellationToken cancellationToken)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            this.Board = board;
            this.CancellationToken = cancellationToken;
        }

        #endregion

        #region Public Properties

        [NotNull]
        public GameBoard Board
        {
            get;
            private set;
        }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        #endregion
    }
}