using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PiecePositionTests
    {
        [Test]
        public void TestConstruction()
        {
            var position = new PiecePosition();
            AssertEmptyPosition(position);
        }

        [Test]
        public void TestSetPiece()
        {
            foreach (var testSquare in ChessHelper.AllSquares)
            {
                foreach (var testPiece in ChessConstants.PiecesExceptNone)
                {
                    var position = new PiecePosition();

                    var oldPiece1 = position.SetPiece(testSquare, testPiece);
                    Assert.That(oldPiece1, Is.EqualTo(Piece.None));

                    foreach (var side in ChessConstants.GameSides)
                    {
                        var expectedSideBitboard = testPiece.GetSide() == side ? testSquare.Bitboard : Bitboard.None;
                        Assert.That(position[side], Is.EqualTo(expectedSideBitboard));
                    }

                    Assert.That(position[Piece.None], Is.EqualTo(~testSquare.Bitboard));
                    foreach (var piece in ChessConstants.PiecesExceptNone)
                    {
                        var expectedBitboard = piece == testPiece ? testSquare.Bitboard : Bitboard.None;
                        Assert.That(position[piece], Is.EqualTo(expectedBitboard));
                    }

                    foreach (var square in ChessHelper.AllSquares)
                    {
                        var expectedPiece = square == testSquare ? testPiece : Piece.None;
                        Assert.That(position[square], Is.EqualTo(expectedPiece));
                    }

                    var oldPiece2 = position.SetPiece(testSquare, Piece.None);
                    Assert.That(oldPiece2, Is.EqualTo(testPiece));
                    AssertEmptyPosition(position);
                }
            }
        }

        private static void AssertEmptyPosition(PiecePosition position)
        {
            foreach (var side in ChessConstants.GameSides)
            {
                Assert.That(position[side], Is.EqualTo(Bitboard.None));
            }

            Assert.That(position[Piece.None], Is.EqualTo(Bitboard.Everything));
            foreach (var piece in ChessConstants.PiecesExceptNone)
            {
                Assert.That(position[piece], Is.EqualTo(Bitboard.None));
            }

            foreach (var square in ChessHelper.AllSquares)
            {
                Assert.That(position[square], Is.EqualTo(Piece.None));
            }
        }
    }
}