using System;
using System.Threading;
using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public abstract class ChessPlayerBase : IChessPlayer
    {
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

        public event EventHandler<ChessPlayerFeedbackEventArgs> FeedbackProvided;

        public GameSide Side
        {
            get;
        }

        public virtual string Name => GetType().FullName;

        public Task<VariationLine> CreateGetMoveTask(GetMoveRequest request)
        {
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

            var result = new Task<VariationLine>(() => DoGetMove(request), request.CancellationToken);
            OnGetMoveTaskCreated(result, request.CancellationToken);
            return result;
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}