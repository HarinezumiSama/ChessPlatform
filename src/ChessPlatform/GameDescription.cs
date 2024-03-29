﻿using System;
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
            if (moves is null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            if (moves.Any(item => item is null))
            {
                throw new ArgumentException(@"The collection contains a null element.", nameof(moves));
            }

            InitialBoard = initialBoard ?? throw new ArgumentNullException(nameof(initialBoard));
            Moves = moves.ToArray().AsReadOnly();
            FinalBoard = IterateAndValidateMoves(initialBoard, moves);
        }

        public GameDescription([NotNull] GameBoard finalBoard)
        {
            if (finalBoard is null)
            {
                throw new ArgumentNullException(nameof(finalBoard));
            }

            var (initialBoard, moves) = PopMoves(finalBoard);
            InitialBoard = initialBoard;
            Moves = moves.AsReadOnly();
            FinalBoard = finalBoard;
        }

        [NotNull]
        public GameBoard InitialBoard { get; }

        [NotNull]
        public ReadOnlyCollection<GameMove> Moves { get; }

        [NotNull]
        public GameBoard FinalBoard { get; }

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

        private static (GameBoard InitialBoard, GameMove[] Moves) PopMoves([NotNull] GameBoard finalBoard)
        {
            var moveStack = new Stack<GameMove>();

            var currentBoard = finalBoard;
            while (currentBoard.PreviousMove != null)
            {
                moveStack.Push(currentBoard.PreviousMove);
                currentBoard = currentBoard.PreviousBoard.EnsureNotNull();
            }

            return (currentBoard, moveStack.ToArray());
        }
    }
}