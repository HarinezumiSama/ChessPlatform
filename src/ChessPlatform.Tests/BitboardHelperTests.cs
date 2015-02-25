using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class BitboardHelperTests
    {
        #region Tests

        [Test]
        public void TestBitboardConstants()
        {
            Assert.That(BitboardHelper.NoBitSetIndex, Is.LessThan(0));
            Assert.That(Bitboards.None, Is.EqualTo(0L));
            Assert.That(Bitboards.Everything, Is.EqualTo(~0L));
        }

        [Test]
        public void TestFindFirstBitSetWhenNoBitsAreSet()
        {
            Assert.That(BitboardHelper.NoBitSetIndex, Is.LessThan(0));

            var bitboard = 0L;
            var actualResult = BitboardHelper.FindFirstBitSetIndex(bitboard);
            Assert.That(actualResult, Is.EqualTo(BitboardHelper.NoBitSetIndex));
        }

        [Test]
        public void TestFindFirstBitSetWhenSingleBitIsSet()
        {
            for (var index = 0; index < ChessConstants.SquareCount; index++)
            {
                var bitboard = 1L << index;
                var actualResult = BitboardHelper.FindFirstBitSetIndex(bitboard);
                Assert.That(actualResult, Is.EqualTo(index), "Failed for the bit {0}", index);
            }
        }

        [Test]
        [TestCase((1L << 0) | (1L << 17), 0)]
        [TestCase((1L << 49) | (1L << 23), 23)]
        public void TestFindFirstBitSetWhenMultipleBitsAreSet(long value, int expectedResult)
        {
            var actualResult = BitboardHelper.FindFirstBitSetIndex(value);
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

            Assert.That(BitboardHelper.GetPositions(value), Is.EquivalentTo(expectedResult));
            Assert.That(BitboardHelper.GetBitSetCount(value), Is.EqualTo(expectedResult.Length));
        }

        [Test]
        [TestCase(0L, 0L)]
        [TestCase(1L << 1, 1L << 1)]
        [TestCase(1L << 49, 1L << 49)]
        [TestCase((1L << 49) | (1L << 23), 1L << 23)]
        [TestCase((1L << 1) | (1L << 59), 1L << 1)]
        public void TestIsolateFirstBitSet(long value, long expectedResult)
        {
            var result = BitboardHelper.IsolateFirstBitSet(value);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("a1", ShiftDirection.North, "a2")]
        [TestCase("a1", ShiftDirection.NorthEast, "b2")]
        [TestCase("a1", ShiftDirection.East, "b1")]
        [TestCase("a1", ShiftDirection.SouthEast, null)]
        [TestCase("a1", ShiftDirection.South, null)]
        [TestCase("a1", ShiftDirection.SouthWest, null)]
        [TestCase("a1", ShiftDirection.West, null)]
        [TestCase("a1", ShiftDirection.NorthWest, null)]
        [TestCase("a8", ShiftDirection.North, null)]
        [TestCase("a8", ShiftDirection.NorthEast, null)]
        [TestCase("a8", ShiftDirection.East, "b8")]
        [TestCase("a8", ShiftDirection.SouthEast, "b7")]
        [TestCase("a8", ShiftDirection.South, "a7")]
        [TestCase("a8", ShiftDirection.SouthWest, null)]
        [TestCase("a8", ShiftDirection.West, null)]
        [TestCase("a8", ShiftDirection.NorthWest, null)]
        [TestCase("h8", ShiftDirection.North, null)]
        [TestCase("h8", ShiftDirection.NorthEast, null)]
        [TestCase("h8", ShiftDirection.East, null)]
        [TestCase("h8", ShiftDirection.SouthEast, null)]
        [TestCase("h8", ShiftDirection.South, "h7")]
        [TestCase("h8", ShiftDirection.SouthWest, "g7")]
        [TestCase("h8", ShiftDirection.West, "g8")]
        [TestCase("h8", ShiftDirection.NorthWest, null)]
        [TestCase("h1", ShiftDirection.North, "h2")]
        [TestCase("h1", ShiftDirection.NorthEast, null)]
        [TestCase("h1", ShiftDirection.East, null)]
        [TestCase("h1", ShiftDirection.SouthEast, null)]
        [TestCase("h1", ShiftDirection.South, null)]
        [TestCase("h1", ShiftDirection.SouthWest, null)]
        [TestCase("h1", ShiftDirection.West, "g1")]
        [TestCase("h1", ShiftDirection.NorthWest, "g2")]
        [TestCase("e2", ShiftDirection.North, "e3")]
        [TestCase("e2", ShiftDirection.NorthEast, "f3")]
        [TestCase("e2", ShiftDirection.East, "f2")]
        [TestCase("e2", ShiftDirection.SouthEast, "f1")]
        [TestCase("e2", ShiftDirection.South, "e1")]
        [TestCase("e2", ShiftDirection.SouthWest, "d1")]
        [TestCase("e2", ShiftDirection.West, "d2")]
        [TestCase("e2", ShiftDirection.NorthWest, "d3")]
        public void TestShift(
            string positionNotation,
            ShiftDirection direction,
            string expectedResultPositionNotation)
        {
            var bitboard = Position.FromAlgebraic(positionNotation).Bitboard;
            var resultBitboard = bitboard.Shift(direction);

            if (expectedResultPositionNotation == null)
            {
                Assert.That(resultBitboard == Bitboards.None, Is.True);
                return;
            }

            var expectedResultBitboard = Position.FromAlgebraic(expectedResultPositionNotation).Bitboard;
            Assert.That(resultBitboard, Is.EqualTo(expectedResultBitboard));
        }

        #endregion
    }
}