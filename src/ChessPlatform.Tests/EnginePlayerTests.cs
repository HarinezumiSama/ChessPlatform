using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using ChessPlatform.Engine;
using ChessPlatform.GamePlay;
using NUnit.Framework;
using Omnifactotum.Annotations;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class EnginePlayerTests
    {
        [Test]
        [TestCase(2, "d8f6")]
        [TestCase(3, "a5c6")]
        [TestCase(4, "a5c6")]
        [TestCase(5, "d7d5")]
        [TestCase(6, null, Explicit = true)]
        [TestCase(7, null, Explicit = true)]
        [TestCase(8, null, Explicit = true)]
        [TestCase(9, null, Explicit = true)]
        public void TestPerformanceOfGetMoveForPosition1(
            int maxPlyDepth,
            [CanBeNull] string expectedBestMoveString)
        {
            const string Fen = "r1bqkbnr/pppp1ppp/4p3/n7/4P3/3B1N2/PPPP1PPP/RNBQK2R b KQkq - 3 4";

            var expectedBestMove = expectedBestMoveString == null
                ? null
                : GameMove.FromStringNotation(expectedBestMoveString);

            ExecuteTestForFenAndDepth(Fen, maxPlyDepth, expectedBestMove);
        }

        [Test]
        [TestCase(2, "e4e5")]
        [TestCase(3, "e4e5")]
        [TestCase(4, "e4e5")]
        [TestCase(5, "e4e5")]
        [TestCase(6, null, Explicit = true)]
        [TestCase(7, null, Explicit = true)]
        [TestCase(8, null, Explicit = true)]
        [TestCase(9, null, Explicit = true)]
        public void TestPerformanceOfGetMoveForQueenUnderAttack(
            int maxPlyDepth,
            [CanBeNull] string expectedBestMoveString)
        {
            const string Fen = "rn3rk1/ppp2ppb/3qp2p/8/1b2P3/2Q3P1/PPPN1PBP/R1B2RK1 w - - 5 13";

            var expectedBestMove = expectedBestMoveString == null
                ? null
                : GameMove.FromStringNotation(expectedBestMoveString);

            ExecuteTestForFenAndDepth(Fen, maxPlyDepth, expectedBestMove);
        }

        private static void ExecuteTestForFenAndDepth(
            string fen,
            int maxPlyDepth,
            [CanBeNull] GameMove expectedBestMove)
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
                UseMultipleProcessors = false,
                UseTranspositionTable = false
            };

            var player = new EnginePlayer(gameBoard.ActiveSide, playerParameters);

            var stopwatch = Stopwatch.StartNew();
            var gameControlStub = new GameControl();
            var request = new GetMoveRequest(gameBoard, CancellationToken.None, gameControlStub);
            var task = player.CreateGetMoveTask(request);

            task.Start();
            var principalVariationInfo = task.Result;
            stopwatch.Stop();

            Console.WriteLine(
                @"[{0} @ {1}] ({2}) Time {3}, PV {{{4}}}, max ply depth {5}.",
                currentMethodName,
                DateTimeOffset.Now.ToFixedString(),
                ChessHelper.PlatformVersion,
                stopwatch.Elapsed,
                principalVariationInfo,
                player.MaxPlyDepth);

            Console.WriteLine();

            Assert.That(principalVariationInfo, Is.Not.Null);

            if (expectedBestMove != null)
            {
                Assert.That(principalVariationInfo.FirstMove, Is.EqualTo(expectedBestMove));
            }
        }
    }
}