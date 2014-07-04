using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
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

        public virtual string Name
        {
            get
            {
                return GetType().FullName;
            }
        }

        public Task<GameMove> CreateGetMoveTask(GetMoveRequest request)
        {
            #region Argument Check

            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (request.Board.ActiveColor != this.Color)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The board's active color '{0}' is inconsistent with the player's color '{1}'.",
                        request.Board.ActiveColor,
                        this.Color),
                    "request");
            }

            if (request.Board.ValidMoves.Count == 0)
            {
                throw new ArgumentException("There are no valid moves.", "request");
            }

            #endregion

            var result = new Task<GameMove>(() => DoGetMove(request), request.CancellationToken);
            OnGetMoveTaskCreated(result, request.CancellationToken);
            return result;
        }

        #endregion

        #region Protected Methods

        [NotNull]
        protected abstract GameMove DoGetMove([NotNull] GetMoveRequest request);

        protected virtual void OnGetMoveTaskCreated(
            [NotNull] Task<GameMove> getMoveTask,
            CancellationToken cancellationToken)
        {
            // Nothing to do; for overriding only
        }

        #endregion
    }
}