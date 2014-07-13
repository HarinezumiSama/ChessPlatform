using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PieceTypeExtensionsTests
    {
        #region Tests

        [Test]
        [TestCase(PieceType.None, PieceColor.White, Piece.None)]
        [TestCase(PieceType.None, PieceColor.Black, Piece.None)]
        [TestCase(PieceType.King, PieceColor.White, Piece.WhiteKing)]
        [TestCase(PieceType.Queen, PieceColor.Black, Piece.BlackQueen)]
        public void TestToPiece(PieceType pieceType, PieceColor color, Piece expectedPiece)
        {
            var piece = pieceType.ToPiece(color);
            Assert.That(piece, Is.EqualTo(expectedPiece));
        }

        #endregion
    }
}