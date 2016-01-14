using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Omnifactotum.NUnit;

//// ReSharper disable PossibleInvalidOperationException - Assertions are supposed to verify that

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class SquareTests
    {
        #region Tests

        [Test]
        public void TestConstructionBySquareIndex()
        {
            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var expectedRank = squareIndex / 8;
                var expectedFile = squareIndex % 8;

                var square = new Square(squareIndex);

                Assert.That(square.SquareIndex, Is.EqualTo(squareIndex));
                Assert.That(square.Rank, Is.EqualTo(expectedRank));
                Assert.That(square.File, Is.EqualTo(expectedFile));
                Assert.That(square.RankChar, Is.EqualTo((char)('1' + expectedRank)));
                Assert.That(square.FileChar, Is.EqualTo((char)('a' + expectedFile)));
                Assert.That(square.Bitboard.InternalValue, Is.EqualTo(1UL << squareIndex));
            }
        }

        [Test]
        [TestCase(-1)]
        [TestCase(ChessConstants.SquareCount)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestConstructionBySquareIndexNegativeCases(int squareIndex)
        {
            Assert.That(() => new Square(squareIndex), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestConstructionByFileAndRank()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var expectedSquareIndex = rank * 8 + file;

                    var square = new Square(file, rank);

                    Assert.That(square.File, Is.EqualTo(file));
                    Assert.That(square.Rank, Is.EqualTo(rank));

                    Assert.That(square.FileChar, Is.EqualTo((char)('a' + file)));
                    Assert.That(
                        square.RankChar.ToString(CultureInfo.InvariantCulture),
                        Is.EqualTo((rank + 1).ToString(CultureInfo.InvariantCulture)));

                    Assert.That(square.SquareIndex, Is.EqualTo(expectedSquareIndex));
                    Assert.That(square.Bitboard.InternalValue, Is.EqualTo(1UL << expectedSquareIndex));
                }
            }
        }

        [Test]
        public void TestConstructionByFileAndRankNegativeCases()
        {
            Assert.That(
                () => new Square(checked(ChessConstants.FileRange.Lower - 1), ChessConstants.RankRange.Lower),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new Square(checked(ChessConstants.FileRange.Upper + 1), ChessConstants.RankRange.Lower),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new Square(ChessConstants.FileRange.Lower, checked(ChessConstants.RankRange.Lower - 1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                () => new Square(ChessConstants.FileRange.Lower, checked(ChessConstants.RankRange.Upper + 1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestToString()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var square = new Square(file, rank);

                    var expectedString = GetSquareString(file, rank);
                    Assert.That(square.ToString(), Is.EqualTo(expectedString));
                }
            }
        }

        [Test]
        public void TestFromAlgebraicAndTryFromAlgebraic()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var squareString = GetSquareString(file, rank);
                    foreach (var useUpperCase in new[] { false, true })
                    {
                        var algebraicNotation = useUpperCase
                            ? squareString.ToUpperInvariant()
                            : squareString.ToLowerInvariant();

                        var fromAlgebraic = Square.FromAlgebraic(algebraicNotation);
                        Assert.That(fromAlgebraic.File, Is.EqualTo(file));
                        Assert.That(fromAlgebraic.Rank, Is.EqualTo(rank));

                        var tryFromAlgebraic = Square.TryFromAlgebraic(algebraicNotation).AssertNotNull();
                        Assert.That(tryFromAlgebraic.File, Is.EqualTo(file));
                        Assert.That(tryFromAlgebraic.Rank, Is.EqualTo(rank));
                    }
                }
            }
        }

        [Test]
        [TestCase(null)]
        [TestCase("1a")]
        [TestCase("a0")]
        [TestCase("b9")]
        [TestCase("i1")]
        [TestCase("a12")]
        public void TestFromAlgebraicAndTryFromAlgebraicNegativeCases(string algebraicNotation)
        {
            Assert.That(() => Square.FromAlgebraic(algebraicNotation), Throws.ArgumentException);
            Assert.That(Square.TryFromAlgebraic(algebraicNotation), Is.Null);
        }

        [Test]
        public void TestOperatorFromString()
        {
            var square = (Square)"a3";
            Assert.That(square.FileChar, Is.EqualTo('a'));
            Assert.That(square.RankChar, Is.EqualTo('3'));
            Assert.That(square.File, Is.EqualTo(0));
            Assert.That(square.Rank, Is.EqualTo(2));
        }

        [Test]
        public void TestOperatorFromStringNegativeCases()
        {
            Assert.That(() => (Square)(string)null, Throws.ArgumentException);
            Assert.That(() => (Square)"1a", Throws.ArgumentException);
            Assert.That(() => (Square)"a0", Throws.ArgumentException);
            Assert.That(() => (Square)"b9", Throws.ArgumentException);
            Assert.That(() => (Square)"i1", Throws.ArgumentException);
        }

        [Test]
        public void TestEquality()
        {
            for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
            {
                for (var rank = ChessConstants.RankRange.Lower; file <= ChessConstants.RankRange.Upper; file++)
                {
                    var square = new Square(file, rank);
                    var sameSquare = new Square(file, rank);
                    var otherFileSquare = new Square(file ^ 1, rank);
                    var otherRankSquare = new Square(file, rank ^ 1);

                    Assert.That(square, Is.EqualTo(sameSquare));
                    Assert.That(EqualityComparer<Square>.Default.Equals(square, sameSquare), Is.True);
                    Assert.That(square == sameSquare, Is.True);
                    Assert.That(square != sameSquare, Is.False);
                    Assert.That(square.GetHashCode(), Is.EqualTo(sameSquare.GetHashCode()));

                    Assert.That(square, Is.Not.EqualTo(otherFileSquare));
                    Assert.That(EqualityComparer<Square>.Default.Equals(square, otherFileSquare), Is.False);
                    Assert.That(square == otherFileSquare, Is.False);
                    Assert.That(square != otherFileSquare, Is.True);

                    Assert.That(square, Is.Not.EqualTo(otherRankSquare));
                    Assert.That(EqualityComparer<Square>.Default.Equals(square, otherRankSquare), Is.False);
                    Assert.That(square == otherRankSquare, Is.False);
                    Assert.That(square != otherRankSquare, Is.True);
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
            var square = new Square(squareIndex);
            var shift = new SquareShift(fileOffset, rankOffset);
            var actualResult = square + shift;

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

        private static string GetSquareString(int file, int rank)
        {
            return
                $@"{(char)('a' + (file - ChessConstants.FileRange.Lower))}{rank - ChessConstants.RankRange.Lower + 1}";
        }

        #endregion
    }
}