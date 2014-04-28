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
        // ReSharper disable once InconsistentNaming
        public void TestFindLs1b(ulong value, int expectedResult)
        {
            var actualResult = value.FindLs1b();
            Assert.That(actualResult, Is.EqualTo(expectedResult));

            var actualResultSigned = ((long)value).FindLs1b();
            Assert.That(actualResultSigned, Is.EqualTo(expectedResult));
        }

        #endregion
    }
}