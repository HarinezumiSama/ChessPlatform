using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public abstract class ChessPlayerBase : IChessPlayer
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        protected ChessPlayerBase(GameSide side)
        {
            Side = side.EnsureDefined();
        }

        ~ChessPlayerBase()
        {
            Dispose(false);
        }

        #endregion

        #region IChessPlayer Members

        public event EventHandler<ChessPlayerFeedbackEventArgs> FeedbackProvided;

        public GameSide Side
        {
            get;
        }

        public virtual string Name => GetType().FullName;

        public Task<VariationLine> CreateGetMoveTask(GetMoveRequest request)
        {
            #region Argument Check

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Board.ActiveSide != Side)
            {
                throw new ArgumentException(
                    $@"The board's active side '{request.Board.ActiveSide
                        }' is inconsistent with the player's side '{Side}'.",
                    nameof(request));
            }

            if (request.Board.ValidMoves.Count == 0)
            {
                throw new ArgumentException("There are no valid moves.", nameof(request));
            }

            #endregion

            var result = new Task<VariationLine>(() => DoGetMove(request), request.CancellationToken);
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
        protected abstract VariationLine DoGetMove([NotNull] GetMoveRequest request);

        protected virtual void OnGetMoveTaskCreated(
            [NotNull] Task<VariationLine> getMoveTask,
            CancellationToken cancellationToken)
        {
            // Nothing to do; for overriding only
        }

        protected virtual void Dispose(bool explicitDisposing)
        {
            // Nothing to do
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}