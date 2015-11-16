using System;
using System.Linq;
using ChessPlatform.Serializers;
using ChessPlatform.Tests.Properties;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PgnGameSerializerTests
    {
        #region Tests

        [Test]
        public void TestDeserialize()
        {
            var data = Resources.PgnSample;

            var gameDescriptions = new PgnGameSerializer().Deserialize(data);
            Assert.That(gameDescriptions, Is.Not.Null);
            Assert.That(gameDescriptions.Length, Is.EqualTo(1));
        }

        #endregion
    }
}