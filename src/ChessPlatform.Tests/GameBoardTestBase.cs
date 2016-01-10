using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Omnifactotum.Annotations;

namespace ChessPlatform.Tests
{
    public abstract class GameBoardTestBase
    {
        #region Constants and Fields

        protected const bool PerformInternalBoardValidation = true;

        #endregion

        #region Protected Methods

        protected static void AssertBaseProperties(
            [NotNull] GameBoard gameBoard,
            PieceColor expectedActiveColor,
            CastlingOptions expectedCastlingOptions,
            EnPassantCaptureInfo expectedEnPassantCaptureInfo,
            int expectedHalfMoveCountBy50MoveRule,
            int expectedFullMoveIndex,
            GameState expectedGameState,
            AutoDrawType autoDrawType = AutoDrawType.None)
        {
            Assert.That(gameBoard, Is.Not.Null);

            Assert.That(gameBoard.ActiveColor, Is.EqualTo(expectedActiveColor));
            Assert.That(gameBoard.CastlingOptions, Is.EqualTo(expectedCastlingOptions));
            AssertEnPassantCaptureInfo(gameBoard.EnPassantCaptureInfo, expectedEnPassantCaptureInfo);
            Assert.That(gameBoard.FullMoveIndex, Is.EqualTo(expectedFullMoveIndex));
            Assert.That(gameBoard.HalfMoveCountBy50MoveRule, Is.EqualTo(expectedHalfMoveCountBy50MoveRule));
            Assert.That(gameBoard.FullMoveCountBy50MoveRule, Is.EqualTo(expectedHalfMoveCountBy50MoveRule / 2));
            Assert.That(gameBoard.State, Is.EqualTo(expectedGameState));
            Assert.That(gameBoard.GetAutoDrawType(), Is.EqualTo(autoDrawType));
        }

        protected static void AssertValidMoves(
            [NotNull] GameBoard gameBoard,
            [NotNull] params GameMove[] expectedValidMoves)
        {
            Assert.That(gameBoard, Is.Not.Null);
            Assert.That(gameBoard.ValidMoves, Is.Not.Null);

            Assert.That(expectedValidMoves, Is.Not.Null);

            var actualValidMoves = gameBoard.ValidMoves.Keys.OrderBy(move => move.ToString()).ToArray();
            var expectedValidMovesSorted = expectedValidMoves.OrderBy(move => move.ToString()).ToArray();

            Assert.That(actualValidMoves, Is.EquivalentTo(expectedValidMovesSorted));

            foreach (var actualValidMove in actualValidMoves)
            {
                var pieceMoveInfo = gameBoard.ValidMoves[actualValidMove];

                Assert.That(
                    pieceMoveInfo.IsAnyCapture,
                    Is.EqualTo(gameBoard.IsCapturingMove(actualValidMove)));

                Assert.That(
                    gameBoard[actualValidMove.To].GetColor(),
                    Is.EqualTo(
                        pieceMoveInfo.IsRegularCapture && !pieceMoveInfo.IsEnPassantCapture
                            ? gameBoard.ActiveColor.Invert()
                            : (PieceColor?)null));

                if (pieceMoveInfo.IsEnPassantCapture)
                {
                    Assert.That(
                        gameBoard[actualValidMove.From],
                        Is.EqualTo(PieceType.Pawn.ToPiece(gameBoard.ActiveColor)));

                    Assert.That(gameBoard[actualValidMove.To].GetPieceType(), Is.EqualTo(PieceType.None));
                }

                Assert.That(
                    pieceMoveInfo.IsPawnPromotion,
                    Is.EqualTo(gameBoard.IsPawnPromotionMove(actualValidMove)));

                Assert.That(
                    pieceMoveInfo.IsPawnPromotion,
                    Is.EqualTo(actualValidMove.PromotionResult != PieceType.None));
            }
        }

        protected static void AssertNoValidMoves([NotNull] GameBoard gameBoard)
        {
            Assert.That(gameBoard, Is.Not.Null);

            Assert.That(gameBoard.ValidMoves, Is.Not.Null);
            Assert.That(gameBoard.ValidMoves.Count, Is.EqualTo(0));
            Assert.That(gameBoard.ValidMoves, Is.Empty);
        }

        protected static void AssertPieces([NotNull] GameBoard gameBoard, [NotNull] params Piece[] pieces)
        {
            Assert.That(gameBoard, Is.Not.Null);
            Assert.That(pieces, Is.Not.Null);
            Assert.That(pieces.Length, Is.EqualTo(ChessConstants.SquareCount));

            for (var squareIndex = 0; squareIndex < pieces.Length; squareIndex++)
            {
                var expectedPiece = pieces[squareIndex];
                var square = new Square(squareIndex);

                Assert.That(
                    gameBoard[square],
                    Is.EqualTo(expectedPiece),
                    $@"Piece at '{square}' must be '{expectedPiece}'.");

                var bitboard = gameBoard.GetBitboard(expectedPiece);

                Assert.That(
                    (bitboard & square.Bitboard).IsExactlyOneBitSet(),
                    Is.True,
                    $@"Bitboard for '{expectedPiece}' at '{square}' is invalid.");

                var squares = gameBoard.GetSquares(expectedPiece);

                Assert.That(
                    squares,
                    Contains.Item(square),
                    $@"Squares of '{expectedPiece}' must contain '{square}'.");
            }
        }


        #endregion

        #region Private Methods

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
                actualEnPassantCaptureInfo.CaptureSquare,
                Is.EqualTo(expectedEnPassantCaptureInfo.CaptureSquare),
                "Capture Square.");

            Assert.That(
                actualEnPassantCaptureInfo.TargetPieceSquare,
                Is.EqualTo(expectedEnPassantCaptureInfo.TargetPieceSquare),
                "Target piece Square.");
        }

        #endregion
    }
}