using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class GameDescription
    {
        public GameDescription(
            [NotNull] GameBoard initialBoard,
            [NotNull] IReadOnlyList<GameMove> moves)
        {
            if (initialBoard == null)
            {
                throw new ArgumentNullException(nameof(initialBoard));
            }

            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            if (moves.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", nameof(moves));
            }

            InitialBoard = initialBoard;
            Moves = moves.ToArray().AsReadOnly();
            FinalBoard = IterateAndValidateMoves(initialBoard, moves);
        }

        public GameDescription([NotNull] GameBoard finalBoard)
        {
            if (finalBoard == null)
            {
                throw new ArgumentNullException(nameof(finalBoard));
            }

            var tuple = PopMoves(finalBoard);
            InitialBoard = tuple.Item1;
            Moves = tuple.Item2.AsReadOnly();
            FinalBoard = finalBoard;
        }

        [NotNull]
        public GameBoard InitialBoard
        {
            get;
        }

        [NotNull]
        public ReadOnlyCollection<GameMove> Moves
        {
            get;
        }

        [NotNull]
        public GameBoard FinalBoard
        {
            get;
        }

        private static GameBoard IterateAndValidateMoves(
            [NotNull] GameBoard initialBoard,
            [NotNull] IReadOnlyList<GameMove> moves)
        {
            var moveCount = moves.Count;

            var currentBoard = initialBoard;
            for (var index = 0; index < moveCount; index++)
            {
                var move = moves[index];

                if (!currentBoard.ValidMoves.ContainsKey(move))
                {
                    throw new ArgumentException(
                        $@"The move #{index + 1}/{moveCount} is not a valid move for the respective board.",
                        nameof(moves));
                }

                currentBoard = currentBoard.MakeMove(move);
            }

            return currentBoard;
        }

        private static Tuple<GameBoard, GameMove[]> PopMoves([NotNull] GameBoard finalBoard)
        {
            var moveStack = new Stack<GameMove>();

            var currentBoard = finalBoard;
            while (currentBoard.PreviousMove != null)
            {
                moveStack.Push(currentBoard.PreviousMove);
                currentBoard = currentBoard.PreviousBoard.EnsureNotNull();
            }

            return Tuple.Create(currentBoard, moveStack.ToArray());
        }
    }
}