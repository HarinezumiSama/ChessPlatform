﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class BoardHelper
    {
        #region Constants and Fields

        private long _localMoveCount;
        private long _totalMoveCount;

        #endregion

        #region Public Properties

        public long LocalMoveCount => _localMoveCount;

        public long TotalMoveCount => _totalMoveCount;

        #endregion

        #region Public Methods

        public GameBoard MakeMove([NotNull] GameBoard board, [NotNull] GameMove move)
        {
            var result = MakeMoveInternal(board, move);
            return result;
        }

        public GameBoard MakeNullMove([NotNull] GameBoard board)
        {
            var result = MakeMoveInternal(board, null);
            return result;
        }

        public void ResetLocalMoveCount()
        {
            _localMoveCount = 0;
        }

        #endregion

        #region Private Methods

        private GameBoard MakeMoveInternal([NotNull] GameBoard board, [CanBeNull] GameMove move)
        {
            Interlocked.Increment(ref _localMoveCount);
            Interlocked.Increment(ref _totalMoveCount);
            return move == null ? board.MakeNullMove() : board.MakeMove(move);
        }

        #endregion
    }
}