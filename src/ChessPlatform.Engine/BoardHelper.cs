using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoard MakeMove([NotNull] GameBoard board, [NotNull] GameMove move)
        {
            var result = MakeMoveInternal(board, move);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameBoard MakeNullMove([NotNull] GameBoard board)
        {
            var result = MakeMoveInternal(board, null);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetLocalMoveCount()
        {
            _localMoveCount = 0;
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private GameBoard MakeMoveInternal([NotNull] GameBoard board, [CanBeNull] GameMove move)
        {
            Interlocked.Increment(ref _localMoveCount);
            Interlocked.Increment(ref _totalMoveCount);
            return move == null ? board.MakeNullMove() : board.MakeMove(move);
        }

        #endregion
    }
}