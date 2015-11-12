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
            Color = color.EnsureDefined();
        }

        #endregion

        #region IChessPlayer Members

        public event EventHandler<ChessPlayerFeedbackEventArgs> FeedbackProvided;

        public PieceColor Color
        {
            get;
        }

        public virtual string Name => GetType().FullName;

        public Task<PrincipalVariationInfo> CreateGetMoveTask(GetMoveRequest request)
        {
            #region Argument Check

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Board.ActiveColor != Color)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The board's active color '{0}' is inconsistent with the player's color '{1}'.",
                        request.Board.ActiveColor,
                        Color),
                    nameof(request));
            }

            if (request.Board.ValidMoves.Count == 0)
            {
                throw new ArgumentException("There are no valid moves.", nameof(request));
            }

            #endregion

            var result = new Task<PrincipalVariationInfo>(() => DoGetMove(request), request.CancellationToken);
            OnGetMoveTaskCreated(result, request.CancellationToken);
            return result;
        }

        #endregion

        #region Protected Methods

        protected void OnFeedbackProvided(ChessPlayerFeedbackEventArgs args)
        {
            FeedbackProvided?.Invoke(this, args);
        }

        [NotNull]
        protected abstract PrincipalVariationInfo DoGetMove([NotNull] GetMoveRequest request);

        protected virtual void OnGetMoveTaskCreated(
            [NotNull] Task<PrincipalVariationInfo> getMoveTask,
            CancellationToken cancellationToken)
        {
            // Nothing to do; for overriding only
        }

        #endregion
    }
}