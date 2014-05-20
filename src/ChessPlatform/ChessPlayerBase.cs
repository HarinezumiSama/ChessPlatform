using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public abstract class ChessPlayerBase : IChessPlayer
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        protected ChessPlayerBase(PieceColor color)
        {
            this.Color = color.EnsureDefined();
        }

        #endregion

        #region IChessPlayer Members

        public PieceColor Color
        {
            get;
            private set;
        }

        public Task<PieceMove> GetMove(IGameBoard board, CancellationToken cancellationToken)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            if (board.ActiveColor != this.Color)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The board's active color '{0}' is inconsistent with the player's color '{1}'.",
                        board.ActiveColor,
                        this.Color),
                    "board");
            }

            #endregion

            var result = new Task<PieceMove>(() => DoGetMove(board, cancellationToken), cancellationToken);
            OnGetMoveTaskCreated(result, cancellationToken);
            return result;
        }

        #endregion

        #region Protected Methods

        [NotNull]
        protected abstract PieceMove DoGetMove([NotNull] IGameBoard board, CancellationToken cancellationToken);

        protected virtual void OnGetMoveTaskCreated(
            [NotNull] Task<PieceMove> getMoveTask,
            CancellationToken cancellationToken)
        {
            // Nothing to do; for overriding only
        }

        #endregion
    }
}