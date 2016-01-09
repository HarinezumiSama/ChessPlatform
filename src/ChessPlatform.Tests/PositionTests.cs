using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

//// ReSharper disable PossibleInvalidOperationException - Assertions are supposed to verify that

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class PositionTests
    {
        #region Tests

        [Test]
        public void TestConstructionBySquareIndex()
        {
            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var position = new Position(squareIndex);
                Assert.That(position.SquareIndex, Is.EqualTo(squareIndex));
                Assert.That(position.Rank, Is.EqualTo(squareIndex / 8));
                Assert.That(position.File, Is.EqualTo(squareIndex % 8));
            }
        }

        [Test]
        [TestCase(-1)]
        [TestCase(ChessConstants.SquareCount)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestConstructionBySquareIndexNegativeCases(int squareIndex)
        {
            Assert.That(() => new Position(squareIndex), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestConstructionByFileAndRank()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var expectedSquareIndex = rank * 8 + file;

                    var position = new Position(file, rank);

                    Assert.That(position.File, Is.EqualTo(file));
                    Assert.That(position.Rank, Is.EqualTo(rank));

                    Assert.That(position.FileChar, Is.EqualTo((char)('a' + file)));
                    Assert.That(
                        position.RankChar.ToString(CultureInfo.InvariantCulture),
                        Is.EqualTo((rank + 1).ToString(CultureInfo.InvariantCulture)));

                    Assert.That(position.SquareIndex, Is.EqualTo(expectedSquareIndex));
                    Assert.That(position.Bitboard.InternalValue, Is.EqualTo(1UL << expectedSquareIndex));
                }
            }
        }

        [Test]
        public void TestConstructionByFileAndRankNegativeCases()
        {
            Assert.That(
                () => new Position(checked(ChessConstants.FileRange.Lower - 1), ChessConstants.RankRange.Lower),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new Position(checked(ChessConstants.FileRange.Upper + 1), ChessConstants.RankRange.Lower),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new Position(ChessConstants.FileRange.Lower, checked(ChessConstants.RankRange.Lower - 1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new Position(ChessConstants.FileRange.Lower, checked(ChessConstants.RankRange.Upper + 1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
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
                    var positionString = GetPositionString(file, rank);
                    foreach (var useUpperCase in new[] { false, true })
                    {
                        var algebraicNotation = useUpperCase
                            ? positionString.ToUpperInvariant()
                            : positionString.ToLowerInvariant();

                        var actualValue = Position.FromAlgebraic(algebraicNotation);

                        Assert.That(actualValue.File, Is.EqualTo(file));
                        Assert.That(actualValue.Rank, Is.EqualTo(rank));
                    }
                }
            }
        }

        [Test]
        public void TestFromAlgebraicNegativeCases()
        {
            Assert.That(() => Position.FromAlgebraic(null), Throws.ArgumentException);
            Assert.That(() => Position.FromAlgebraic("1a"), Throws.ArgumentException);
            Assert.That(() => Position.FromAlgebraic("a0"), Throws.ArgumentException);
            Assert.That(() => Position.FromAlgebraic("b9"), Throws.ArgumentException);
            Assert.That(() => Position.FromAlgebraic("i1"), Throws.ArgumentException);
        }

        [Test]
        public void TestOperatorFromString()
        {
            var position = (Position)"a3";
            Assert.That(position.File, Is.EqualTo(0));
            Assert.That(position.Rank, Is.EqualTo(2));
        }

        [Test]
        public void TestOperatorFromStringNegativeCases()
        {
            Assert.That(() => (Position)(string)null, Throws.ArgumentException);
            Assert.That(() => (Position)"1a", Throws.ArgumentException);
            Assert.That(() => (Position)"a0", Throws.ArgumentException);
            Assert.That(() => (Position)"b9", Throws.ArgumentException);
            Assert.That(() => (Position)"i1", Throws.ArgumentException);
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

        [Test]
        [TestCase(0, 0, 0, 0)]
        [TestCase(0, -1, 0, null)]
        [TestCase(0, 0, -1, null)]
        [TestCase(0, 8, 0, null)]
        [TestCase(0, 0, 8, null)]
        [TestCase(0, 3, 4, 35)]
        [TestCase(35, -3, -4, 0)]
        public void TestAdditionWithSquareShift(
            int squareIndex,
            int fileOffset,
            int rankOffset,
            int? expectedResultSquareIndex)
        {
            var position = new Position(squareIndex);
            var shift = new SquareShift(fileOffset, rankOffset);
            var actualResult = position + shift;

            if (expectedResultSquareIndex.HasValue)
            {
                Assert.That(actualResult.HasValue, Is.True);
                Assert.That(actualResult.Value.SquareIndex, Is.EqualTo(expectedResultSquareIndex.Value));
            }
            else
            {
                Assert.That(actualResult.HasValue, Is.False);
            }
        }

        #endregion

        #region Private Methods

        private static string GetPositionString(int file, int rank)
        {
            return
                $@"{(char)('a' + (file - ChessConstants.FileRange.Lower))}{rank - ChessConstants.RankRange.Lower + 1}";
        }

        #endregion
    }
}