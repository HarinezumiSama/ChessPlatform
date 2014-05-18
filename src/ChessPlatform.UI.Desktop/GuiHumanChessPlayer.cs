﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal sealed class GuiHumanChessPlayer : IChessPlayer
    {
        #region Constants and Fields

        private readonly object _syncLock = new object();
        private bool _isAwaitingMove;
        private PieceMove _move;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GuiHumanChessPlayer"/> class.
        /// </summary>
        internal GuiHumanChessPlayer(PieceColor color)
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

        public PieceMove GetMove(IGameBoard board)
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

            while (true)
            {
                lock (_syncLock)
                {
                    var move = _move;
                    if (move != null)
                    {
                        _move = null;
                        return move;
                    }

                    _isAwaitingMove = true;
                }

                Thread.Sleep(10);
            }
        }

        #endregion

        #region Internal Methods

        internal void ApplyMove([NotNull] PieceMove move)
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
            }
        }

        #endregion
    }
}