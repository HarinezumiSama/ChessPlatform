using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class BitboardTests
    {
        #region Tests

        [Test]
        [TestCase(0L, -1)]
        [TestCase(1L, 0)]
        [TestCase(1L << 1, 1)]
        [TestCase(1L << 49, 49)]
        [TestCase((1L << 49) | (1L << 23), 23)]
        public void TestFindFirstBitSet(long value, int expectedResult)
        {
            var bitboard = new Bitboard(value);
            var actualResult = bitboard.FindFirstBitSet();
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(0L)]
        [TestCase(1L, 0)]
        [TestCase(1L << 1, 1)]
        [TestCase(1L << 49, 49)]
        [TestCase((1L << 49) | (1L << 23), 49, 23)]
        [TestCase((1L << 1) | (1L << 59), 1, 59)]
        public void TestFindAllBitsSet(long value, params int[] expectedIndexesResult)
        {
            var expectedResult = expectedIndexesResult.Select(Position.FromBitboardBitIndex).ToArray();

            var bitboard = new Bitboard(value);
            var actualResult = bitboard.GetPositions();
            Assert.That(actualResult, Is.EquivalentTo(expectedResult));
        }

        #endregion
    }
}