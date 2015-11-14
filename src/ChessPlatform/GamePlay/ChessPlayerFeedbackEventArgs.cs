using System;
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

            if (variation == null)
            {
                throw new ArgumentNullException(nameof(variation));
            }

            #endregion

            Color = color;
            Board = board;
            Depth = depth;
            Variation = variation;
        }

        #endregion

        #region Public Properties

        public PieceColor Color
        {
            get;
        }

        [NotNull]
        public GameBoard Board
        {
            get;
        }

        public int Depth
        {
            get;
        }

        [NotNull]
        public VariationLine Variation
        {
            get;
        }

        #endregion
    }
}