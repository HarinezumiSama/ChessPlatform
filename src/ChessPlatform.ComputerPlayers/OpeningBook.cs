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

            const int MoveLength = 4;

            var openingMap = new Dictionary<PackedGameBoard, HashSet<PieceMove>>();
            var initialBoard = new GameBoard();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
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

                GameBoard currentBoard = null;
                var moves = new PieceMove[lineLength / MoveLength];
                for (int startIndex = 0, index = 0; startIndex < lineLength; startIndex += MoveLength, index++)
                {
                    currentBoard = currentBoard == null ? initialBoard : currentBoard.MakeMove(moves[index - 1]);

                    var stringNotation = line.Substring(startIndex, MoveLength);
                    var move = PieceMove.FromStringNotation(stringNotation);
                    moves[index] = move;

                    var packedGameBoard = currentBoard.Pack();
                    var openingMoves = openingMap.GetValueOrCreate(packedGameBoard);
                    openingMoves.Add(move);
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
    }
}