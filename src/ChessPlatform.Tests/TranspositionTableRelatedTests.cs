using System;
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
        [Test]
        public void TestTranspositionTableEntrySize()
        {
            GetAndAssertStructureSize<TranspositionTableEntry>(30);
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
        public void TestTranspositionTableBucketConstruction()
        {
            var entry1 = new TranspositionTableEntry(
                0x1234567890A,
                GameMove.FromStringNotation("a1b1"),
                EvaluationScore.Mate,
                EvaluationScore.Zero,
                ScoreBound.Exact,
                3);

            var entry2 = new TranspositionTableEntry(
                0xABCDEF9876,
                GameMove.FromStringNotation("g7g8q"),
                new EvaluationScore(1001),
                new EvaluationScore(997),
                ScoreBound.Lower,
                11);

            Assert.That(entry1.Key, Is.Not.EqualTo(entry2.Key));
            Assert.That(entry1.BestMove, Is.Not.EqualTo(entry2.BestMove));
            Assert.That(entry1.Score, Is.Not.EqualTo(entry2.Score));
            Assert.That(entry1.LocalScore, Is.Not.EqualTo(entry2.LocalScore));
            Assert.That(entry1.Bound, Is.Not.EqualTo(entry2.Bound));
            Assert.That(entry1.Depth, Is.Not.EqualTo(entry2.Depth));

            var bucket = new TranspositionTableBucket
            {
                Entry1 = entry1,
                Entry2 = entry2
            };

            Assert.That(bucket.Entry1, Is.EqualTo(entry1));
            Assert.That(bucket.Entry2, Is.EqualTo(entry2));
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

        private static void GetAndAssertStructureSize<T>(int expectedSize)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            Console.WriteLine($@"{typeof(T).GetQualifiedName()} size: {size} bytes.");
            Assert.That(size, Is.EqualTo(expectedSize));
        }
    }
}