using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers
{
    public sealed class OpeningBook
    {
        #region Constants and Fields

        private static readonly PieceMove[] NoMoves = new PieceMove[0];

        private readonly Dictionary<PackedGameBoard, PieceMove[]> _openingMap;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="OpeningBook"/> class.
        /// </summary>
        public OpeningBook([NotNull] TextReader reader)
        {
            #region Argument Check

            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            #endregion

            var initialBoard = new GameBoard();

            var openingLines = new List<List<Tuple<PackedGameBoard, PieceMove>>>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var openingLine = ParseLine(line, initialBoard);
                openingLines.Add(openingLine);
            }

            var openingMap = new Dictionary<PackedGameBoard, HashSet<PieceMove>>();
            foreach (var openingLine in openingLines)
            {
                foreach (var tuple in openingLine)
                {
                    var packedGameBoard = tuple.Item1;
                    var openingMoves = openingMap.GetValueOrCreate(packedGameBoard);
                    openingMoves.Add(tuple.Item2);
                }
            }

            _openingMap = openingMap.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }

        #endregion

        #region Public Methods

        public PieceMove[] FindPossibleMoves([NotNull] IGameBoard board)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            var packedGameBoard = board.Pack();
            var moves = _openingMap.GetValueOrDefault(packedGameBoard);
            return moves == null ? NoMoves : moves.Copy();
        }

        #endregion

        #region Private Methods

        private static List<Tuple<PackedGameBoard, PieceMove>> ParseLine(
            [NotNull] string line,
            [NotNull] GameBoard initialBoard)
        {
            const int MoveLength = 4;

            line = line.Trim();
            var lineLength = line.Length;

            if (lineLength % MoveLength != 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid line '{0}'.",
                        line));
            }

            const int OpeningLineCapacity = 20;
            var openingLine = new List<Tuple<PackedGameBoard, PieceMove>>(OpeningLineCapacity);

            var currentBoard = initialBoard;
            for (int startIndex = 0, index = 0; startIndex < lineLength; startIndex += MoveLength, index++)
            {
                var stringNotation = line.Substring(startIndex, MoveLength);
                var move = PieceMove.FromStringNotation(stringNotation);

                openingLine.Add(Tuple.Create(currentBoard.Pack(), move));
                currentBoard = currentBoard.MakeMove(move);
            }

            return openingLine;
        }

        #endregion
    }
}