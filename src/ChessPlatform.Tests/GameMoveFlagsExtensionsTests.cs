using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class GameMoveFlagsExtensionsTests
    {
        [Test]
        [TestCase(GameMoveFlags.None, GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsRegularCapture, GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsEnPassantCapture, GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsKingCastling, GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.None, GameMoveFlags.IsPawnPromotion, false)]
        [TestCase(GameMoveFlags.None, GameMoveFlags.IsRegularCapture, false)]
        [TestCase(GameMoveFlags.None, GameMoveFlags.IsEnPassantCapture, false)]
        [TestCase(GameMoveFlags.None, GameMoveFlags.IsKingCastling, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, GameMoveFlags.IsPawnPromotion, true)]
        [TestCase(GameMoveFlags.IsPawnPromotion, GameMoveFlags.IsRegularCapture, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, GameMoveFlags.IsEnPassantCapture, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, GameMoveFlags.IsKingCastling, false)]
        [TestCase(
            GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture,
            GameMoveFlags.IsPawnPromotion, true)]
        [TestCase(
            GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture,
            GameMoveFlags.IsRegularCapture, true)]
        [TestCase(
            GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture,
            GameMoveFlags.IsEnPassantCapture, false)]
        [TestCase(
            GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture,
            GameMoveFlags.IsKingCastling, false)]
        public void TestIsAnySet(GameMoveFlags value, GameMoveFlags flagsToCheck, bool expectedResult)
        {
            Assert.That(value.IsAnySet(flagsToCheck), Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, true)]
        [TestCase(GameMoveFlags.IsRegularCapture, false)]
        [TestCase(GameMoveFlags.IsEnPassantCapture, false)]
        [TestCase(GameMoveFlags.IsKingCastling, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture, true)]
        public void TestIsPawnPromotion(GameMoveFlags value, bool expectedResult)
        {
            Assert.That(value.IsPawnPromotion(), Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, false)]
        [TestCase(GameMoveFlags.IsRegularCapture, true)]
        [TestCase(GameMoveFlags.IsEnPassantCapture, false)]
        [TestCase(GameMoveFlags.IsKingCastling, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture, true)]
        public void TestIsRegularCapture(GameMoveFlags value, bool expectedResult)
        {
            Assert.That(value.IsRegularCapture(), Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, false)]
        [TestCase(GameMoveFlags.IsRegularCapture, false)]
        [TestCase(GameMoveFlags.IsEnPassantCapture, true)]
        [TestCase(GameMoveFlags.IsKingCastling, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture, false)]
        public void TestIsEnPassantCapture(GameMoveFlags value, bool expectedResult)
        {
            Assert.That(value.IsEnPassantCapture(), Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, false)]
        [TestCase(GameMoveFlags.IsRegularCapture, true)]
        [TestCase(GameMoveFlags.IsEnPassantCapture, true)]
        [TestCase(GameMoveFlags.IsKingCastling, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture, true)]
        public void TestIsAnyCapture(GameMoveFlags value, bool expectedResult)
        {
            Assert.That(value.IsAnyCapture(), Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(GameMoveFlags.None, false)]
        [TestCase(GameMoveFlags.IsPawnPromotion, false)]
        [TestCase(GameMoveFlags.IsRegularCapture, false)]
        [TestCase(GameMoveFlags.IsEnPassantCapture, false)]
        [TestCase(GameMoveFlags.IsKingCastling, true)]
        [TestCase(GameMoveFlags.IsPawnPromotion | GameMoveFlags.IsRegularCapture, false)]
        public void TestIsKingCastling(GameMoveFlags value, bool expectedResult)
        {
            Assert.That(value.IsKingCastling(), Is.EqualTo(expectedResult));
        }
    }
}