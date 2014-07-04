using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal sealed class GuiHumanChessPlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private readonly object _syncLock = new object();
        private bool _isAwaitingMove;
        private GameMove _move;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GuiHumanChessPlayer"/> class.
        /// </summary>
        internal GuiHumanChessPlayer(PieceColor color)
            : base(color)
        {
            // Nothing to do
        }

        #endregion

        #region Events

        public event EventHandler MoveRequested;

        public event EventHandler MoveRequestCancelled;

        #endregion

        #region Public Properties

        public override string Name
        {
            get
            {
                return "Human Player";
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ {0} : {1} }}",
                GetType().GetQualifiedName(),
                this.Color.GetName());
        }

        #endregion

        #region Protected Methods

        protected override GameMove DoGetMove(GetMoveRequest request)
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
                        return move;
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

        protected override void OnGetMoveTaskCreated(Task<GameMove> getMoveTask, CancellationToken cancellationToken)
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

        #endregion

        #region Internal Methods

        internal void ApplyMove([NotNull] GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

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

        #endregion

        #region Private Methods

        private void RaiseMoveRequestedAsync()
        {
            var handler = this.MoveRequested;
            if (handler == null)
            {
                return;
            }

            Task.Factory.StartNew(() => handler(this, EventArgs.Empty));
        }

        private void RaiseMoveRequestCancelledAsync()
        {
            var handler = this.MoveRequestCancelled;
            if (handler == null)
            {
                return;
            }

            Task.Factory.StartNew(() => handler(this, EventArgs.Empty));
        }

        #endregion
    }
}