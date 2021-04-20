using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PieceTypeExtensionsTests
    {
        [Test]
        [TestCase(PieceType.None, GameSide.White, Piece.None)]
        [TestCase(PieceType.None, GameSide.Black, Piece.None)]
        [TestCase(PieceType.King, GameSide.White, Piece.WhiteKing)]
        [TestCase(PieceType.Queen, GameSide.Black, Piece.BlackQueen)]
        public void TestToPiece(PieceType pieceType, GameSide side, Piece expectedPiece)
        {
            var piece = pieceType.ToPiece(side);
            Assert.That(piece, Is.EqualTo(expectedPiece));
        }
    }
}