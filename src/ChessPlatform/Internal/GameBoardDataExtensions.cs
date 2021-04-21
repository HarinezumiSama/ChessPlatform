using System;
using System.Text;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal static class GameBoardDataExtensions
    {
        public static void GetFenSnippet(this GameBoardData gameBoardData, StringBuilder resultBuilder)
        {
            if (gameBoardData is null)
            {
                throw new ArgumentNullException(nameof(gameBoardData));
            }

            if (resultBuilder is null)
            {
                throw new ArgumentNullException(nameof(resultBuilder));
            }

            var emptySquareCount = new ValueContainer<int>(0);

            void WriteEmptySquareCount()
            {
                //// ReSharper disable once InvertIf
                if (emptySquareCount.Value > 0)
                {
                    resultBuilder.Append(emptySquareCount.Value);
                    emptySquareCount.Value = 0;
                }
            }

            for (var rank = ChessConstants.RankCount - 1; rank >= 0; rank--)
            {
                if (rank < ChessConstants.RankCount - 1)
                {
                    resultBuilder.Append(ChessConstants.FenRankSeparator);
                }

                for (var file = 0; file < ChessConstants.FileCount; file++)
                {
                    var square = new Square(file, rank);
                    var piece = gameBoardData.PiecePosition[square];
                    if (piece == Piece.None)
                    {
                        emptySquareCount.Value++;
                        continue;
                    }

                    WriteEmptySquareCount();
                    var fenChar = piece.GetFenChar();
                    resultBuilder.Append(fenChar);
                }

                WriteEmptySquareCount();
            }
        }

        public static string GetFenSnippet(this GameBoardData gameBoardData)
        {
            var resultBuilder = new StringBuilder();
            GetFenSnippet(gameBoardData, resultBuilder);
            return resultBuilder.ToString();
        }
    }
}