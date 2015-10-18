using System;
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

        private long _moveCount;

        #endregion

        #region Public Properties

        public long MoveCount => _moveCount;

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

        #endregion

        #region Private Methods

        private GameBoard MakeMoveInternal([NotNull] GameBoard board, [CanBeNull] GameMove move)
        {
            Interlocked.Increment(ref _moveCount);
            return move == null ? board.MakeNullMove() : board.MakeMove(move);
        }

        #endregion
    }
}