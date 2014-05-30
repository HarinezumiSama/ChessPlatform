using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class ChessHelperTests
    {
        #region Tests

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestGetPlatformVersion(bool fullVersion)
        {
            var platformVersion = ChessHelper.GetPlatformVersion(fullVersion);

            Console.WriteLine(
                @"[{0}] {1} -> '{2}'",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                fullVersion,
                platformVersion);

            Assert.That(platformVersion, Is.Not.Null);
            Assert.That(platformVersion, Is.Not.Empty);
        }

        #endregion
    }
}