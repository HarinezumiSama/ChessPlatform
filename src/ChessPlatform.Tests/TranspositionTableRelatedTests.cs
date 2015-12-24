using System;
using System.Linq;
using System.Runtime.InteropServices;
using ChessPlatform.Engine;
using ChessPlatform.GamePlay;
using NUnit.Framework;

//// ReSharper disable PossibleInvalidOperationException

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class TranspositionTableRelatedTests
    {
        #region Tests

        [Test]
        public void TestTranspositionTableEntrySize()
        {
            GetAndAssertStructureSize<TranspositionTableEntry>(40);
        }

        [Test]
        public void TestTranspositionTableBucketSize()
        {
            const int ExpectedSize = 64;

            GetAndAssertStructureSize<TranspositionTableBucket>(ExpectedSize);
            Assert.That(TranspositionTable.BucketSizeInBytes, Is.EqualTo(ExpectedSize));
        }

        [Test]
        public void TestTranspositionTableEntryConstruction()
        {
            const long Key = 0x12345678ABCDEF01L;
            const ScoreBound Bound = ScoreBound.Exact;
            const int Depth = CommonEngineConstants.MaxPlyDepthUpperLimit;

            var bestMove = GameMove.FromStringNotation("b2b1q");
            var score = EvaluationScore.Mate;
            var localScore = new EvaluationScore(-789);

            var entry = new TranspositionTableEntry(Key, bestMove, score, localScore, Bound, Depth);
            Assert.That(entry.Key, Is.EqualTo(Key));
            Assert.That(entry.BestMove, Is.EqualTo(bestMove));
            Assert.That(entry.Score, Is.EqualTo(score));
            Assert.That(entry.LocalScore, Is.EqualTo(localScore));
            Assert.That(entry.Bound, Is.EqualTo(Bound));
            Assert.That(entry.Depth, Is.EqualTo(Depth));
            Assert.That(entry.Version, Is.EqualTo(0));
        }

        [Test]
        public void TestTranspositionTable()
        {
            var transpositionTable = new TranspositionTable(TranspositionTableHelper.SizeInMegaBytesRange.Lower);
            Assert.That(transpositionTable.Version, Is.Not.EqualTo(0));

            const long Key = 0x12345678ABCDEF01L;
            const long OtherKey = 0x987654321L;
            const ScoreBound Bound = ScoreBound.Exact;
            const int Depth = CommonEngineConstants.MaxPlyDepthUpperLimit;

            var bestMove = GameMove.FromStringNotation("b2b1q");
            var score = EvaluationScore.Mate;
            var localScore = new EvaluationScore(-789);

            var entry = new TranspositionTableEntry(Key, bestMove, score, localScore, Bound, Depth);

            transpositionTable.Save(ref entry);
            Assert.That(entry.Version, Is.EqualTo(transpositionTable.Version));

            Assert.That(transpositionTable.ProbeCount, Is.EqualTo(0));
            Assert.That(transpositionTable.HitCount, Is.EqualTo(0));

            var foundEntry1 = transpositionTable.Probe(Key);

            Assert.That(transpositionTable.ProbeCount, Is.EqualTo(1));
            Assert.That(transpositionTable.HitCount, Is.EqualTo(1));
            Assert.That(foundEntry1.HasValue, Is.True);
            Assert.That(foundEntry1.Value.Key, Is.EqualTo(Key));
            Assert.That(foundEntry1.Value.BestMove, Is.EqualTo(bestMove));
            Assert.That(foundEntry1.Value.Score, Is.EqualTo(score));
            Assert.That(foundEntry1.Value.LocalScore, Is.EqualTo(localScore));
            Assert.That(foundEntry1.Value.Bound, Is.EqualTo(Bound));
            Assert.That(foundEntry1.Value.Depth, Is.EqualTo(Depth));
            Assert.That(foundEntry1.Value.Version, Is.EqualTo(transpositionTable.Version));

            var foundEntry2 = transpositionTable.Probe(OtherKey);
            Assert.That(transpositionTable.ProbeCount, Is.EqualTo(2));
            Assert.That(transpositionTable.HitCount, Is.EqualTo(1));
            Assert.That(foundEntry2.HasValue, Is.False);
        }

        #endregion

        #region Private Methods

        private static void GetAndAssertStructureSize<T>(int expectedSize)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            Console.WriteLine($@"{typeof(T).GetQualifiedName()} size: {size} bytes.");
            Assert.That(size, Is.EqualTo(expectedSize));
        }

        #endregion
    }
}