﻿using System;
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

            AssertBaseProperties(boardState, PieceColor.White, CastlingOptions.All, null, 0, 1, GameState.Default);

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
        public void TestMakeMoveBasicScenario()
        {
            var boardState1W = TestMakeMoveBasicScenario1W();
            var boardState1B = TestMakeMoveBasicScenario1B(boardState1W);
            var boardState2W = TestMakeMoveBasicScenario2W(boardState1B);
            var boardState2B = TestMakeMoveBasicScenario2B(boardState2W);
            var boardState3W = TestMakeMoveBasicScenario3W(boardState2B);
            Assert.That(boardState3W, Is.Not.Null);
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

        private static BoardState TestMakeMoveBasicScenario1W()
        {
            var boardState1W = new BoardState();

            // Testing invalid move (no piece at the source position)
            Assert.That(() => boardState1W.MakeMove(new PieceMove("a3", "a4"), null), Throws.ArgumentException);

            AssertBaseProperties(boardState1W, PieceColor.White, CastlingOptions.All, null, 0, 1, GameState.Default);

            return boardState1W;
        }

        private static BoardState TestMakeMoveBasicScenario1B(BoardState boardState1W)
        {
            var boardState1B = boardState1W.MakeMove(new PieceMove("e2", "e4"), null);

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
            Assert.That(() => boardState1B.MakeMove(new PieceMove("d2", "d4"), null), Throws.ArgumentException);

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
            var boardState2W = boardState1B.MakeMove(new PieceMove("e7", "e5"), null);

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
            var boardState2B = boardState2W.MakeMove(new PieceMove("b1", "c3"), null);

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
            var boardState3W = boardState2B.MakeMove(new PieceMove("e8", "e7"), null);

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

        #endregion
    }
}