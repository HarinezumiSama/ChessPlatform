using System;
using System.Threading;
using System.Threading.Tasks;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal sealed class GuiHumanChessPlayer : ChessPlayerBase
    {
        private readonly object _syncLock = new object();
        private bool _isAwaitingMove;
        private GameMove _move;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GuiHumanChessPlayer"/> class.
        /// </summary>
        internal GuiHumanChessPlayer(GameSide side)
            : base(side)
        {
            // Nothing to do
        }

        public event EventHandler MoveRequested;

        public event EventHandler MoveRequestCancelled;

        public override string Name => "Human Player";

        public override string ToString()
        {
            return $@"{{ {GetType().GetQualifiedName()} : {Side.GetName()} }}";
        }

        internal void ApplyMove([NotNull] GameMove move)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            lock (_syncLock)
            {
                if (!_isAwaitingMove)
                {
                    throw new InvalidOperationException("Not waiting for a move.");
                }

                _move = move;
                _isAwaitingMove = false;
            }
        }

        protected override VariationLine DoGetMove(GetMoveRequest request)
        {
            while (true)
            {
                request.CancellationToken.ThrowIfCancellationRequested();

                lock (_syncLock)
                {
                    var move = _move;
                    if (move != null)
                    {
                        _move = null;
                        return move | VariationLine.Zero;
                    }

                    if (!_isAwaitingMove)
                    {
                        _isAwaitingMove = true;
                        RaiseMoveRequestedAsync();
                    }
                }

                Thread.Sleep(10);
            }
        }

        protected override void OnGetMoveTaskCreated(
            Task<VariationLine> getMoveTask,
            CancellationToken cancellationToken)
        {
            base.OnGetMoveTaskCreated(getMoveTask, cancellationToken);

            getMoveTask.ContinueWith(
                t =>
                {
                    _isAwaitingMove = false;
                    RaiseMoveRequestCancelledAsync();
                },
                TaskContinuationOptions.OnlyOnCanceled);
        }

        private void RaiseMoveRequestedAsync()
        {
            var handler = MoveRequested;
            if (handler == null)
            {
                return;
            }

            Task.Factory.StartNew(() => handler(this, EventArgs.Empty));
        }

        private void RaiseMoveRequestCancelledAsync()
        {
            var handler = MoveRequestCancelled;
            if (handler == null)
            {
                return;
            }

            Task.Factory.StartNew(() => handler(this, EventArgs.Empty));
        }
    }
}