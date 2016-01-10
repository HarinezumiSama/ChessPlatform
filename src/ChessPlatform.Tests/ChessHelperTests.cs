using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class ChessHelperTests
    {
        #region Tests

        [Test]
        public void TestGetPlatformVersion()
        {
            var platformVersion = ChessHelper.PlatformVersion;

            Console.WriteLine($@"[{nameof(TestGetPlatformVersion)}] -> '{platformVersion}'");

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

        [Test]
        [TestCase(ChessConstants.DefaultInitialFen, "e2e4", "e4")]
        [TestCase(ChessConstants.DefaultInitialFen, "b1c3", "Nc3")]
        [TestCase("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", "d7d5", "d5")]
        [TestCase("rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 2", "e4d5", "exd5")]
        [TestCase("rnbqkbnr/pp2pppp/8/2pP4/8/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 3", "d5c6", "dxc6")]
        [TestCase("rnbqkbnr/pp2pppp/2P5/8/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 3", "b8c6", "Nxc6")]
        [TestCase("r1bqkbnr/pp2pppp/2n5/8/8/8/PPPP1PPP/RNBQKBNR w KQkq - 0 4", "f1b5", "Bb5")]
        [TestCase("r1bqkbnr/1p2pppp/2n5/pB6/8/8/PPPP1PPP/RNBQK1NR w KQkq a6 0 5", "g1h3", "Nh3")]
        [TestCase("r1bqkbnr/4pppp/1pn5/pB4N1/8/5Q2/PPPP1PPP/RNB1K2R w KQkq - 1 8", "f3f7", "Qxf7+")]
        [TestCase("r1bq1bnr/3kpQpp/1pn5/pB4N1/8/8/PPPP1PPP/RNB1K2R w KQ - 1 9", "f7e6", "Qe6+")]
        [TestCase("r1bqkbnr/4p1pp/1pn1Q3/pB4N1/8/8/PPPP1PPP/RNB1K2R w KQ - 1 10", "b5c6", "Bxc6+")]
        [TestCase("r1bqkbnr/4p1pp/1pB1Q3/p5N1/8/8/PPPP1PPP/RNB1K2R b KQ - 0 10", "c8d7", "Bd7")]
        [TestCase("r2qkbnr/3bp1pp/1pB1Q3/p5N1/8/8/PPPP1PPP/RNB1K2R w KQ - 1 11", "e6f7", "Qf7#")]
        [TestCase("r2qkbnr/3bpppp/1pB5/p5N1/8/5Q2/PPPP1PPP/RNB1K2R w KQkq - 1 9", "f3f7", "Qxf7#")]
        [TestCase("r2qkb1r/3bpppp/1pB4n/p5N1/8/2N2Q2/PPPP1PPP/R1B1K2R w KQkq - 1 10", "c3e4", "Nce4")]
        [TestCase("r2qkb1r/3b1ppp/1pB4n/p5N1/P3p3/2N2Q2/1PPP1PPP/R1B2RK1 w kq - 0 12", "c3e4", "Ncxe4")]
        [TestCase("r4b1r/3k2pp/1p6/p3P1N1/6n1/2N5/PPP2PPP/R1B1K2R w KQ - 1 16", "e5e6", "e6+")]
        [TestCase("r4b1r/6pp/1p1kP3/p5N1/6n1/2N5/PPP2PPP/R1B1K2R w KQ - 1 17", "c1f4", "Bf4+")]
        [TestCase("r4b1r/6pp/1p2P3/p1k3N1/5Bn1/2N5/PPP2PPP/R3K2R w KQ - 1 18", "e1c1", "O-O-O")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7d8Q", "exd8=Q")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7d8R", "exd8=R")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7d8B", "exd8=B")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7d8N", "exd8=N+")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7e8Q", "e8=Q+")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7e8R", "e8=R")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7e8B", "e8=B+")]
        [TestCase("r2r4/4P1pp/1pkB4/p5N1/6n1/2N5/PPP2PPP/2KR3R w - - 1 21", "e7e8N", "e8=N")]
        [TestCase("3r4/8/1pkB2p1/p5N1/3RN1np/8/PPP2PPP/2KR4 w - - 0 25", "d1d3", "R1d3")]
        [TestCase("3r4/8/1pkB2p1/p5N1/3RN1np/1R3R2/PPP2PPP/2KR4 w - - 0 1", "b3c3", "Rbc3+")]
        [TestCase("3r4/8/1pkB2p1/p5N1/3RN1np/1R3R2/PPP2PPP/2KR4 w - - 0 1", "f3c3", "Rfc3+")]
        [TestCase("3r4/6k1/1p1B2p1/p1N3N1/6np/2N3N1/PPP2PPP/2KR3R w - - 0 1", "g3e4", "Ng3e4")]
        [TestCase("3r4/8/1p1B1kp1/p1N3N1/6np/P1N3N1/1PP2PPP/2KR3R w - - 1 2", "g3e4", "Ng3e4+")]
        [TestCase("3r4/8/1p1B1kp1/p1N3N1/6np/P1N3N1/1PP2PPP/2KR3R w - - 1 2", "c5e4", "Nc5e4+")]
        [TestCase("3r4/8/1p1B1kp1/p1N5/6np/P1N3N1/1PP2PPP/2KR3R w - - 0 1", "g3e4", "Nge4+")]
        [TestCase("3r4/8/1p1B1kp1/p1N5/6np/P1N3N1/1PP2PPP/2KR3R w - - 0 1", "c3e4", "Nc3e4+")]
        [TestCase("3r4/8/1p1B1kp1/p1N5/6np/P1N3N1/1PP2PPP/2KR3R w - - 0 1", "c5e4", "N5e4+")]
        [TestCase("3r4/8/1p1B1kp1/p1N5/4n2p/P1N3N1/1PP2PPP/2KR3R w - - 0 1", "g3e4", "Ngxe4+")]
        [TestCase("3r4/8/1p1B1kp1/p1N5/4n2p/P1N3N1/1PP2PPP/2KR3R w - - 0 1", "c3e4", "Nc3xe4+")]
        [TestCase("3r4/8/1p1B1kp1/p1N5/4n2p/P1N3N1/1PP2PPP/2KR3R w - - 0 1", "c5e4", "N5xe4+")]
        [TestCase("4k3/2Br4/1p6/p1N3p1/4N1np/PPN5/2P2PPP/2KRR3 w - - 1 5", "e4d6", "Nd6+")]
        [TestCase("r3k2r/pppqbppp/2npbn2/1B2p3/4P3/2NP1N2/PPPBQPPP/R3K2R w KQkq - 1 8", "e1g1", "O-O")]
        [TestCase("r3k2r/pppqbppp/2npbn2/1B2p3/4P3/2NP1N2/PPPBQPPP/R3K2R w KQkq - 1 8", "e1c1", "O-O-O")]
        [TestCase("r3k2r/pppqbppp/2npbn2/1B2p3/4P3/2NP1N2/PPPBQPPP/R4RK1 b kq - 0 8", "e8g8", "O-O")]
        [TestCase("r3k2r/pppqbppp/2npbn2/1B2p3/4P3/2NP1N2/PPPBQPPP/R4RK1 b kq - 0 8", "e8c8", "O-O-O")]
        [TestCase("8/8/8/8/8/8/7R/1k2K2R w K - 0 1", "e1g1", "O-O#")]
        [TestCase("8/8/8/7R/8/8/8/2k1K2R w K - 1 2", "e1g1", "O-O+")]
        public void TestGetStandardAlgebraicNotation(string fen, string moveNotation, string expectedResult)
        {
            var board = new GameBoard(fen);
            var move = GameMove.FromStringNotation(moveNotation);

            var actualResult1 = board.GetStandardAlgebraicNotation(move);
            Assert.That(actualResult1, Is.EqualTo(expectedResult));

            var actualResult2 = move.ToStandardAlgebraicNotation(board);
            Assert.That(actualResult2, Is.EqualTo(expectedResult));
        }

        public void TestGetStandardAlgebraicNotationForCollection()
        {
            const string Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
            var board = new GameBoard(Fen);

            var moves = new[]
            {
                GameMove.FromStringNotation("d7d5"),
                GameMove.FromStringNotation("e4d5"),
                GameMove.FromStringNotation("d8d5")
            };

            var actualResult = board.GetStandardAlgebraicNotation(moves);
            Assert.That(actualResult, Is.EqualTo("d5, exd5, Qxd5"));
        }

        [Test]
        [TestCase("a1", "c2", PieceType.None, "a1c2")]
        [TestCase("f2", "f4", PieceType.None, "f2f4")]
        [TestCase("b7", "b8", PieceType.Knight, "b7b8n")]
        [TestCase("f2", "e1", PieceType.Queen, "f2e1q")]
        public void TestToUciNotation(string from, string to, PieceType promotionResult, string expectedResult)
        {
            var move = new GameMove(Square.FromAlgebraic(@from), Square.FromAlgebraic(to), promotionResult);
            var actualResult = move.ToUciNotation();
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public void TestToUciNotationForCollection()
        {
            Assert.That(new GameMove[0].ToUciNotation(), Is.EqualTo(string.Empty));

            var moves = new[]
            {
                new GameMove(Square.FromAlgebraic("a1"), Square.FromAlgebraic("c2"), PieceType.None),
                new GameMove(Square.FromAlgebraic("f2"), Square.FromAlgebraic("f4"), PieceType.None),
                new GameMove(Square.FromAlgebraic("b7"), Square.FromAlgebraic("b8"), PieceType.Knight),
                new GameMove(Square.FromAlgebraic("f2"), Square.FromAlgebraic("e1"), PieceType.Queen)
            };

            var actualResult = moves.ToUciNotation();
            Assert.That(actualResult, Is.EqualTo("a1c2, f2f4, b7b8n, f2e1q"));
        }

        #endregion
    }
}