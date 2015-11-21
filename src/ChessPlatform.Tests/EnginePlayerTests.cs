using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using ChessPlatform.Engine;
using ChessPlatform.GamePlay;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class EnginePlayerTests
    {
        #region Tests

        [Test]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6, Explicit = true)]
        [TestCase(7, Explicit = true)]
        [TestCase(8, Explicit = true)]
        [TestCase(9, Explicit = true)]
        public void TestPerformanceOfGetMoveForPosition1(int maxPlyDepth)
        {
            const string Fen = "r1bqkbnr/pppp1ppp/4p3/n7/4P3/3B1N2/PPPP1PPP/RNBQK2R b KQkq - 3 4";
            ExecuteTestForFenAndDepth(Fen, maxPlyDepth);
        }

        [Test]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6, Explicit = true)]
        [TestCase(7, Explicit = true)]
        [TestCase(8, Explicit = true)]
        [TestCase(9, Explicit = true)]
        public void TestPerformanceOfGetMoveForQueenUnderAttack(int maxPlyDepth)
        {
            // Currently, the issue occurs at depth 5, where the computer player gives up a queen by move a2-a3
            const string Fen = "rn3rk1/ppp2ppb/3qp2p/8/1b2P3/2Q3P1/PPPN1PBP/R1B2RK1 w - - 5 13";
            ExecuteTestForFenAndDepth(Fen, maxPlyDepth);
        }

        #endregion

        #region Private Methods

        private static void ExecuteTestForFenAndDepth(string fen, int maxPlyDepth)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Console.WriteLine(
                @"[{0}] Executing the test for '{1}' with max ply depth {2}...",
                currentMethodName,
                fen,
                maxPlyDepth);

            var gameBoard = new GameBoard(fen);

            var playerParameters = new EnginePlayerParameters
            {
                MaxPlyDepth = maxPlyDepth,
                UseOpeningBook = false,
                MaxTimePerMove = null,
                UseMultipleProcessors = false
            };

            var player = new EnginePlayer(gameBoard.ActiveColor, playerParameters);

            var stopwatch = Stopwatch.StartNew();
            var gameControlStub = new GameControl();
            var task = player.CreateGetMoveTask(
                new GetMoveRequest(gameBoard, CancellationToken.None, gameControlStub));
            task.Start();
            var principalVariationInfo = task.Result;
            stopwatch.Stop();

            Console.WriteLine(
                @"[{0} @ {1}] ({2}) Time {3}, PV {{{4}}}, max ply depth {5}.",
                currentMethodName,
                DateTimeOffset.Now.ToFixedString(),
                ChessHelper.GetPlatformVersion(true),
                stopwatch.Elapsed,
                principalVariationInfo,
                player.MaxPlyDepth);

            Console.WriteLine();

            Assert.That(principalVariationInfo, Is.Not.Null);
        }

        #endregion
    }
}