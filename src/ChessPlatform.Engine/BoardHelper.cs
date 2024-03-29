﻿using System.Runtime.CompilerServices;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    internal sealed class BoardHelper
    {
        private long _localMoveCount;
        private long _totalMoveCount;

        public long LocalMoveCount => _localMoveCount;

        public long TotalMoveCount => _totalMoveCount;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private GameBoard MakeMoveInternal([NotNull] GameBoard board, [CanBeNull] GameMove move)
        {
            Interlocked.Increment(ref _localMoveCount);
            Interlocked.Increment(ref _totalMoveCount);
            return move is null ? board.MakeNullMove() : board.MakeMove(move);
        }
    }
}