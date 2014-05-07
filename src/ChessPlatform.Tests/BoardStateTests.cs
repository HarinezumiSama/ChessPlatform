using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
                        PerftPosition.Position3,
                        "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1"
                    },
                    {
                        PerftPosition.Position4,
                        "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1"
                    },
                    {
                        PerftPosition.Position5,
                        "rnbqkb1r/pp1p1ppp/2p5/4P3/2B5/8/PPP1NnPP/RNBQK2R w KQkq - 0 6"
                    },
                    {
                        PerftPosition.Position6,
                        "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10"
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
        public void TestKingInCheckByCloseQueenCase()
        {
            var boardState = new BoardState("r3k2r/p1ppqQ2/1n2p1pb/3PN2b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1");

            AssertBaseProperties(
                boardState,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(boardState, "e8-d8", "e7-f7");
        }

        [Test]
        public void TestKingInCheckByFarQueenAndPinnedOwnQueenCase1()
        {
            var boardState = new BoardState("r3k2r/p1ppq3/3n2Qb/3PR2b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1");

            AssertBaseProperties(
                boardState,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(boardState, "e8-d8", "e8-f8", "d6-f7", "h5-g6");
        }

        [Test]
        public void TestKingInCheckByFarQueenAndPinnedOwnQueenCase2()
        {
            var boardState = new BoardState("r3k2r/p1pp4/3nq1Qb/3PR2b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1");

            AssertBaseProperties(
                boardState,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(boardState, "e8-d8", "e8-f8", "e8-e7", "d6-f7", "h5-g6");
        }

        [Test]
        public void TestPinnedOwnQueenCase()
        {
            var boardState = new BoardState("k7/8/8/3q4/4Q3/5P2/5K2/8 b - - 0 1");

            AssertBaseProperties(
                boardState,
                PieceColor.Black,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Default);

            AssertValidMoves(boardState, "a8-b8", "a8-a7", "a8-b7", "d5-b7", "d5-c6", "d5-e4");
        }

        [Test]
        public void TestKingInCheckByFarQueenAndCanBlockByEnPassantB2B4Case()
        {
            var boardState = new BoardState("rnb1kbnr/pp1ppppp/2p5/q7/2PP4/8/PP2PPPP/RNBQKBNR w KQkq - 0 1");

            AssertBaseProperties(
                boardState,
                PieceColor.White,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(boardState, "d1-d2", "c1-d2", "b2-b4", "b1-c3", "b1-d2");
        }

        [Test]
        public void TestKingInDoubleCheckByQueenAndKnight()
        {
            var boardState = new BoardState("r3k2r/p1ppq3/1n1Np1Qb/3P3b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1");

            AssertBaseProperties(
                boardState,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.DoubleCheck);

            AssertValidMoves(boardState, "e8-d8", "e8-f8");
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
        public void TestPerft(PerftPosition perftPosition, ExpectedPerftResult expectedResult)
        {
            var fen = PerftPositionToFenMap[perftPosition];
            var boardState = new BoardState(fen);

            var flags = PerftFlags.None;

            var includeExtraCountTypes = expectedResult.CheckCount.HasValue || expectedResult.CheckmateCount.HasValue;
            if (includeExtraCountTypes)
            {
                flags |= PerftFlags.IncludeExtraCountTypes;
            }

            var actualResult = boardState.Perft(expectedResult.Depth, flags);

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

        private static void AssertPerftResult(PerftResult actualResult, ExpectedPerftResult expectedResult)
        {
            Assert.That(actualResult, Is.Not.Null);
            Assert.That(expectedResult, Is.Not.Null);

            Assert.That(actualResult.Depth, Is.EqualTo(expectedResult.Depth), "Depth mismatch.");
            Assert.That(actualResult.NodeCount, Is.EqualTo(expectedResult.NodeCount), "Node count mismatch.");

            if (expectedResult.CheckCount.HasValue)
            {
                Assert.That(
                    actualResult.CheckCount,
                    Is.EqualTo(expectedResult.CheckCount.Value),
                    "Check count mismatch.");
            }

            if (expectedResult.CheckmateCount.HasValue)
            {
                Assert.That(
                    actualResult.CheckmateCount,
                    Is.EqualTo(expectedResult.CheckmateCount.Value),
                    "Checkmate count mismatch.");
            }

            if (expectedResult.NodesPerSecond.HasValue)
            {
                Assert.That(
                    actualResult.NodesPerSecond,
                    Is.GreaterThanOrEqualTo(expectedResult.NodesPerSecond.Value),
                    "NPS is too low.");
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
            Position3,
            Position4,
            Position5,
            Position6
        }

        #endregion

        #region ExpectedPerftResult Class

        public sealed class ExpectedPerftResult
        {
            #region Constructors

            internal ExpectedPerftResult(int depth, ulong nodeCount)
            {
                #region Argument Check

                if (depth < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "depth",
                        depth,
                        @"The value cannot be negative.");
                }

                #endregion

                this.Depth = depth;
                this.NodeCount = nodeCount;
            }

            #endregion

            #region Public Properties

            public int Depth
            {
                get;
                private set;
            }

            public ulong NodeCount
            {
                get;
                private set;
            }

            public ulong? CheckCount
            {
                get;
                set;
            }

            public ulong? CheckmateCount
            {
                get;
                set;
            }

            public ulong? NodesPerSecond
            {
                get;
                set;
            }

            #endregion

            #region Public Methods

            public override string ToString()
            {
                const string NotSpecified = "n/a";

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{{ Depth = {0}, NodeCount = {1}, NPS = {2}, CheckCount = {3}, CheckmateCount = {4} }}",
                    this.Depth,
                    this.NodeCount,
                    this.NodesPerSecond.ToStringSafelyInvariant(NotSpecified),
                    this.CheckCount.ToStringSafelyInvariant(NotSpecified),
                    this.CheckmateCount.ToStringSafelyInvariant(NotSpecified));
            }

            #endregion
        }

        #endregion

        #region TestPerftForInitialPositionCases Class

        //// TODO [vmcl] Use Omnifactotum.NUnit once it's published
        public sealed class TestPerftForInitialPositionCases : IEnumerable<TestCaseData>
        {
            public IEnumerator<TestCaseData> GetEnumerator()
            {
                const string TooLongNow = "Move generation takes too much time now.";

                #region Initial

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(0, 1UL));

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(1, 20UL));

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(2, 400UL));

                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(3, 8902UL));

                // Targeting ~100,000 NPS (or better) for the first optimization
                yield return new TestCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(4, 197281UL) { NodesPerSecond = 100000 });

                yield return new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(5, 4865609UL));

                yield return new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(6, 119060324UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(7, 3195901860UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(8, 84998978956UL))
                    .MakeExplicit(TooLongNow);

                yield return
                    new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(9, 2439530234167UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(10, 69352859712417UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(11, 2097651003696806UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(12, 62854969236701747UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestCaseData(PerftPosition.Initial, new ExpectedPerftResult(13, 1981066775000396239UL))
                        .MakeExplicit(TooLongNow);

                #endregion

                #region Position2

                yield return new TestCaseData(PerftPosition.Position2, new ExpectedPerftResult(1, 48UL));
                yield return new TestCaseData(PerftPosition.Position2, new ExpectedPerftResult(2, 2039UL));
                yield return new TestCaseData(PerftPosition.Position2, new ExpectedPerftResult(3, 97862UL));
                yield return new TestCaseData(PerftPosition.Position2, new ExpectedPerftResult(4, 4085603UL));

                yield return new TestCaseData(PerftPosition.Position2, new ExpectedPerftResult(5, 193690690UL))
                    .MakeExplicit(TooLongNow);

                #endregion

                #region Position3

                yield return new TestCaseData(PerftPosition.Position3, new ExpectedPerftResult(1, 14UL));
                yield return new TestCaseData(PerftPosition.Position3, new ExpectedPerftResult(2, 191UL));
                yield return new TestCaseData(PerftPosition.Position3, new ExpectedPerftResult(3, 2812UL));
                yield return new TestCaseData(PerftPosition.Position3, new ExpectedPerftResult(4, 43238UL));
                yield return new TestCaseData(PerftPosition.Position3, new ExpectedPerftResult(5, 674624UL));

                yield return new TestCaseData(PerftPosition.Position3, new ExpectedPerftResult(6, 11030083UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position3, new ExpectedPerftResult(7, 178633661UL))
                    .MakeExplicit(TooLongNow);

                #endregion

                #region Position4

                yield return
                    new TestCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(1, 6UL) { CheckCount = 0, CheckmateCount = 0 });

                yield return
                    new TestCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(2, 264UL) { CheckCount = 10, CheckmateCount = 0 });

                yield return
                    new TestCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(3, 9467UL) { CheckCount = 38, CheckmateCount = 22 });

                yield return
                    new TestCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(4, 422333UL) { CheckCount = 15492 });

                yield return
                    new TestCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(5, 15833292UL) { CheckCount = 200568, CheckmateCount = 50562 })
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(6, 706045033UL) { CheckCount = 26973664, CheckmateCount = 81076 })
                        .MakeExplicit(TooLongNow);

                #endregion

                #region Position5

                yield return new TestCaseData(PerftPosition.Position5, new ExpectedPerftResult(1, 42UL));
                yield return new TestCaseData(PerftPosition.Position5, new ExpectedPerftResult(2, 1352UL));
                yield return new TestCaseData(PerftPosition.Position5, new ExpectedPerftResult(3, 53392UL));

                #endregion

                #region Position6

                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(1, 46UL));
                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(2, 2079UL));
                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(3, 89890UL));
                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(4, 3894594UL));

                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(5, 164075551UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(6, 6923051137UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(7, 287188994746UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(8, 11923589843526UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestCaseData(PerftPosition.Position6, new ExpectedPerftResult(9, 490154852788714UL))
                    .MakeExplicit(TooLongNow);

                #endregion
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion
    }
}