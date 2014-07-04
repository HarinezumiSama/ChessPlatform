using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    public abstract class GameBoardTestBase
    {
        #region Constants and Fields

        protected const bool PerformInternalBoardValidation = true;

        #endregion

        #region Protected Methods

        protected static void AssertBaseProperties(
            GameBoard gameBoard,
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
                Is.EqualTo(expectedEnPassantCaptureInfo.CapturePosition),
                "Capture position.");

            Assert.That(
                actualEnPassantCaptureInfo.TargetPiecePosition,
                Is.EqualTo(expectedEnPassantCaptureInfo.TargetPiecePosition),
                "Target piece position.");
        }

        protected static void AssertValidMoves(GameBoard gameBoard, params GameMove[] expectedValidMoves)
        {
            Assert.That(gameBoard, Is.Not.Null);
            Assert.That(expectedValidMoves, Is.Not.Null);

            var actualValidMoves = gameBoard.ValidMoves.Keys.OrderBy(move => move.ToString()).ToArray();
            var expectedValidMovesSorted = expectedValidMoves.OrderBy(move => move.ToString()).ToArray();

            Assert.That(actualValidMoves, Is.EquivalentTo(expectedValidMovesSorted));

            foreach (var actualValidMove in actualValidMoves)
            {
                var pieceMoveInfo = gameBoard.ValidMoves[actualValidMove];

                Assert.That(
                    pieceMoveInfo.IsCapture,
                    Is.EqualTo(gameBoard.IsCapturingMove(actualValidMove)));

                Assert.That(
                    gameBoard[actualValidMove.To].GetColor(),
                    Is.EqualTo(
                        pieceMoveInfo.IsCapture && !pieceMoveInfo.IsEnPassantCapture
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

        protected static void AssertValidMoves(GameBoard gameBoard, IEnumerable<GameMove> expectedValidMoves)
        {
            Assert.That(expectedValidMoves, Is.Not.Null);

            AssertValidMoves(gameBoard, expectedValidMoves.ToArray());
        }

        protected static void AssertNoValidMoves(GameBoard gameBoard)
        {
            Assert.That(gameBoard, Is.Not.Null);

            Assert.That(gameBoard.ValidMoves.Count, Is.EqualTo(0));
            Assert.That(gameBoard.ValidMoves, Is.Empty);
        }

        protected static void AssertDefaultInitialBoard(GameBoard gameBoard)
        {
            Assert.That(gameBoard, Is.Not.Null);

            Assert.That(gameBoard.GetFen(), Is.EqualTo(ChessConstants.DefaultInitialFen));

            AssertBaseProperties(gameBoard, PieceColor.White, CastlingOptions.All, null, 0, 1, GameState.Default);

            AssertValidMoves(
                gameBoard,
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

        #endregion
    }
}