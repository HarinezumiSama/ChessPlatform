using System;
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

        #endregion
    }
}