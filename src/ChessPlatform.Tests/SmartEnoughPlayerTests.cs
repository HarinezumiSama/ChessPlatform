using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using ChessPlatform.ComputerPlayers;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class SmartEnoughPlayerTests
    {
        #region Tests

        [Test]
        public void TestPerformanceOfGetMoveForPosition1()
        {
            var gameBoard = new GameBoard("r1bqkbnr/pppp1ppp/4p3/n7/4P3/3B1N2/PPPP1PPP/RNBQK2R b KQkq - 3 4");

            var player = new SmartEnoughPlayer(PieceColor.Black, 4, false);

            var stopwatch = Stopwatch.StartNew();
            var task = player.GetMove(gameBoard, CancellationToken.None);
            task.Start();
            var move = task.Result;
            stopwatch.Stop();

            Console.WriteLine(
                @"[{0}] GetMove took {1} (move {2}).",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                stopwatch.Elapsed,
                move);

            Assert.That(move, Is.Not.Null);
        }

        #endregion
    }
}