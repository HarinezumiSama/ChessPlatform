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
        [TestCaseSource(typeof(ConstructionWithPromotionArgumentCases))]
        public void TestConstructionWithPromotionArgument(Position from, Position to, PieceType promotion)
        {
            var move = new PieceMove(from, to, promotion);
            Assert.That(move.From, Is.EqualTo(from));
            Assert.That(move.To, Is.EqualTo(to));
        }

        #endregion

        #region ConstructionWithPromotionArgumentCaseData Class

        public sealed class ConstructionWithPromotionArgumentCaseData : TestCaseData
        {
            #region Constructors

            internal ConstructionWithPromotionArgumentCaseData(Position from, Position to, PieceType promotion)
                : base(from, to, promotion)
            {
                this.From = from;
                this.To = to;
                this.Promotion = promotion;
            }

            #endregion

            #region Public Properties

            public Position From
            {
                get;
                private set;
            }

            public Position To
            {
                get;
                private set;
            }

            public PieceType Promotion
            {
                get;
                private set;
            }

            #endregion
        }

        #endregion

        #region ConstructionWithPromotionArgumentCases Class

        public sealed class ConstructionWithPromotionArgumentCases : IEnumerable<TestCaseData>
        {
            #region IEnumerable<TestCaseData> Members

            public IEnumerator<TestCaseData> GetEnumerator()
            {
                var promotionIndex = 0;
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
                        var promotion = ValidPromotionArguments[promotionIndex % ValidPromotionArguments.Length];
                        promotionIndex++;

                        yield return new ConstructionWithPromotionArgumentCaseData(from, to, promotion);
                    }
                }
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