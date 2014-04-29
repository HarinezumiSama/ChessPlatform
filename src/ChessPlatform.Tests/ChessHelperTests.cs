using System;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class ChessHelperTests
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
            var actualResult = value.FindFirstBitSet();
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(0L)]
        [TestCase(1L, 0)]
        [TestCase(1L << 1, 1)]
        [TestCase(1L << 49, 49)]
        [TestCase((1L << 49) | (1L << 23), 49, 23)]
        [TestCase((1L << 1) | (1L << 59), 1, 59)]
        public void TestFindAllBitsSet(long value, params int[] expectedResult)
        {
            var actualResult = value.FindAllBitsSet();
            Assert.That(actualResult, Is.EquivalentTo(expectedResult));
        }

        #endregion
    }
}