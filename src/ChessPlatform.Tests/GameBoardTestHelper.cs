﻿using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Omnifactotum.Annotations;

namespace ChessPlatform.Tests
{
    internal static class GameBoardTestHelper
    {
        #region Public Methods

        public static GameBoard MakeMultipleMoves(
            [NotNull] this GameBoard gameBoard,
            [CanBeNull] Action<GameBoard, string> makeAssertion,
            [NotNull] params PieceMove[] moves)
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
                if (makeAssertion == null)
                {
                    continue;
                }

                var errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "Assertion has failed after the move '{0}' at the index {1}.",
                    move,
                    index);

                makeAssertion(currentBoard, errorMessage);
            }

            return currentBoard;
        }

        public static GameBoard MakeMultipleMoves(
            [NotNull] this GameBoard gameBoard,
            [NotNull] params PieceMove[] moves)
        {
            return MakeMultipleMoves(gameBoard, null, moves);
        }

        #endregion
    }
}