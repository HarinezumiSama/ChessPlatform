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
        [TestCase(0UL, -1)]
        [TestCase(1UL, 0)]
        [TestCase(1UL << 1, 1)]
        [TestCase(1UL << 49, 49)]
        [TestCase((1UL << 49) | (1UL << 23), 23)]
        public void TestFindFirstBitSet(long value, int expectedResult)
        {
            var actualResult = value.FindFirstBitSet();
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public void TestFindAllBitsSet()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}