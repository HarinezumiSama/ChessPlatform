using System;
using System.Diagnostics;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public sealed class ChessPlayerFeedbackEventArgs
    {
        public ChessPlayerFeedbackEventArgs(
            GameSide side,
            [NotNull] GameBoard board,
            int depth,
            int maxDepth,
            [NotNull] VariationLine variation)
        {
            if (depth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth),
                    depth,
                    @"The value must be positive.");
            }

            if (maxDepth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxDepth),
                    maxDepth,
                    @"The value must be positive.");
            }

            if (depth > maxDepth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth),
                    depth,
                    $@"{nameof(depth)} must not be greater than {nameof(maxDepth)} ({maxDepth}).");
            }

            Side = side;
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Depth = depth;
            MaxDepth = maxDepth;
            Variation = variation ?? throw new ArgumentNullException(nameof(variation));
        }

        public GameSide Side
        {
            [DebuggerStepThrough]
            get;
        }

        [NotNull]
        public GameBoard Board
        {
            [DebuggerStepThrough]
            get;
        }

        public int Depth
        {
            [DebuggerStepThrough]
            get;
        }

        public int MaxDepth
        {
            [DebuggerStepThrough]
            get;
        }

        [NotNull]
        public VariationLine Variation
        {
            [DebuggerStepThrough]
            get;
        }
    }
}