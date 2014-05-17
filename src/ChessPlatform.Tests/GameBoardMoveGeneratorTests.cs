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
    public sealed class GameBoardMoveGeneratorTests : GameBoardTestBase
    {
        #region Constants and Fields

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
                        PerftPosition.MirroredPosition4,
                        "r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1"
                    },
                    {
                        PerftPosition.Position5,
                        "rnbqkb1r/pp1p1ppp/2p5/4P3/2B5/8/PPP1NnPP/RNBQK2R w KQkq - 0 6"
                    },
                    {
                        PerftPosition.Position6,
                        "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10"
                    },
                    {
                        PerftPosition.WideOpen,
                        "rnbqkbnr/8/8/8/8/8/8/RNBQKBNR w KQkq - 0 1"
                    },
                    {
                        PerftPosition.KingAndPawns,
                        "4k3/pppppppp/8/8/8/8/PPPPPPPP/4K3 w - - 0 1"
                    },
                    {
                        PerftPosition.KingAndRooks,
                        "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1"
                    },
                    {
                        PerftPosition.KingAndBishops,
                        "2b1kb2/8/8/8/8/8/8/2B1KB2 w - - 0 1"
                    }
                });

        #endregion

        #region Tests

        [Test]
        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(int.MinValue)]
        public void TestPerftForInvalidArgument(int depth)
        {
            var gameBoard = new GameBoard();
            Assert.That(() => gameBoard.Perft(depth), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [TestCaseSource(typeof(TestPerftCases))]
        public void TestPerft(PerftPosition perftPosition, ExpectedPerftResult expectedResult)
        {
            var fen = PerftPositionToFenMap[perftPosition];
            var gameBoard = new GameBoard(fen);

            var flags = PerftFlags.IncludeDivideMap;

            var includeExtraCountTypes = expectedResult.CheckCount.HasValue || expectedResult.CheckmateCount.HasValue;
            if (includeExtraCountTypes)
            {
                flags |= PerftFlags.IncludeExtraCountTypes;
            }

            #region For Finding Bugs in Move Generator

            var extraMoves = new PieceMove[]
            {
            };

            gameBoard = extraMoves.Aggregate(gameBoard, (current, extraMove) => current.MakeMove(extraMove));

            #endregion

            var actualResult = gameBoard.Perft(expectedResult.Depth - extraMoves.Length, flags);

            string extraInfo = null;
            if (actualResult.Flags.HasFlag(PerftFlags.IncludeDivideMap))
            {
                var divideResult = actualResult
                    .DividedMoves
                    .OrderBy(pair => pair.Key.ToString())
                    .Select(pair => string.Format(CultureInfo.InvariantCulture, "  {0} -> {1}", pair.Key, pair.Value))
                    .Join(Environment.NewLine);

                extraInfo = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}Divide ({1}):{0}{2}",
                    Environment.NewLine,
                    actualResult.DividedMoves.Count,
                    divideResult);
            }

            Trace.TraceInformation(
                "[{0}] {1} {{ {2} }} : {3}{4}",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                perftPosition.GetName(),
                fen,
                actualResult,
                extraInfo);

            AssertPerftResult(actualResult, expectedResult);
        }

        #endregion

        #region Private Methods

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
            MirroredPosition4,
            Position5,
            Position6,
            WideOpen,
            KingAndPawns,
            KingAndRooks,
            KingAndBishops
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

        #region TestPerftCaseData Class

        public sealed class TestPerftCaseData : TestCaseData
        {
            #region Constructors

            internal TestPerftCaseData(PerftPosition position, ExpectedPerftResult expectedResult)
                : base(position, expectedResult)
            {
                Assert.That(expectedResult, Is.Not.Null);

                this.Position = position;
                this.ExpectedResult = expectedResult;
            }

            #endregion

            #region Public Properties

            public PerftPosition Position
            {
                get;
                private set;
            }

            public ExpectedPerftResult ExpectedResult
            {
                get;
                private set;
            }

            #endregion

            #region Public Methods

            public new TestPerftCaseData MakeExplicit(string reason)
            {
                base.MakeExplicit(reason);
                return this;
            }

            #endregion
        }

        #endregion

        #region TestPerftCases Class

        public sealed class TestPerftCases : IEnumerable<TestCaseData>
        {
            #region IEnumerable<TestCaseData> Members

            public IEnumerator<TestCaseData> GetEnumerator()
            {
                var cases = GetCases()
                    .OrderBy(
                        item =>
                            item.ExpectedResult.CheckCount.HasValue || item.ExpectedResult.CheckmateCount.HasValue
                                ? 1
                                : 0)
                    .ThenBy(item => item.ExpectedResult.NodeCount)
                    .ThenBy(item => item.ExpectedResult.Depth)
                    .ThenBy(item => item.Position.ToString());

                return cases.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region Private Methods

            private static IEnumerable<TestPerftCaseData> GetCases()
            {
                const string TooLongNow = "Move generation takes too much time now.";

                #region Initial

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(0, 1UL) { CheckCount = 0, CheckmateCount = 0 });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(1, 20UL) { CheckCount = 0, CheckmateCount = 0 });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(2, 400UL) { CheckCount = 0, CheckmateCount = 0 });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(3, 8902UL) { CheckCount = 12, CheckmateCount = 0 });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(4, 197281UL) { CheckCount = 469, CheckmateCount = 8 });

                // Targeting ~100,000 NPS (or better) for the first optimization
                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(4, 197281UL) { NodesPerSecond = 100000 });

                yield return new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(5, 4865609UL));

                yield return new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(6, 119060324UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(7, 3195901860UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(8, 84998978956UL))
                    .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(9, 2439530234167UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(10, 69352859712417UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(11, 2097651003696806UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(12, 62854969236701747UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Initial, new ExpectedPerftResult(13, 1981066775000396239UL))
                        .MakeExplicit(TooLongNow);

                #endregion

                #region Position2

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(1, 48UL) { CheckCount = 0, CheckmateCount = 0 });

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(2, 2039UL) { CheckCount = 3, CheckmateCount = 0 });

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(3, 97862UL) { CheckCount = 993, CheckmateCount = 1 });

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(4, 4085603UL));

                yield return new TestPerftCaseData(PerftPosition.Position2, new ExpectedPerftResult(5, 193690690UL))
                    .MakeExplicit(TooLongNow);

                #endregion

                #region Position3

                yield return new TestPerftCaseData(PerftPosition.Position3, new ExpectedPerftResult(1, 14UL));
                yield return new TestPerftCaseData(PerftPosition.Position3, new ExpectedPerftResult(2, 191UL));
                yield return new TestPerftCaseData(PerftPosition.Position3, new ExpectedPerftResult(3, 2812UL));
                yield return new TestPerftCaseData(PerftPosition.Position3, new ExpectedPerftResult(4, 43238UL));
                yield return new TestPerftCaseData(PerftPosition.Position3, new ExpectedPerftResult(5, 674624UL));
                yield return new TestPerftCaseData(PerftPosition.Position3, new ExpectedPerftResult(6, 11030083UL));

                yield return new TestPerftCaseData(PerftPosition.Position3, new ExpectedPerftResult(7, 178633661UL))
                    .MakeExplicit(TooLongNow);

                #endregion

                #region Position4 and MirroredPosition4

                var mirroredPositions = new[] { PerftPosition.Position4, PerftPosition.MirroredPosition4 };
                foreach (var position in mirroredPositions)
                {
                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(1, 6UL) { CheckCount = 0, CheckmateCount = 0 });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(2, 264UL) { CheckCount = 10, CheckmateCount = 0 });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(3, 9467UL) { CheckCount = 38, CheckmateCount = 22 });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(4, 422333UL) { CheckCount = 15492, CheckmateCount = 5 });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(5, 15833292UL) { CheckCount = 200568, CheckmateCount = 50562 })
                        .MakeExplicit(TooLongNow);

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(6, 706045033UL) { CheckCount = 26973664, CheckmateCount = 81076 })
                        .MakeExplicit(TooLongNow);
                }

                #endregion

                #region Position5

                yield return new TestPerftCaseData(PerftPosition.Position5, new ExpectedPerftResult(1, 42UL));
                yield return new TestPerftCaseData(PerftPosition.Position5, new ExpectedPerftResult(2, 1352UL));
                yield return new TestPerftCaseData(PerftPosition.Position5, new ExpectedPerftResult(3, 53392UL));

                #endregion

                #region Position6

                yield return new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(1, 46UL));
                yield return new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(2, 2079UL));
                yield return new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(3, 89890UL));
                yield return new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(4, 3894594UL));

                yield return new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(5, 164075551UL))
                    .MakeExplicit(TooLongNow);

                yield return new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(6, 6923051137UL))
                    .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(7, 287188994746UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(8, 11923589843526UL))
                        .MakeExplicit(TooLongNow);

                yield return
                    new TestPerftCaseData(PerftPosition.Position6, new ExpectedPerftResult(9, 490154852788714UL))
                        .MakeExplicit(TooLongNow);

                #endregion

                #region WideOpen

                yield return new TestPerftCaseData(PerftPosition.WideOpen, new ExpectedPerftResult(1, 50UL));
                yield return new TestPerftCaseData(PerftPosition.WideOpen, new ExpectedPerftResult(2, 2125UL));
                yield return new TestPerftCaseData(PerftPosition.WideOpen, new ExpectedPerftResult(3, 96062UL));
                yield return new TestPerftCaseData(PerftPosition.WideOpen, new ExpectedPerftResult(4, 4200525UL));

                yield return new TestPerftCaseData(PerftPosition.WideOpen, new ExpectedPerftResult(5, 191462298UL))
                    .MakeExplicit(TooLongNow);

                #endregion

                #region KingAndPawns

                yield return new TestPerftCaseData(PerftPosition.KingAndPawns, new ExpectedPerftResult(1, 18UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndPawns, new ExpectedPerftResult(2, 324UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndPawns, new ExpectedPerftResult(3, 5658UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndPawns, new ExpectedPerftResult(4, 98766UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndPawns, new ExpectedPerftResult(5, 1683597UL));

                yield return new TestPerftCaseData(PerftPosition.KingAndPawns, new ExpectedPerftResult(6, 28677387UL))
                    .MakeExplicit(TooLongNow);

                #endregion

                #region KingAndRooks

                yield return new TestPerftCaseData(PerftPosition.KingAndRooks, new ExpectedPerftResult(1, 26UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndRooks, new ExpectedPerftResult(2, 568UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndRooks, new ExpectedPerftResult(3, 13744UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndRooks, new ExpectedPerftResult(4, 314346UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndRooks, new ExpectedPerftResult(5, 7594526UL));

                yield return
                    new TestPerftCaseData(PerftPosition.KingAndRooks, new ExpectedPerftResult(6, 179862938UL))
                        .MakeExplicit(TooLongNow);

                #endregion

                #region KingAndBishops

                yield return new TestPerftCaseData(PerftPosition.KingAndBishops, new ExpectedPerftResult(1, 18UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndBishops, new ExpectedPerftResult(2, 305UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndBishops, new ExpectedPerftResult(3, 5575UL));
                yield return new TestPerftCaseData(PerftPosition.KingAndBishops, new ExpectedPerftResult(4, 99932UL));
                yield return
                    new TestPerftCaseData(PerftPosition.KingAndBishops, new ExpectedPerftResult(5, 1879563UL));

                yield return
                    new TestPerftCaseData(PerftPosition.KingAndBishops, new ExpectedPerftResult(6, 34853962UL))
                        .MakeExplicit(TooLongNow);

                #endregion
            }

            #endregion
        }

        #endregion
    }
}