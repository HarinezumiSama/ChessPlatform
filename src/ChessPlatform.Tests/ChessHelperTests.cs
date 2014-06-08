using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class ChessHelperTests
    {
        #region Tests

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestGetPlatformVersion(bool fullVersion)
        {
            var platformVersion = ChessHelper.GetPlatformVersion(fullVersion);

            Console.WriteLine(
                @"[{0}] {1} -> '{2}'",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                fullVersion,
                platformVersion);

            Assert.That(platformVersion, Is.Not.Null);
            Assert.That(platformVersion, Is.Not.Empty);
        }

        [Test]
        [TestCase(ChessConstants.DefaultInitialFen, true)]
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -", false)]
        [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 15 89", true)]
        [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 3 8", true)]
        [TestCase("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", true)]
        [TestCase("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1", true)]
        [TestCase("rnbqkb1r/pp1p1ppp/2p5/4P3/2B5/8/PPP1NnPP/RNBQK2R w KQkq - 0 6", true)]
        [TestCase("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10", true)]
        public void TestIsValidFenFormat(string fen, bool expectedResult)
        {
            var actualResult = ChessHelper.IsValidFenFormat(fen);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        #endregion
    }
}