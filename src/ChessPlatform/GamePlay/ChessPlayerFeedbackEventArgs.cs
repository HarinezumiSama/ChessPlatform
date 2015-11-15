using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public sealed class ChessPlayerFeedbackEventArgs
    {
        #region Constructors

        public ChessPlayerFeedbackEventArgs(
            PieceColor color,
            [NotNull] GameBoard board,
            int depth,
            int maxDepth,
            [NotNull] VariationLine variation)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

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

            if (variation == null)
            {
                throw new ArgumentNullException(nameof(variation));
            }

            #endregion

            Color = color;
            Board = board;
            Depth = depth;
            MaxDepth = maxDepth;
            Variation = variation;
        }

        #endregion

        #region Public Properties

        public PieceColor Color
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

        #endregion
    }
}