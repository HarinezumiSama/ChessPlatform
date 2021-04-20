using System;
using NUnit.Framework;
using Omnifactotum.Annotations;

namespace ChessPlatform.Tests
{
    internal static class GameBoardTestHelper
    {
        public static GameBoard MakeMultipleMoves(
            [NotNull] this GameBoard gameBoard,
            [CanBeNull] Action<GameBoard, string> makeAssertion,
            [NotNull] params GameMove[] moves)
        {
            Assert.That(gameBoard, Is.Not.Null);
            Assert.That(moves, Is.Not.Null);
            Assert.That(moves.Length, Is.GreaterThan(0));

            var currentBoard = gameBoard;
            for (var index = 0; index < moves.Length; index++)
            {
                var move = moves[index];
                Assert.That(move, Is.Not.Null, "The move at the index {0} is null.", index);

                currentBoard = currentBoard.MakeMove(move);
                if (makeAssertion is null)
                {
                    continue;
                }

                var errorMessage = $@"Assertion has failed after the move '{move}' at the index {index}.";
                makeAssertion(currentBoard, errorMessage);
            }

            return currentBoard;
        }

        public static GameBoard MakeMultipleMoves(
            [NotNull] this GameBoard gameBoard,
            [NotNull] params GameMove[] moves)
        {
            return MakeMultipleMoves(gameBoard, null, moves);
        }
    }
}