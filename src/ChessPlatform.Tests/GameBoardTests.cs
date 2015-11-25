using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class GameBoardTests : GameBoardTestBase
    {
        #region Tests

        [Test]
        public void TestDefaultConstruction()
        {
            var gameBoard = new GameBoard(PerformInternalBoardValidation);
            AssertDefaultInitialBoard(gameBoard);
        }

        [Test]
        public void TestConstructionByDefaultFen()
        {
            var gameBoard = new GameBoard(ChessConstants.DefaultInitialFen, PerformInternalBoardValidation);
            AssertDefaultInitialBoard(gameBoard);
        }

        [Test]
        public void TestConstructionByStalemateFen()
        {
            const string Fen = "k7/8/1Q6/8/8/8/8/7K b - - 0 1";

            var gameBoard = new GameBoard(Fen, PerformInternalBoardValidation);
            AssertBaseProperties(gameBoard, PieceColor.Black, CastlingOptions.None, null, 0, 1, GameState.Stalemate);
            AssertNoValidMoves(gameBoard);
        }

        [Test]
        [TestCase(PieceColor.White)]
        [TestCase(PieceColor.Black)]
        public void TestTwoKingsTooCloseToEachOther(PieceColor activeColor)
        {
            var fen = string.Format(
                CultureInfo.InvariantCulture,
                "k7/K7/8/8/8/8/8/8 {0} - - 0 1",
                activeColor.GetFenSnippet());

            Assert.That(
                () => new GameBoard(fen, PerformInternalBoardValidation),
                Throws.TypeOf<ChessPlatformException>());
        }

        [Test]
        public void TestMakeMoveBasicScenario()
        {
            var gameBoard1W = TestMakeMoveBasicScenario1W();
            var gameBoard1B = TestMakeMoveBasicScenario1B(gameBoard1W);
            var gameBoard2W = TestMakeMoveBasicScenario2W(gameBoard1B);
            var gameBoard2B = TestMakeMoveBasicScenario2B(gameBoard2W);
            var gameBoard3W = TestMakeMoveBasicScenario3W(gameBoard2B);
            var gameBoard3B = TestMakeMoveBasicScenario3B(gameBoard3W);
            Assert.That(gameBoard3B, Is.Not.Null);
        }

        [Test]
        public void TestMakeMoveFoolsMateScenario()
        {
            var gameBoard1W = new GameBoard(PerformInternalBoardValidation);
            var gameBoard1B = TestMakeMoveFoolsMateScenario1B(gameBoard1W);
            var gameBoard2W = TestMakeMoveFoolsMateScenario2W(gameBoard1B);
            var gameBoard2B = TestMakeMoveFoolsMateScenario2B(gameBoard2W);
            TestMakeMoveFoolsMateScenario3W(gameBoard2B);
        }

        [Test]
        public void TestTwoKingsOnlyCase()
        {
            var gameBoard = new GameBoard("k7/8/K7/8/8/8/8/8 b - - 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Default,
                AutoDrawType.InsufficientMaterial);

            AssertValidMoves(gameBoard, "a8-b8");
        }

        [Test]
        public void TestKingInCheckByCloseQueenCase()
        {
            var gameBoard = new GameBoard(
                "r3k2r/p1ppqQ2/1n2p1pb/3PN2b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1",
                PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "e8-d8", "e7-f7");
        }

        [Test]
        public void TestKingInCheckByFarQueenAndPinnedOwnQueenCase1()
        {
            var gameBoard = new GameBoard(
                "r3k2r/p1ppq3/3n2Qb/3PR2b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1",
                PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "e8-d8", "e8-f8", "d6-f7", "h5-g6");
        }

        [Test]
        public void TestKingInCheckByFarQueenAndPinnedOwnQueenCase2()
        {
            var gameBoard = new GameBoard(
                "r3k2r/p1pp4/3nq1Qb/3PR2b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1",
                PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "e8-d8", "e8-f8", "e8-e7", "d6-f7", "h5-g6");
        }

        [Test]
        public void TestPinnedOwnQueenCase()
        {
            var gameBoard = new GameBoard("k7/8/8/3q4/4Q3/5P2/5K2/8 b - - 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Default);

            AssertValidMoves(gameBoard, "a8-b8", "a8-a7", "a8-b7", "d5-b7", "d5-c6", "d5-e4");
        }

        [Test]
        public void TestKingInCheckByFarQueenAndCanBlockByEnPassantB2B4Case()
        {
            var gameBoard = new GameBoard(
                "rnb1kbnr/pp1ppppp/2p5/q7/2PP4/8/PP2PPPP/RNBQKBNR w KQkq - 0 1",
                PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.White,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "d1-d2", "c1-d2", "b2-b4", "b1-c3", "b1-d2");
        }

        [Test]
        public void TestKingInDoubleCheckByQueenAndKnight()
        {
            var gameBoard = new GameBoard(
                "r3k2r/p1ppq3/1n1Np1Qb/3P3b/1p2P3/2N4p/PPPBBPPP/R3K2R b KQkq - 0 1",
                PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.DoubleCheck);

            AssertValidMoves(gameBoard, "e8-d8", "e8-f8");
        }

        [Test]
        public void TestCanCaptureCheckingPawnByEnPassantCapture()
        {
            var gameBoard = new GameBoard(
                "8/8/3p4/1Pp3kr/1K3p2/1R6/4P1P1/8 w - c6 0 1",
                PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.White,
                CastlingOptions.None,
                new EnPassantCaptureInfo("c6", "c5"),
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "b4-a5", "b4-a4", "b4-a3", "b4-c3", "b4-c4", "b5-c6");
        }

        [Test]
        public void TestCannotCaptureEnPassantPawnByPinnedPawn()
        {
            var gameBoard = new GameBoard("8/2p5/3p4/KP5r/1R2Pp1k/8/6P1/8 b - e3 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.None,
                new EnPassantCaptureInfo("e3", "e4"),
                0,
                1,
                GameState.Default);

            Assert.That(gameBoard.ValidMoves.Keys.Contains("f4-e3"), Is.False);
            Assert.That(gameBoard.ValidMoves.Count, Is.EqualTo(16));
        }

        [Test]
        public void TestCannotCaptureEnPassantCheckingPawnByPinnedPawn()
        {
            var gameBoard = new GameBoard(
                "8/2p5/3p4/KP3k1r/5pP1/8/4P3/5R2 b - g3 0 1",
                PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.None,
                new EnPassantCaptureInfo("g3", "g4"),
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "f5-g6", "f5-e4", "f5-e6", "f5-g4", "f5-f6", "f5-g5", "f5-e5");
        }

        [Test]
        public void TestCannotCaptureCheckingPieceByPinnedPiece()
        {
            var gameBoard = new GameBoard("Q2k4/8/1n6/B7/8/8/8/7K b - - 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.Black,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "d8-c7", "d8-d7", "d8-e7");
        }

        [Test]
        public void TestCanPromoteByMovingAndByCapturing()
        {
            var gameBoard = new GameBoard("r6k/1P6/8/8/8/8/8/7K w - - 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.White,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Default);

            var kingMoves = new GameMove[] { "h1-g1", "h1-g2", "h1-h2" };
            var expectedValidMoves = kingMoves
                .Concat(new GameMove("b7", "b8").MakeAllPromotions())
                .Concat(new GameMove("b7", "a8").MakeAllPromotions());

            AssertValidMoves(gameBoard, expectedValidMoves);
        }

        [Test]
        public void TestCanHideFromCheckByPromotingPawn()
        {
            var gameBoard = new GameBoard("1K5q/2P5/k7/8/8/8/8/8 w - - 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.White,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, "c7-c8=Q", "c7-c8=R", "c7-c8=B", "c7-c8=N");
        }

        [Test]
        public void TestCanCaptureCheckingPieceByPromotingPawn()
        {
            var gameBoard = new GameBoard("r6k/1P6/1P6/KR6/1R6/8/8/8 w - - 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.White,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Check);

            AssertValidMoves(gameBoard, new GameMove("b7", "a8").MakeAllPromotions());
        }

        [Test]
        public void TestCheckmateByDoubleCheckAndPawnCannotCapture()
        {
            var gameBoard = new GameBoard("r6k/1P6/1Pn5/KR6/1R6/8/8/8 w - - 0 1", PerformInternalBoardValidation);

            AssertBaseProperties(
                gameBoard,
                PieceColor.White,
                CastlingOptions.None,
                null,
                0,
                1,
                GameState.Checkmate);

            AssertNoValidMoves(gameBoard);
        }

        [Test]
        public void TestMakeNullMove()
        {
            const string Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            var gameBoard = new GameBoard(Fen, PerformInternalBoardValidation);

            Assert.That(gameBoard.CanMakeNullMove, Is.True);
            var nullMoveGameBoard = gameBoard.MakeNullMove();

            AssertBaseProperties(
                nullMoveGameBoard,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                0,
                1,
                GameState.Default);

            Assert.That(nullMoveGameBoard.CanMakeNullMove, Is.True);
            var doubleNullMoveGameBoard = nullMoveGameBoard.MakeNullMove();

            AssertBaseProperties(
                doubleNullMoveGameBoard,
                gameBoard.ActiveColor,
                gameBoard.CastlingOptions,
                gameBoard.EnPassantCaptureInfo,
                gameBoard.HalfMoveCountBy50MoveRule,
                gameBoard.FullMoveIndex,
                GameState.Default);

            Assert.That(doubleNullMoveGameBoard.GetFen(), Is.EqualTo(gameBoard.GetFen()));
        }

        [Test]
        public void TestThreefoldRepetition()
        {
            var initialBoard = new GameBoard(PerformInternalBoardValidation);

            var boardBeforeRepetition = initialBoard.MakeMultipleMoves(
                (board, message) => Assert.That(board.GetAutoDrawType(), Is.EqualTo(AutoDrawType.None), message),
                "g1-f3",
                "g8-f6",
                "f3-g1",
                "f6-g8",
                "g1-f3",
                "g8-f6",
                "f3-g1");

            var threefoldBoard = boardBeforeRepetition.MakeMove("f6-g8");
            Assert.That(threefoldBoard.GetAutoDrawType(), Is.EqualTo(AutoDrawType.ThreefoldRepetition));

            var threefoldContinuedBoard = threefoldBoard.MakeMove("e2-e4");
            Assert.That(threefoldContinuedBoard.GetAutoDrawType(), Is.EqualTo(AutoDrawType.ThreefoldRepetition));
        }

        #endregion

        #region Private Methods

        private static GameBoard TestMakeMoveBasicScenario1W()
        {
            var gameBoard1W = new GameBoard(PerformInternalBoardValidation);

            // Testing invalid move (no piece at the source position)
            Assert.That(() => gameBoard1W.MakeMove("a3-a4"), Throws.ArgumentException);

            AssertBaseProperties(gameBoard1W, PieceColor.White, CastlingOptions.All, null, 0, 1, GameState.Default);

            return gameBoard1W;
        }

        private static GameBoard TestMakeMoveBasicScenario1B(GameBoard gameBoard1W)
        {
            var gameBoard1B = gameBoard1W.MakeMove("e2-e4");

            Assert.That(
                gameBoard1B.GetFen(),
                Is.EqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1"));

            AssertBaseProperties(
                gameBoard1B,
                PieceColor.Black,
                CastlingOptions.All,
                new EnPassantCaptureInfo("e3", "e4"),
                0,
                1,
                GameState.Default);

            // Testing invalid move (piece of non-active color at the source position)
            Assert.That(() => gameBoard1B.MakeMove("d2-d4"), Throws.ArgumentException);

            AssertBaseProperties(
                gameBoard1B,
                PieceColor.Black,
                CastlingOptions.All,
                new EnPassantCaptureInfo("e3", "e4"),
                0,
                1,
                GameState.Default);

            return gameBoard1B;
        }

        private static GameBoard TestMakeMoveBasicScenario2W(GameBoard gameBoard1B)
        {
            var gameBoard2W = gameBoard1B.MakeMove("e7-e5");

            Assert.That(
                gameBoard2W.GetFen(),
                Is.EqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2"));

            AssertBaseProperties(
                gameBoard2W,
                PieceColor.White,
                CastlingOptions.All,
                new EnPassantCaptureInfo("e6", "e5"),
                0,
                2,
                GameState.Default);

            return gameBoard2W;
        }

        private static GameBoard TestMakeMoveBasicScenario2B(GameBoard gameBoard2W)
        {
            var gameBoard2B = gameBoard2W.MakeMove("b1-c3");

            Assert.That(
                gameBoard2B.GetFen(),
                Is.EqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR b KQkq - 1 2"));

            AssertBaseProperties(
                gameBoard2B,
                PieceColor.Black,
                CastlingOptions.All,
                null,
                1,
                2,
                GameState.Default);

            return gameBoard2B;
        }

        private static GameBoard TestMakeMoveBasicScenario3W(GameBoard gameBoard2B)
        {
            var gameBoard3W = gameBoard2B.MakeMove("e8-e7");

            Assert.That(
                gameBoard3W.GetFen(),
                Is.EqualTo("rnbq1bnr/ppppkppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR w KQ - 2 3"));

            AssertBaseProperties(
                gameBoard3W,
                PieceColor.White,
                CastlingOptions.WhiteKingSide | CastlingOptions.WhiteQueenSide,
                null,
                2,
                3,
                GameState.Default);

            return gameBoard3W;
        }

        private static GameBoard TestMakeMoveBasicScenario3B(GameBoard gameBoard3W)
        {
            var gameBoard3B = gameBoard3W.MakeMove("c3-d5");

            Assert.That(
                gameBoard3B.GetFen(),
                Is.EqualTo("rnbq1bnr/ppppkppp/8/3Np3/4P3/8/PPPP1PPP/R1BQKBNR b KQ - 3 3"));

            AssertBaseProperties(
                gameBoard3B,
                PieceColor.Black,
                CastlingOptions.WhiteKingSide | CastlingOptions.WhiteQueenSide,
                null,
                3,
                3,
                GameState.Check);

            return gameBoard3B;
        }

        private static GameBoard TestMakeMoveFoolsMateScenario1B(GameBoard gameBoard1W)
        {
            var gameBoard1B = gameBoard1W.MakeMove("g2-g4");
            return gameBoard1B;
        }

        private static GameBoard TestMakeMoveFoolsMateScenario2W(GameBoard gameBoard1B)
        {
            var gameBoard2W = gameBoard1B.MakeMove("e7-e6");
            return gameBoard2W;
        }

        private static GameBoard TestMakeMoveFoolsMateScenario2B(GameBoard gameBoard2W)
        {
            var gameBoard2B = gameBoard2W.MakeMove("f2-f4");
            return gameBoard2B;
        }

        private static void TestMakeMoveFoolsMateScenario3W(GameBoard gameBoard2B)
        {
            var gameBoard3W = gameBoard2B.MakeMove("d8-h4");

            AssertBaseProperties(
                gameBoard3W,
                PieceColor.White,
                CastlingOptions.All,
                null,
                1,
                3,
                GameState.Checkmate);

            AssertNoValidMoves(gameBoard3W);
        }

        #endregion
    }
}