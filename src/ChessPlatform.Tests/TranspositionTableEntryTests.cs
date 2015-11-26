using System;
using System.Linq;
using ChessPlatform.Engine;
using ChessPlatform.GamePlay;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class TranspositionTableEntryTests
    {
        #region Tests

        [Test]
        public void TestStructureSize()
        {
            var size = TranspositionTableEntry.SizeInBytes;
            Console.WriteLine($@"{nameof(TranspositionTableEntry)} size: {size} bytes.");
        }

        [Test]
        public void TestConstruction()
        {
            const long Key = 0x12345678ABCDEF01L;
            const ScoreBound Bound = ScoreBound.Exact;
            const int Depth = CommonEngineConstants.MaxPlyDepthUpperLimit;
            const int Version = 87654321;

            var bestMove = GameMove.FromStringNotation("b2b1q");
            var score = EvaluationScore.Mate;
            var localScore = -EvaluationScore.Mate;

            var entry = new TranspositionTableEntry(Key, bestMove, score, localScore, Bound, Depth, Version);
            Assert.That(entry.Key, Is.EqualTo(Key));
            Assert.That(entry.BestMove, Is.EqualTo(bestMove));
            Assert.That(entry.Score, Is.EqualTo(score));
            Assert.That(entry.LocalScore, Is.EqualTo(localScore));
            Assert.That(entry.Bound, Is.EqualTo(Bound));
            Assert.That(entry.Depth, Is.EqualTo(Depth));
            Assert.That(entry.Version, Is.EqualTo(Version));
        }

        #endregion
    }
}