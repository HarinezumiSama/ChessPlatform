using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using ChessPlatform.ComputerPlayers.SmartEnough;
using ChessPlatform.GamePlay;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class SmartEnoughPlayerTests
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
            var gameBoard = new GameBoard(fen);

            var player = new SmartEnoughPlayer(gameBoard.ActiveColor, maxPlyDepth, false, null);

            var stopwatch = Stopwatch.StartNew();
            var task = player.CreateGetMoveTask(new GetMoveRequest(gameBoard, CancellationToken.None));
            task.Start();
            var move = task.Result;
            stopwatch.Stop();

            Console.WriteLine(
                @"[{0} @ {1}] Time {2}, move {3}, max ply depth {4}.",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                DateTimeOffset.Now.ToFixedString(),
                stopwatch.Elapsed,
                move,
                player.MaxPlyDepth);

            Assert.That(move, Is.Not.Null);
        }

        #endregion
    }
}