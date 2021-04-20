using System.Linq;
using ChessPlatform.Serializers;
using ChessPlatform.Tests.Properties;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PgnGameSerializerTests
    {
        [Test]
        public void TestDeserialize()
        {
            const string ExpectedFinalFen = @"8/8/4R1p1/2k3p1/1p4P1/1P1b1P2/3K1n2/8 b - - 2 43";
            const int ExpectedMoveCount = 85;

            var data = Resources.PgnSample;

            var gameDescriptions = new PgnGameSerializer().Deserialize(data);
            Assert.That(gameDescriptions, Is.Not.Null);
            Assert.That(gameDescriptions.Length, Is.EqualTo(1));

            var gameDescription = gameDescriptions.Single();
            Assert.That(gameDescription.InitialBoard.GetFen(), Is.EqualTo(ChessConstants.DefaultInitialFen));
            Assert.That(gameDescription.Moves.Count, Is.EqualTo(ExpectedMoveCount));
            Assert.That(gameDescription.FinalBoard.GetFen(), Is.EqualTo(ExpectedFinalFen));
        }
    }
}