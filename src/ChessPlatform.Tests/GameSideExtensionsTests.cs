using System;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class GameSideExtensionsTests
    {
        #region Tests

        [Test]
        public void TestEnsureDefined()
        {
            Assert.That(() => GameSide.White.EnsureDefined(), Throws.Nothing);
            Assert.That(() => GameSide.Black.EnsureDefined(), Throws.Nothing);

            Assert.That(() => ((GameSide)123).EnsureDefined(), Throws.TypeOf<InvalidEnumArgumentException>());
        }

        [Test]
        public void TestInvert()
        {
            Assert.That(GameSide.White.Invert(), Is.EqualTo(GameSide.Black));
            Assert.That(GameSide.Black.Invert(), Is.EqualTo(GameSide.White));
        }

        [Test]
        [TestCase(GameSide.White, PieceType.None, Piece.None)]
        [TestCase(GameSide.Black, PieceType.None, Piece.None)]
        [TestCase(GameSide.White, PieceType.King, Piece.WhiteKing)]
        [TestCase(GameSide.Black, PieceType.Queen, Piece.BlackQueen)]
        public void TestToPiece(GameSide side, PieceType pieceType, Piece expectedPiece)
        {
            var piece = side.ToPiece(pieceType);
            Assert.That(piece, Is.EqualTo(expectedPiece));
        }

        #endregion
    }
}