using System;
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
        public void TestPerft(PerftPosition perftPosition, bool enableParallelism, ExpectedPerftResult expectedResult)
        {
            var fen = PerftPositionToFenMap[perftPosition];
            var gameBoard = new GameBoard(fen, PerformInternalBoardValidation);

            var flags = PerftFlags.IncludeDivideMap;
            if (enableParallelism)
            {
                flags |= PerftFlags.EnableParallelism;
            }

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
                    .Select(pair => $@"  {pair.Key} -> {pair.Value}")
                    .Join(Environment.NewLine);

                extraInfo =
                    $@"{Environment.NewLine}Divide ({actualResult.DividedMoves.Count}):{Environment.NewLine}{
                        divideResult}";
            }

            Console.WriteLine(
                @"[{0}] ({1}) {2} {{ {3} }} ({4}) : {5}{6}",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                ChessHelper.PlatformVersion,
                perftPosition.GetName(),
                fen,
                actualResult.Flags,
                actualResult,
                extraInfo);

            Console.WriteLine();

            AssertPerftResult(actualResult, expectedResult);
        }

        [Test]
        [TestCase("6rk/pp3p1p/3p2pb/3Pp3/2P3P1/1P1b1P2/P6P/R3R2K b - - 3 22", 1, 8, Explicit = true)]
        [TestCase("r1b2rk1/1p3pp1/p1nbqn1p/8/3Pp2N/1NP1B1P1/PPQ2PBP/R3K2R b KQ - 2 15", 2, 3)]
        public void TestPerftSpecificCases(string fen, int startDepth, int endDepth)
        {
            for (var currentDepth = startDepth; currentDepth <= endDepth; currentDepth++)
            {
                var board = new GameBoard(fen, PerformInternalBoardValidation);
                const PerftFlags Flags = PerftFlags.EnableParallelism | PerftFlags.IncludeDivideMap;
                var perftResult = board.Perft(currentDepth, Flags);

                string extraInfo = null;
                if (perftResult.Flags.HasFlag(PerftFlags.IncludeDivideMap))
                {
                    var divideResult = perftResult
                        .DividedMoves
                        .OrderBy(pair => pair.Key.ToString())
                        .Select(pair => $@"  {pair.Key} -> {pair.Value}")
                        .Join(Environment.NewLine);

                    extraInfo =
                        $@"{Environment.NewLine}Divide ({perftResult.DividedMoves.Count}):{Environment.NewLine}{
                            divideResult}";
                }

                Console.WriteLine(
                    $@"[{MethodBase.GetCurrentMethod().GetQualifiedName()}] ({ChessHelper.PlatformVersion
                        }) {{ {fen} }} ({perftResult.Flags}) : {perftResult}{extraInfo}");

                Console.WriteLine();
            }

            /*
*-------------------------------------------------------------*
| Chess Platform UI for Desktop 0.1.0.312 (rev. ec3b5f331493) |
*-------------------------------------------------------------*

------------------------------------------------------------------------------------------------------------------------
ChessPlatform.UI.Desktop.exe Information: 0 : [SmartEnoughPlayer.DoGetMove] Color: Black, max depth: 8 plies, max time: unlimited, multi CPU: True, FEN: "6rk/pp3p1p/3p2pb/3Pp3/2P3P1/1P1b1P2/P6P/R3R2K b - - 3 22".
ChessPlatform.UI.Desktop.exe Information: 0 : [SmartEnoughPlayer.DoGetMoveInternal] Number of available moves: 30.

ChessPlatform.UI.Desktop.exe Information: 0 : [SmartEnoughPlayer.DoGetMoveInternal] Fixed depth: 8.
ChessPlatform.UI.Desktop.exe Warning: 0 : [SimpleTranspositionTable..ctor] The transposition table is DISABLED.
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #13/30] Bf5: -25, time: 0:00:24.5185544
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #01/30] Bxc4: -51, time: 0:00:55.8890279
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #21/30] Bf8: -49, time: 0:01:20.2642979
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #17/30] Rg7: 161, time: 0:01:22.6777899
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #02/30] Bf1: -134, time: 0:00:28.6191033
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #25/30] a5: 242, time: 0:01:33.8772758
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #14/30] e4: 107, time: 0:01:13.0023309
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #03/30] Be2: -141, time: 0:00:23.3566278
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #28/30] a6: 226, time: 0:01:56.0697976
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #09/30] g5: 193, time: 0:02:05.1629138
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #15/30] f5: 232, time: 0:00:35.4808219
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #05/30] Bd2: 154, time: 0:02:36.6748482
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #04/30] Bc1: -176, time: 0:00:55.4954289
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #10/30] Bb1: -137, time: 0:00:43.4778715
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #06/30] Be3: -404, time: 0:00:12.4505468
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #26/30] b6: 221, time: 0:01:48.0770289
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #18/30] Bg7: 25, time: 0:02:07.6889404
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #16/30] Kg7: 152, time: 0:01:31.2442893
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #22/30] Re8: 187, time: 0:02:26.9121297
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #29/30] Rb8: 182, time: 0:02:26.0801797
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #23/30] b5: 208, time: 0:00:47.184861
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #11/30] Bc2: 192, time: 0:01:49.5059476
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #12/30] Be4: -160, time: 0:00:18.8098226
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #19/30] f6: 221, time: 0:01:38.7527853
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #07/30] Bf4: 223, time: 0:02:26.0766087
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #30/30] Ra8: 175, time: 0:01:53.1468655
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #20/30] Rf8: 210, time: 0:01:22.7724898
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #27/30] Rc8: 194, time: 0:03:13.427106
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #24/30] Rd8: 201, time: 0:02:02.0335471
ChessPlatform.UI.Desktop.exe Information: 0 : [AnalyzeRootMoveInternal #08/30] Bg5: 223, time: 0:01:25.5120915
ChessPlatform.UI.Desktop.exe Information: 0 : [SmartEnoughPlayerMoveChooser.ComputeAlphaBetaRoot] Best move a5: 242.

PVs ordered by score:
  { 242 : a5, Rg1, Bf4, Rg2, e4, fxe4, Bxe4, Rg1 }
  { 232 : f5, gxf5, gxf5, Rg1, Bg5, Rg2, Be3, Re1 }
  { 226 : a6, Rad1, Bc2, Rc1, Bxc1, Rxc1, Bd3, h4 }
  { 223 : Bf4, Rad1, Bc2, Rc1, Bd3, Rc3, Bd2, Rxd3 }
  { 223 : Bg5, Rad1, Bc2, Rc1, Bd3, Rc3, Bd2, Rxd3 }
  { 221 : b6, Rg1, Bf4, Rg2, e4, fxe4, Bxe4, Rg1 }
  { 221 : f6, Rg1, Bf4, Rg2, e4, fxe4, Bxe4, Rg1 }
  { 210 : Rf8, Rad1, Bc2, Rc1, Bd3, Rcd1, Bc2, b4 }
  { 208 : b5, cxb5, Bxb5, Rad1, Bf4, Re4, f5, gxf5 }
  { 201 : Rd8, Rad1, Bc2, Rc1, Bd3, Rcd1, Bc2, b4 }
  { 194 : Rc8, Rad1, Bc2, Rc1, Bd3, Rc3, Bd2, Rxd3 }
  { 193 : g5, Rad1, Bg6, c5, Bf8, Re3, Be7, Rg1 }
  { 192 : Bc2, Rac1, Bxc1, Rxc1, Bd3, Rc3, e4, h4 }
  { 187 : Re8, Rad1, Bc2, Rc1, Bxc1, Rxc1, Bd3, h4 }
  { 182 : Rb8, Rad1, Bc2, Rc1, Bd3, Rcd1, Bc2, b4 }
  { 175 : Ra8, Rad1, Bc2, Rc1, Bd3, Rcd1, Bc2, b4 }
  { 161 : Rg7, c5, dxc5, Rxe5, Bf4, Re8+, Rg8, Rxg8+ }
  { 154 : Bd2, Red1, Bc3, Rxd3, Bxa1, a4, f5, h4 }
  { 152 : Kg7, c5, dxc5, Rxe5, Kh8, Re7, Bg7, Rg1 }
  { 107 : e4, Rxe4, Bxe4, fxe4, Re8, Re1, f5, gxf5 }
  { 25 : Bg7, Rad1, e4, fxe4, Bc2, Rd2, Bc3, Rxc2 }
  { -25 : Bf5, gxf5, gxf5, Rg1, Bg5, Rg2, Rg6, Rag1 }
  { -49 : Bf8, Rad1, Bc2, Rd2, Bf5, gxf5, Bh6, Rg2 }
  { -51 : Bxc4, bxc4, Bd2, Re2, Bf4, Rb1, b6, Rg1 }
  { -134 : Bf1, Rxf1, Bf4, Rf2, g5, Rg1, Rg6, Rg3 }
  { -137 : Bb1, Raxb1, Bf4, Rb2, g5, Re4, Rg6, Rg2 }
  { -141 : Be2, Rxe2, Bf4, Rg1, g5, Rg3, Bxg3, hxg3 }
  { -160 : Be4, fxe4, Bf4, Rg1, g5, Rg2, Rg6, Rag1 }
  { -176 : Bc1, Raxc1, f5, c5, fxg4, fxg4, dxc5, Rxe5 }
  { -404 : Be3, Rxe3, Bc2, Rc1, Bf5, gxf5, gxf5, Rg1 }

ChessPlatform.UI.Desktop.exe Information: 0 : [SmartEnoughPlayerMoveChooser.GetBestMove] Result: { 242 : a5, Rg1, Bf4, Rg2, e4, fxe4, Bxe4, Rg1 }, depth 8, time: 00:06:40.7191082, FEN "6rk/pp3p1p/3p2pb/3Pp3/2P3P1/1P1b1P2/P6P/R3R2K b - - 3 22".
ChessPlatform.UI.Desktop.exe Information: 0 : [SmartEnoughPlayer.DoGetMove] Result: { 242 : a5, Rg1, Bf4, Rg2, e4, fxe4, Bxe4, Rg1 }, depth 8, time: 0:06:40.7198951, 59101630 nodes (147489 NPS), FEN "6rk/pp3p1p/3p2pb/3Pp3/2P3P1/1P1b1P2/P6P/R3R2K b - - 3 22".
------------------------------------------------------------------------------------------------------------------------

Stockfish 6 64 POPCNT by Tord Romstad, Marco Costalba and Joona Kiiski

position fen 6rk/pp3p1p/3p2pb/3Pp3/2P3P1/1P1b1P2/P6P/R3R2K b - - 3 22
go depth 8
info depth 1 seldepth 1 multipv 1 score cp 255 nodes 36 nps 7200 tbhits 0 time 5 pv h6d2
info depth 2 seldepth 2 multipv 1 score cp 336 nodes 77 nps 12833 tbhits 0 time 6 pv h6d2 a2a3 d2e1 a1e1
info depth 3 seldepth 3 multipv 1 score cp 336 nodes 121 nps 20166 tbhits 0 time 6 pv h6d2 a2a3 d2e1
info depth 4 seldepth 4 multipv 1 score cp 336 nodes 176 nps 25142 tbhits 0 time 7 pv h6d2 a2a3 d2e1 a1e1
info depth 5 seldepth 5 multipv 1 score cp 342 nodes 504 nps 63000 tbhits 0 time 8 pv h6d2 a1d1 d2e1 d1e1 f7f5
info depth 6 seldepth 7 multipv 1 score cp 208 nodes 3153 nps 286636 tbhits 0 time 11 pv h6f4 c4c5 d6c5 h1g2 f7f5 d5d6
info depth 7 seldepth 8 multipv 1 score cp 209 nodes 4542 nps 324428 tbhits 0 time 14 pv f7f5 h1g2 f5g4 f3g4 h6d2 e1d1 d3e4 g2h3
info depth 8 seldepth 12 multipv 1 score cp 262 nodes 8034 nps 446333 tbhits 0 time 18 pv h6d2 c4c5 d2e1 a1e1 d6c5 h1g2 f7f5 e1e5
bestmove h6d2 ponder c4c5

            */
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

                Depth = depth;
                NodeCount = nodeCount;
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
                    Depth,
                    NodeCount);

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

            internal TestPerftCaseData(
                PerftPosition position,
                [NotNull] ExpectedPerftResult expectedResult,
                bool enableParallelism = true)
                : base(position, enableParallelism, expectedResult)
            {
                Assert.That(expectedResult, Is.Not.Null);

                Position = position;
                ExpectedResult = expectedResult;
                EnableParallelism = enableParallelism;
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

            public bool EnableParallelism
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

                yield return
                    new TestPerftCaseData(
                        PerftPosition.Position4,
                        new ExpectedPerftResult(5, 15833292UL)
                        {
                            CaptureCount = 2046173,
                            EnPassantCaptureCount = 6512
                        },
                        false);

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