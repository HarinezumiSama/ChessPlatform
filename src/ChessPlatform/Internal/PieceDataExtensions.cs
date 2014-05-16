using System;
using System.Linq;
using System.Text;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal static class PieceDataExtensions
    {
        #region Public Methods

        public static void GetFenSnippet(this PieceData pieceData, StringBuilder resultBuilder)
        {
            #region Argument Check

            if (pieceData == null)
            {
                throw new ArgumentNullException("pieceData");
            }

            if (resultBuilder == null)
            {
                throw new ArgumentNullException("resultBuilder");
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
                    var position = new Position((byte)file, (byte)rank);
                    var piece = pieceData[position];
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

        public static string GetFenSnippet(this PieceData pieceData)
        {
            var resultBuilder = new StringBuilder();
            GetFenSnippet(pieceData, resultBuilder);
            return resultBuilder.ToString();
        }

        #endregion
    }
}