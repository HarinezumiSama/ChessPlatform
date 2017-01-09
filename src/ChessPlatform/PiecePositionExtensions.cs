using System;
using System.Text;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class PiecePositionExtensions
    {
        #region Public Methods

        public static void GetFenSnippet([NotNull] this PiecePosition position, [NotNull] StringBuilder resultBuilder)
        {
            #region Argument Check

            if (position == null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (resultBuilder == null)
            {
                throw new ArgumentNullException(nameof(resultBuilder));
            }

            #endregion

            var emptySquareCount = new ValueContainer<int>(0);
            Action writeEmptySquareCount =
                () =>
                {
                    if (emptySquareCount.Value > 0)
                    {
                        resultBuilder.Append(emptySquareCount.Value);
                        emptySquareCount.Value = 0;
                    }
                };

            for (var rank = ChessConstants.RankCount - 1; rank >= 0; rank--)
            {
                if (rank < ChessConstants.RankCount - 1)
                {
                    resultBuilder.Append(ChessConstants.FenRankSeparator);
                }

                for (var file = 0; file < ChessConstants.FileCount; file++)
                {
                    var square = new Square(file, rank);
                    var piece = position[square];
                    if (piece == Piece.None)
                    {
                        emptySquareCount.Value++;
                        continue;
                    }

                    writeEmptySquareCount();
                    var fenChar = piece.GetFenChar();
                    resultBuilder.Append(fenChar);
                }

                writeEmptySquareCount();
            }
        }

        public static string GetFenSnippet([NotNull] this PiecePosition position)
        {
            var resultBuilder = new StringBuilder();
            GetFenSnippet(position, resultBuilder);
            return resultBuilder.ToString();
        }

        #endregion
    }
}