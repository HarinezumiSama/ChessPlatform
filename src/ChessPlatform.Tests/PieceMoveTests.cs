using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PieceMoveTests
    {
        #region Constants and Fields

        private static readonly PieceType[] ValidPromotionArguments =
            ChessConstants.ValidPromotions.Concat(PieceType.None.AsArray()).ToArray();

        #endregion

        #region Tests

        [Test]
        public void TestConstruction()
        {
            for (var fromIndex = 0; fromIndex < Position.MaxBitboardBitIndex; fromIndex++)
            {
                var from = Position.FromBitboardBitIndex(fromIndex);

                for (var toIndex = 0; toIndex < Position.MaxBitboardBitIndex; toIndex++)
                {
                    if (fromIndex == toIndex)
                    {
                        continue;
                    }

                    var to = Position.FromBitboardBitIndex(toIndex);

                    var outerMove = new PieceMove(from, to);
                    Assert.That(outerMove.From, Is.EqualTo(from));
                    Assert.That(outerMove.To, Is.EqualTo(to));
                    Assert.That(outerMove.PromotionResult, Is.EqualTo(PieceType.None));

                    foreach (var promotion in ValidPromotionArguments)
                    {
                        var innerMove = new PieceMove(from, to, promotion);
                        Assert.That(innerMove.From, Is.EqualTo(from));
                        Assert.That(innerMove.To, Is.EqualTo(to));
                        Assert.That(innerMove.PromotionResult, Is.EqualTo(promotion));
                    }
                }
            }
        }

        [Test]
        [TestCaseSource(typeof(FromStringNotationCases))]
        public void TestFromStringNotation(
            string input,
            string expectedFromString,
            string expectedToString,
            PieceType expectedPromotionResult)
        {
            var expectedFrom = Position.FromAlgebraic(expectedFromString);
            var expectedTo = Position.FromAlgebraic(expectedToString);

            var move = PieceMove.FromStringNotation(input);
            Assert.That(move, Is.Not.Null);
            Assert.That(move.From, Is.EqualTo(expectedFrom));
            Assert.That(move.To, Is.EqualTo(expectedTo));
            Assert.That(move.PromotionResult, Is.EqualTo(expectedPromotionResult));
        }

        [Test]
        [TestCaseSource(typeof(EqualityCases))]
        public void TestEquality(string firstMoveStringNotation, PieceMove second, bool expectedEquals)
        {
            var first = PieceMove.FromStringNotation(firstMoveStringNotation);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);

            Assert.That(first.Equals(second), Is.EqualTo(expectedEquals));
            Assert.That(second.Equals(first), Is.EqualTo(expectedEquals));
            Assert.That(Equals(first, second), Is.EqualTo(expectedEquals));
            Assert.That(EqualityComparer<PieceMove>.Default.Equals(first, second), Is.EqualTo(expectedEquals));
            Assert.That(first == second, Is.EqualTo(expectedEquals));
            Assert.That(first != second, Is.EqualTo(!expectedEquals));

            if (expectedEquals)
            {
                Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            }
        }

        #endregion

        #region FromStringNotationCases Class

        public sealed class FromStringNotationCases : IEnumerable<TestCaseData>
        {
            #region IEnumerable<TestCaseData> Members

            public IEnumerator<TestCaseData> GetEnumerator()
            {
                yield return new TestCaseData("a1-c2", "a1", "c2", PieceType.None);
                yield return new TestCaseData("c1-g5", "c1", "g5", PieceType.None);
                yield return new TestCaseData("c7xd8=Q", "c7", "d8", PieceType.Queen);
                yield return new TestCaseData("h2xg1=R", "h2", "g1", PieceType.Rook);
                yield return new TestCaseData("a7-a8=B", "a7", "a8", PieceType.Bishop);
                yield return new TestCaseData("b2-b1=N", "b2", "b1", PieceType.Knight);
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region EqualityCases Class

        public sealed class EqualityCases : IEnumerable<TestCaseData>
        {
            #region IEnumerable<TestCaseData> Members

            public IEnumerator<TestCaseData> GetEnumerator()
            {
                yield return new TestCaseData("a1-g7", new PieceMove("a1", "g7"), true);
                yield return new TestCaseData("a2-a1", new PieceMove("a2", "a1", PieceType.Queen), false);
                yield return new TestCaseData("c2xb1=B", new PieceMove("c2", "b1", PieceType.Bishop), true);
                yield return new TestCaseData("c2xb1=B", new PieceMove("c2", "b1", PieceType.Rook), false);
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion
    }
}