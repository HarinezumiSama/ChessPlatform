using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PositionTests
    {
        #region Tests

        [Test]
        public void TestConstructionByFileAndRank()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var position = new Position(file, rank);

                    Assert.That(position.File, Is.EqualTo(file));
                    Assert.That(position.Rank, Is.EqualTo(rank));
                }
            }
        }

        [Test]
        public void TestToString()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var position = new Position(file, rank);

                    var expectedString = GetPositionString(file, rank);
                    Assert.That(position.ToString(), Is.EqualTo(expectedString));
                }
            }
        }

        [Test]
        public void TestFromAlgebraic()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var algebraicNotation = GetPositionString(file, rank);
                    var position = Position.FromAlgebraic(algebraicNotation);

                    Assert.That(position.File, Is.EqualTo(file));
                    Assert.That(position.Rank, Is.EqualTo(rank));
                }
            }
        }

        [Test]
        public void TestEquality()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var position = new Position(file, rank);
                    var samePosition = new Position(file, rank);
                    var otherFilePosition = new Position(file ^ 1, rank);
                    var otherRankPosition = new Position(file, rank ^ 1);

                    Assert.That(position, Is.EqualTo(samePosition));
                    Assert.That(EqualityComparer<Position>.Default.Equals(position, samePosition), Is.True);
                    Assert.That(position == samePosition, Is.True);
                    Assert.That(position != samePosition, Is.False);
                    Assert.That(position.GetHashCode(), Is.EqualTo(samePosition.GetHashCode()));

                    Assert.That(position, Is.Not.EqualTo(otherFilePosition));
                    Assert.That(EqualityComparer<Position>.Default.Equals(position, otherFilePosition), Is.False);
                    Assert.That(position == otherFilePosition, Is.False);
                    Assert.That(position != otherFilePosition, Is.True);

                    Assert.That(position, Is.Not.EqualTo(otherRankPosition));
                    Assert.That(EqualityComparer<Position>.Default.Equals(position, otherRankPosition), Is.False);
                    Assert.That(position == otherRankPosition, Is.False);
                    Assert.That(position != otherRankPosition, Is.True);
                }
            }
        }

        #endregion

        #region Private Methods

        private static string GetPositionString(int file, int rank)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}",
                (char)('a' + (file - ChessConstants.FileRange.Lower)),
                rank - ChessConstants.RankRange.Lower + 1);
        }

        #endregion
    }
}