using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class BoardStateTests
    {
        #region Constants and Fields

        private const string DefaultFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        #endregion

        #region Tests

        [Test]
        public void TestDefaultConstruction()
        {
            var boardState = new BoardState();

            Assert.That(boardState.GetFen(), Is.EqualTo(DefaultFen));

            AssertBaseProperties(boardState, PieceColor.White, CastlingOptions.All, null, 1, 0, GameState.Default);

            AssertValidMoves(
                boardState,
                new PieceMove("a2", "a3"),
                new PieceMove("a2", "a4"),
                new PieceMove("b2", "b3"),
                new PieceMove("b2", "b4"),
                new PieceMove("c2", "c3"),
                new PieceMove("c2", "c4"),
                new PieceMove("d2", "d3"),
                new PieceMove("d2", "d4"),
                new PieceMove("e2", "e3"),
                new PieceMove("e2", "e4"),
                new PieceMove("f2", "f3"),
                new PieceMove("f2", "f4"),
                new PieceMove("g2", "g3"),
                new PieceMove("g2", "g4"),
                new PieceMove("h2", "h3"),
                new PieceMove("h2", "h4"),
                new PieceMove("b1", "a3"),
                new PieceMove("b1", "c3"),
                new PieceMove("g1", "f3"),
                new PieceMove("g1", "h3"));
        }

        [Test]
        public void TestMakeMoveScenario()
        {
            var boardState1 = new BoardState();

            Assert.That(() => boardState1.MakeMove(new PieceMove("a3", "a4"), null), Throws.ArgumentException);

            var boardState2 = boardState1.MakeMove(new PieceMove("e2", "e4"), null);
            Assert.That(
                boardState2.GetFen(),
                Is.EqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1"));

            Assert.That(() => boardState2.MakeMove(new PieceMove("d2", "d4"), null), Throws.ArgumentException);

            var boardState3 = boardState2.MakeMove(new PieceMove("e7", "e5"), null);
            Assert.That(
                boardState3.GetFen(),
                Is.EqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2"));

            var boardState4 = boardState3.MakeMove(new PieceMove("b1", "c3"), null);
            Assert.That(
                boardState4.GetFen(),
                Is.EqualTo("rnbqkbnr/pppp1ppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR b KQkq - 1 2"));

            var boardState5 = boardState4.MakeMove(new PieceMove("e8", "e7"), null);
            Assert.That(
                boardState5.GetFen(),
                Is.EqualTo("rnbq1bnr/ppppkppp/8/4p3/4P3/2N5/PPPP1PPP/R1BQKBNR w KQ - 2 3"));
        }

        #endregion

        #region Private Methods

        private static void AssertBaseProperties(
            BoardState boardState,
            PieceColor expectedActiveColor,
            CastlingOptions expectedCastlingOptions,
            EnPassantCaptureInfo expectedEnPassantCaptureInfo,
            int expectedFullMoveIndex,
            int expectedHalfMovesBy50MoveRule,
            GameState expectedGameState)
        {
            Assert.That(boardState, Is.Not.Null);

            Assert.That(boardState.ActiveColor, Is.EqualTo(expectedActiveColor));
            Assert.That(boardState.CastlingOptions, Is.EqualTo(expectedCastlingOptions));
            Assert.That(boardState.EnPassantCaptureInfo, Is.EqualTo(expectedEnPassantCaptureInfo));
            Assert.That(boardState.FullMoveIndex, Is.EqualTo(expectedFullMoveIndex));
            Assert.That(boardState.HalfMovesBy50MoveRule, Is.EqualTo(expectedHalfMovesBy50MoveRule));
            Assert.That(boardState.State, Is.EqualTo(expectedGameState));
        }

        private static void AssertValidMoves(BoardState boardState, params PieceMove[] expectedValidMoves)
        {
            Assert.That(boardState, Is.Not.Null);
            Assert.That(expectedValidMoves, Is.Not.Null);

            Assert.That(boardState.ValidMoves, Is.EquivalentTo(expectedValidMoves));
        }

        #endregion
    }
}