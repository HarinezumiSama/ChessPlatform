﻿using System;
using System.Diagnostics;
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
        internal GuiHumanChessPlayer(GameSide side)
            : base(side)
        {
            // Nothing to do
        }

        #endregion

        #region Events

        public event EventHandler MoveRequested;

        public event EventHandler MoveRequestCancelled;

        #endregion

        #region Public Properties

        public override string Name => "Human Player";

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{{ {GetType().GetQualifiedName()} : {Side.GetName()} }}";
        }

        #endregion

        #region Internal Methods

        internal void ApplyMove([NotNull] GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
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

        #region Protected Methods

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

        #endregion

        #region Private Methods

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

        #endregion
    }
}