using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class BitboardTests
    {
        #region Tests

        [Test]
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(0x1234567890ABCDEFL)]
        public void TestConstructionFromValue(long value)
        {
            var bitboard = new Bitboard(value);
            Assert.That(bitboard.Value, Is.EqualTo(value));
        }

        [Test]
        [TestCase(0L)]
        [TestCase(1L, "a1")]
        [TestCase(1L << 1, "b1")]
        [TestCase(1L << 49, "b7")]
        [TestCase((1L << 49) | (1L << 23), "b7", "h3")]
        [TestCase((1L << 1) | (1L << 59), "b1", "d8")]
        public void TestConstructionFromPositions(long expectedValue, params string[] positionNotations)
        {
            Assert.That(positionNotations, Is.Not.Null);
            var positions = positionNotations.Select(Position.FromAlgebraic).ToArray();

            var bitboard = new Bitboard(positions);
            Assert.That(bitboard.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void TestFindFirstBitSetWhenNoBitsAreSet()
        {
            Assert.That(Bitboard.NoBitSetIndex, Is.LessThan(0));

            var bitboard = new Bitboard(0L);
            var actualResult = bitboard.FindFirstBitSet();
            Assert.That(actualResult, Is.EqualTo(Bitboard.NoBitSetIndex));
        }

        [Test]
        public void TestFindFirstBitSetWhenSingleBitIsSet()
        {
            for (var index = 0; index < ChessConstants.SquareCount; index++)
            {
                var value = 1L << index;
                var bitboard = new Bitboard(value);
                var actualResult = bitboard.FindFirstBitSet();
                Assert.That(actualResult, Is.EqualTo(index), "Failed for the bit {0}", index);
            }
        }

        [Test]
        [TestCase((1L << 0) | (1L << 17), 0)]
        [TestCase((1L << 49) | (1L << 23), 23)]
        public void TestFindFirstBitSetWhenMultipleBitsAreSet(long value, int expectedResult)
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
        public void TestGetPositionsAndGetCount(long value, params int[] expectedIndexesResult)
        {
            Assert.That(expectedIndexesResult, Is.Not.Null);
            var expectedResult = expectedIndexesResult.Select(Position.FromSquareIndex).ToArray();

            var bitboard = new Bitboard(value);
            Assert.That(bitboard.GetPositions(), Is.EquivalentTo(expectedResult));
            Assert.That(bitboard.GetCount(), Is.EqualTo(expectedResult.Length));
        }

        #endregion
    }
}