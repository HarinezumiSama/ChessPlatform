﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Omnifactotum;
using Omnifactotum.Annotations;

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
                        ChessConstants.DefaultInitialFen
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

        #region SetUp/TearDown

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Console.WriteLine($@"*** CLR version: {Environment.Version}");

            var coreAssemblyName = typeof(GameBoard).Assembly.GetName();
            Console.WriteLine($@"*** {coreAssemblyName.Name} assembly version: {coreAssemblyName.Version}");
        }

        #endregion

        #region Tests

        [Test]
        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(int.MinValue)]
        public void TestPerftForInvalidArgument(int depth)
        {
            var gameBoard = new GameBoard(PerformInternalBoardValidation);
            Assert.That(() => gameBoard.Perft(depth), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [TestCaseSource(typeof(TestPerftCases))]
        public void TestPerft(PerftPosition perftPosition, ExpectedPerftResult expectedResult)
        {
            var fen = PerftPositionToFenMap[perftPosition];
            var gameBoard = new GameBoard(fen, PerformInternalBoardValidation);

            var flags = PerftFlags.IncludeDivideMap; // | PerftFlags.DisableParallelism;

            var includeExtraCountTypes = expectedResult.CheckCount.HasValue || expectedResult.CheckmateCount.HasValue;
            if (includeExtraCountTypes)
            {
                flags |= PerftFlags.IncludeExtraCountTypes;
            }

            #region For Finding Bugs in Move Generator

            var extraMoves = new GameMove[]
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

            Console.WriteLine(
                @"[{0}] ({1}) {2} {{ {3} }} ({4}) : {5}{6}",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                ChessHelper.GetPlatformVersion(true),
                perftPosition.GetName(),
                fen,
                actualResult.Flags,
                actualResult,
                extraInfo);

            Console.WriteLine();

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

            if (expectedResult.CaptureCount.HasValue)
            {
                Assert.That(
                    actualResult.CaptureCount,
                    Is.EqualTo(expectedResult.CaptureCount),
                    "Capture count mismatch.");
            }

            if (expectedResult.EnPassantCaptureCount.HasValue)
            {
                Assert.That(
                    actualResult.EnPassantCaptureCount,
                    Is.EqualTo(expectedResult.EnPassantCaptureCount),
                    "En passant capture count mismatch.");
            }

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
                        nameof(depth),
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
            }

            public ulong NodeCount
            {
                get;
            }

            public ulong? CaptureCount
            {
                get;
                set;
            }

            public ulong? EnPassantCaptureCount
            {
                get;
                set;
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

            #endregion

            #region Public Methods

            public override string ToString()
            {
                var resultBuilder = new StringBuilder();

                resultBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{{ Depth = {0}, NodeCount = {1}",
                    this.Depth,
                    this.NodeCount);

                WriteProperty(resultBuilder, obj => obj.CaptureCount);
                WriteProperty(resultBuilder, obj => obj.EnPassantCaptureCount);
                WriteProperty(resultBuilder, obj => obj.CheckCount);
                WriteProperty(resultBuilder, obj => obj.CheckmateCount);

                resultBuilder.Append(" }");

                return resultBuilder.ToString();
            }

            #endregion

            #region Private Methods

            private void WriteProperty<T>(
                [NotNull] StringBuilder stringBuilder,
                [NotNull] Expression<Func<ExpectedPerftResult, T?>> expression)
                where T : struct
            {
                var value = expression.Compile().Invoke(this);
                if (!value.HasValue)
                {
                    return;
                }

                stringBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    ", {0} = {1}",
                    Factotum.For<ExpectedPerftResult>.GetPropertyName(expression),
                    value.Value);
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
            }

            public ExpectedPerftResult ExpectedResult
            {
                get;
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
                    new ExpectedPerftResult(0, 1UL)
                    {
                        CaptureCount = 0,
                        EnPassantCaptureCount = 0,
                        CheckCount = 0,
                        CheckmateCount = 0
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(1, 20UL)
                    {
                        CaptureCount = 0,
                        EnPassantCaptureCount = 0,
                        CheckCount = 0,
                        CheckmateCount = 0
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(2, 400UL)
                    {
                        CaptureCount = 0,
                        EnPassantCaptureCount = 0,
                        CheckCount = 0,
                        CheckmateCount = 0
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(3, 8902UL)
                    {
                        CaptureCount = 34,
                        EnPassantCaptureCount = 0,
                        CheckCount = 12,
                        CheckmateCount = 0
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Initial,
                    new ExpectedPerftResult(4, 197281UL)
                    {
                        CaptureCount = 1576,
                        EnPassantCaptureCount = 0,
                        CheckCount = 469,
                        CheckmateCount = 8
                    });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Initial,
                        new ExpectedPerftResult(5, 4865609UL)
                        {
                            CaptureCount = 82719,
                            EnPassantCaptureCount = 258
                        });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Initial,
                        new ExpectedPerftResult(6, 119060324UL)
                        {
                            CaptureCount = 2812008,
                            EnPassantCaptureCount = 5248
                        })
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
                    new ExpectedPerftResult(1, 48UL)
                    {
                        CaptureCount = 8,
                        EnPassantCaptureCount = 0,
                        CheckCount = 0,
                        CheckmateCount = 0
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(2, 2039UL)
                    {
                        CaptureCount = 351,
                        EnPassantCaptureCount = 1,
                        CheckCount = 3,
                        CheckmateCount = 0
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(3, 97862UL)
                    {
                        CaptureCount = 17102,
                        EnPassantCaptureCount = 45,
                        CheckCount = 993,
                        CheckmateCount = 1
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(4, 4085603UL)
                    {
                        CaptureCount = 757163,
                        EnPassantCaptureCount = 1929
                    });

                yield return new TestPerftCaseData(
                    PerftPosition.Position2,
                    new ExpectedPerftResult(5, 193690690UL)
                    {
                        CaptureCount = 35043416,
                        EnPassantCaptureCount = 73365
                    })
                    .MakeExplicit(TooLongNow);

                #endregion

                #region Position3

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position3,
                        new ExpectedPerftResult(1, 14UL) { CaptureCount = 1, EnPassantCaptureCount = 0 });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position3,
                        new ExpectedPerftResult(2, 191UL) { CaptureCount = 14, EnPassantCaptureCount = 0 });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position3,
                        new ExpectedPerftResult(3, 2812UL) { CaptureCount = 209, EnPassantCaptureCount = 2 });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position3,
                        new ExpectedPerftResult(4, 43238UL) { CaptureCount = 3348, EnPassantCaptureCount = 123 });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position3,
                        new ExpectedPerftResult(5, 674624UL) { CaptureCount = 52051, EnPassantCaptureCount = 1165 });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position3,
                        new ExpectedPerftResult(6, 11030083UL)
                        {
                            CaptureCount = 940350,
                            EnPassantCaptureCount = 33325
                        });

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position3,
                        new ExpectedPerftResult(7, 178633661UL)
                        {
                            CaptureCount = 14519036,
                            EnPassantCaptureCount = 294874
                        })
                        .MakeExplicit(TooLongNow);

                #endregion

                #region Position4 and MirroredPosition4

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(5, 15833292UL)
                        {
                            CaptureCount = 2046173,
                            EnPassantCaptureCount = 6512
                        });

                var mirroredPositions = new[] { PerftPosition.Position4, PerftPosition.MirroredPosition4 };
                foreach (var position in mirroredPositions)
                {
                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(1, 6UL)
                        {
                            CaptureCount = 0,
                            EnPassantCaptureCount = 0,
                            CheckCount = 0,
                            CheckmateCount = 0
                        });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(2, 264UL)
                        {
                            CaptureCount = 87,
                            EnPassantCaptureCount = 0,
                            CheckCount = 10,
                            CheckmateCount = 0
                        });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(3, 9467UL)
                        {
                            CaptureCount = 1021,
                            EnPassantCaptureCount = 4,
                            CheckCount = 38,
                            CheckmateCount = 22
                        });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(4, 422333UL)
                        {
                            CaptureCount = 131393,
                            EnPassantCaptureCount = 0,
                            CheckCount = 15492,
                            CheckmateCount = 5
                        });

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(5, 15833292UL)
                        {
                            CaptureCount = 2046173,
                            EnPassantCaptureCount = 6512,
                            CheckCount = 200568,
                            CheckmateCount = 50562
                        })
                        .MakeExplicit(TooLongNow);

                    yield return new TestPerftCaseData(
                        position,
                        new ExpectedPerftResult(6, 706045033UL)
                        {
                            CaptureCount = 210369132,
                            EnPassantCaptureCount = 212,
                            CheckCount = 26973664,
                            CheckmateCount = 81076
                        })
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