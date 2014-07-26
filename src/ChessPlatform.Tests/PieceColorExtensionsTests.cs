using System;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PieceColorExtensionsTests
    {
        #region Tests

        [Test]
        public void TestEnsureDefined()
        {
            Assert.That(() => PieceColor.White.EnsureDefined(), Throws.Nothing);
            Assert.That(() => PieceColor.Black.EnsureDefined(), Throws.Nothing);

            Assert.That(() => ((PieceColor)123).EnsureDefined(), Throws.TypeOf<InvalidEnumArgumentException>());
        }

        [Test]
        public void TestInvert()
        {
            Assert.That(PieceColor.White.Invert(), Is.EqualTo(PieceColor.Black));
            Assert.That(PieceColor.Black.Invert(), Is.EqualTo(PieceColor.White));
        }

        #endregion
    }
}