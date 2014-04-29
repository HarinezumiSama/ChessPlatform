using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Omnifactotum;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class BoardStateTests
    {
        #region Constants and Fields

        private const string DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private static readonly ReadOnlyDictionary<PerftPosition, string> PerftPositionToFenMap =
            new ReadOnlyDictionary<PerftPosition, string>(
                new Dictionary<PerftPosition, string>
                {
                    {
                        PerftPosition.Initial,
                        DefaultFen
                    },
                    {
                        PerftPosition.Position2,
                        "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1"
                    },
                    {
                        PerftPosition.Position4,
                        "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1"
                    },
                    {
                        PerftPosition.Position5,
                        "rnbqkb1r/pp1p1ppp/2p5/4P3/2B5/8/PPP1NnPP/RNBQK2R w KQkq - 0 6"
                    }
                });

        #endregion

        #region Tests

        [Test]
        public void TestDefaultConstruction()
        {
            var boardState = new BoardState();
            AssertDefaultInitialBoard(boardState);
        }

        [Test]
        public void TestConstructionByDefaultFen()
        {
            var boardState = new BoardState(DefaultFen);
            AssertDefaultInitialBoard(boardState);
        }

        [Test]
        public void TestConstructionByStalemateFen()
        {
            const string StalemateFen = "k7/8/1Q6/8/8/8/8/7K b - - 0 1";

            var boardState = new BoardState(StalemateFen);
            AssertBaseProperties(boardState, PieceColor.Black, CastlingOptions.None, null, 0, 1, GameState.Stalemate);
            AssertNoValidMoves(boardState);
        }

        [Test]
        public void TestMakeMoveBasicScenario()
        {
            var boardState1W = TestMakeMoveBasicScenario1W();
            var boardState1B = TestMakeMoveBasicScenario1B(boardState1W);
            var boardState2W = TestMakeMoveBasicScenario2W(boardState1B);
            var boardState2B = TestMakeMoveBasicScenario2B(boardState2W);
            var boardState3W = TestMakeMoveBasicScenario3W(boardState2B);
            var boardState3B = TestMakeMoveBasicScenario3B(boardState3W);
            Assert.That(boardState3B, Is.Not.Null);
        }

        [Test]
        public void TestMakeMoveFoolsMateScenario()
        {
            var boardState1W = new BoardState();
            var boardState1B = TestMakeMoveFoolsMateScenario1B(boardState1W);
            var boardState2W = TestMakeMoveFoolsMateScenario2W(boardState1B);
            var boardState2B = TestMakeMoveFoolsMateScenario2B(boardState2W);
            TestMakeMoveFoolsMateScenario3W(boardState2B);
        }

        [Test]
        public void TestTwoKingsOnlyCase()
        {
            var boardState = new BoardState("k7/8/K7/8/8/8/8/8 w - - 0 1");

            AssertBaseProperties(
                boardState,
                PieceColor.White,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.ForcedDrawInsufficientMaterial);

            AssertNoValidMoves(boardState);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(int.MinValue)]
        public void TestPerftForInvalidArgument(int depth)
        {
            var boardState = new BoardState();
            Assert.That(() => boardState.Perft(depth), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [TestCaseSource(typeof(TestPerftForInitialPositionCases))]
        public void TestPerft(PerftPosition perftPosition, PerftResult expectedResult)
        {
            var fen = PerftPositionToFenMap[perftPosition];
            var boardState = new BoardState(fen);

            var actualResult = boardState.Perft(expectedResult.Depth);

            Console.WriteLine(
                "[{0}] {1}: {2}",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                perftPosition.GetName(),
                actualResult);

            AssertPerftResult(actualResult, expectedResult);
        }

        #endregion

        #region Private Methods

        private static void AssertBaseProperties(
            BoardState boardState,
            PieceColor expectedActiveColor,
            CastlingOptions expectedCastlingOptions,
            EnPassantCaptureInfo expectedEnPassantCaptureInfo,
            int expectedHalfMovesBy50MoveRule,
            int expectedFullMoveIndex,
            GameState expectedGameState)
        {
            Assert.That(boardState, Is.Not.Null);

            Assert.That(boardState.ActiveColor, Is.EqualTo(expectedActiveColor));
            Assert.That(boardState.CastlingOptions, Is.EqualTo(expectedCastlingOptions));
            AssertEnPassantCaptureInfo(boardState.EnPassantCaptureInfo, expectedEnPassantCaptureInfo);
            Assert.That(boardState.FullMoveIndex, Is.EqualTo(expectedFullMoveIndex));
            Assert.That(boardState.HalfMovesBy50MoveRule, Is.EqualTo(expectedHalfMovesBy50MoveRule));
            Assert.That(boardState.State, Is.EqualTo(expectedGameState));
        }

        private static void AssertEnPassantCaptureInfo(
            EnPassantCaptureInfo actualEnPassantCaptureInfo,
            EnPassantCaptureInfo expectedEnPassantCaptureInfo)
        {
            if (expectedEnPassantCaptureInfo == null)
            {
                Assert.That(actualEnPassantCaptureInfo, Is.Null);
                return;
            }

            Assert.That(actualEnPassantCaptureInfo, Is.Not.Null);
            Assert.That(
                actualEnPassantCaptureInfo.CapturePosition,
                Is.EqualTo(expectedEnPassantCaptureInfo.CapturePosition));
            Assert.That(
                actualEnPassantCaptureInfo.TargetPiecePosition,
                Is.EqualTo(expectedEnPassantCaptureInfo.TargetPiecePosition));
        }

        private static void AssertValidMoves(BoardState boardState, params PieceMove[] expectedValidMoves)
        {
            Assert.That(boardState, Is.Not.Null);
            Assert.That(expectedValidMoves, Is.Not.Null);

            Assert.That(boardState.ValidMoves, Is.EquivalentTo(expectedValidMoves));
        }

        private static void AssertNoValidMoves(BoardState boardState)
        {
            Assert.That(boardState, Is.Not.Null);

            Assert.That(boardState.ValidMoves.Count, Is.EqualTo(0));
            Assert.That(boardState.ValidMoves, Is.Empty);
        }

        private static void AssertDefaultInitialBoard(BoardState boardState)
        {
            Assert.That(boardState, Is.Not.Null);

            Assert.That(boardState.GetFen(), Is.EqualTo(DefaultFen));

            AssertBaseProperties(boardState, PieceColor.White, CastlingOptions.All, null, 0, 1, GameState.Default);

            AssertValidMoves(
                boardState,
                "a2-a3",
                "a2-a4",
                "b2-b3",
                "b2-b4",
                "c2-c3",
                "c2-c4",
                "d2-d3",
                "d2-d4",
                "e2-e3",
                "e2-e4",
                "f2-f3",
                "f2-f4",
                "g2-g3",
                "g2-g4",
                "h2-h3",
                "h2-h4",
                "b1-a3",
                "b1-c3",
                "g1-f3",
                "g1-h3");
        }

        private static void AssertPerftResult(PerftResult actualResult, PerftResult expectedResult)
        {
            Assert.That(actualResult, Is.Not.Null);
            Assert.That(expectedResult, Is.Not.Null);

            Assert.That(actualResult.Depth, Is.EqualTo(expectedResult.Depth));
            Assert.That(actualResult.NodeCount, Is.EqualTo(expectedResult.NodeCount));

            if (expectedResult.Elapsed > TimeSpan.Zero)
            {
                Assert.That(actualResult.Elapsed, Is.LessThanOrEqualTo(expectedResult.Elapsed));
            }
        }

        private static BoardState TestMakeMoveBasicScenario1W()
        {
            var boardState1W = new BoardState();

            // Testing invalid move (no piece at the source position)
            Assert.That(() => boardState1W.MakeMove("a3-a4"), Throws.ArgumentException);

            AssertBaseProperties(boardState1W, PieceColor.White, CastlingOptions.All, null, 0, 1, GameState.Default);

            return boardState1W;
        }

        private static BoardState TestMakeMoveBasicScenario1B(BoardState boardState1W)
        {
            var boardState1B = boardState1W.MakeMove("e2-e4");

            Assert.That(
                boardState1B.GetFen(),
                Is.EqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1"));

            AssertBaseProperties(
                boardState1B,
                PieceColor.Black,
                CastlingOptions.All,
                new EnPassantCaptureInfo("e3", "e4"),
                0,
                1,
                GameState.Default);

            // Testing invalid move (piece of non-active color at the source position)
            Assert.That(() => boardState1B.MakeMove("d2-d4"), Throws.ArgumentException);

            AssertBaseProperties(
                boardState1B,
                PieceColor.Black,
                CastlingOptions.All,
                new EnPassantCaptureInfo("e3", "e4"),
                0,
                1,
                GameState.Default);

            return boardState1B;
        }

        private static BoardState TestMakeMoveBasicScenario2W(BoardState boardState1B)
        {
            var boardState2W = boardState1B.MakeMove("e7-e5");

            Assert.That(
                boardState2W.GetFen(),
                Is.EqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2"));

            AssertBaseProperties(
                boardState2W,
                PieceColor.White,
                CastlingOptions.All,
                new EnPassantCaptureInfo("e6", "e5"),
                0,
                2,
                GameState.Default);

            return boardState2W;
        }

        private static BoardState TestMakeMoveBasicScenario2B(BoardState boardState2W)
        {
            var boardState2B = boardState2W.MakeMove("b1-c3");

            Assert.That(
                boardState2B.GetFen(),
                Is.EqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR b KQkq - 1 2"));

            AssertBaseProperties(
                boardState2B,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                1,
                2,
                GameState.Default);

            return boardState2B;
        }

        private static BoardState TestMakeMoveBasicScenario3W(BoardState boardState2B)
        {
            var boardState3W = boardState2B.MakeMove("e8-e7");

            Assert.That(
                boardState3W.GetFen(),
                Is.EqualTo("rnbq1bnr/ppppkppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR w KQ - 2 3"));

            AssertBaseProperties(
                boardState3W,
                PieceColor.White,
                CastlingOptions.WhiteKingSide | CastlingOptions.WhiteQueenSide,
                null,
                2,
                3,
                GameState.Default);

            return boardState3W;
        }

        private static BoardState TestMakeMoveBasicScenario3B(BoardState boardState3W)
        {
            var boardState3B = boardState3W.MakeMove("c3-d5");

            Assert.That(
                boardState3B.GetFen(),
                Is.EqualTo("rnbq1bnr/ppppkppp/8/3Np3/4P3/8/PPPP1PPP/R1BQKBNR b KQ - 3 3"));

            AssertBaseProperties(
                boardState3B,
                PieceColor.Black,
                CastlingOptions.WhiteKingSide | CastlingOptions.WhiteQueenSide,
                null,
                3,
                3,
                GameState.Check);

            return boardState3B;
        }

        private static BoardState TestMakeMoveFoolsMateScenario1B(BoardState boardState1W)
        {
            var boardState1B = boardState1W.MakeMove("g2-g4");
            return boardState1B;
        }

        private static BoardState TestMakeMoveFoolsMateScenario2W(BoardState boardState1B)
        {
            var boardState2W = boardState1B.MakeMove("e7-e6");
            return boardState2W;
        }

        private static BoardState TestMakeMoveFoolsMateScenario2B(BoardState boardState2W)
        {
            var boardState2B = boardState2W.MakeMove("f2-f4");
            return boardState2B;
        }

        private static void TestMakeMoveFoolsMateScenario3W(BoardState boardState2B)
        {
            var boardState3W = boardState2B.MakeMove("d8-h4");

            AssertBaseProperties(
                boardState3W,
                PieceColor.White,
                CastlingOptions.All,
                null,
                1,
                3,
                GameState.Checkmate);

            AssertNoValidMoves(boardState3W);
        }

        #endregion

        #region PerftPosition Enumeration

        /// <summary>
        ///     Positions from http://chessprogramming.wikispaces.com/Perft+Results
        /// </summary>
        public enum PerftPosition
        {
            Initial,
            Position2,
            Position4,
            Position5
        }

        #endregion

        #region TestPerftForInitialPositionCases Class

        //// TODO [vmcl] Use Omnifactotum.NUnit once it's published
        public sealed class TestPerftForInitialPositionCases : IEnumerable<TestCaseData>
        {
            public IEnumerator<TestCaseData> GetEnumerator()
            {
                const string TooLongNow = "Move generation takes too much time now.";

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new PerftResult(0, TimeSpan.FromMilliseconds(100), 1));

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new PerftResult(1, TimeSpan.Zero, 20));

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new PerftResult(2, TimeSpan.Zero, 400));

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new PerftResult(3, TimeSpan.Zero, 8902));

                // Targeting ~100,000 NPS (or better) for the first optimization
                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new PerftResult(4, TimeSpan.FromSeconds(2d), 197281));

                yield return new TestCaseData(PerftPosition.Initial, new PerftResult(5, TimeSpan.Zero, 4865609))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Initial, new PerftResult(6, TimeSpan.Zero, 119060324))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Initial, new PerftResult(7, TimeSpan.Zero, 3195901860))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Initial, new PerftResult(8, TimeSpan.Zero, 84998978956))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position2, new PerftResult(1, TimeSpan.Zero, 48));

                yield return new TestCaseData(PerftPosition.Position2, new PerftResult(2, TimeSpan.Zero, 2039));

                yield return new TestCaseData(PerftPosition.Position2, new PerftResult(3, TimeSpan.Zero, 97862));

                yield return new TestCaseData(PerftPosition.Position2, new PerftResult(4, TimeSpan.Zero, 4085603))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position2, new PerftResult(5, TimeSpan.Zero, 193690690))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position4, new PerftResult(1, TimeSpan.Zero, 6));
                yield return new TestCaseData(PerftPosition.Position4, new PerftResult(2, TimeSpan.Zero, 264));
                yield return new TestCaseData(PerftPosition.Position4, new PerftResult(3, TimeSpan.Zero, 9467));

                yield return new TestCaseData(PerftPosition.Position4, new PerftResult(4, TimeSpan.Zero, 422333));

                yield return new TestCaseData(PerftPosition.Position4, new PerftResult(5, TimeSpan.Zero, 15833292))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position4, new PerftResult(6, TimeSpan.Zero, 706045033))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position5, new PerftResult(1, TimeSpan.Zero, 42));

                yield return new TestCaseData(PerftPosition.Position5, new PerftResult(2, TimeSpan.Zero, 1352));

                yield return new TestCaseData(PerftPosition.Position5, new PerftResult(3, TimeSpan.Zero, 53392));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion
    }
}