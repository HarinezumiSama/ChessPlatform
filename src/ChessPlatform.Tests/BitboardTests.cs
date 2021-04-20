using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Omnifactotum.NUnit;

namespace ChessPlatform.Tests
{
    [TestFixture]
    public sealed class BitboardTests
    {
        [Test]
        public void TestBitboardConstants()
        {
            Assert.That(Bitboard.NoSquareIndex, Is.LessThan(0));
            Assert.That(Bitboard.None.Value, Is.EqualTo(0L));
            Assert.That(Bitboard.Everything.Value, Is.EqualTo(~0L));
        }

        [Test]
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(0x1234567890ABCDEFL)]
        public void TestConstructionFromValue(long value)
        {
            var bitboard = new Bitboard(value);
            Assert.That(bitboard.Value, Is.EqualTo(value));
            Assert.That(bitboard.InternalValue, Is.EqualTo((ulong)value));
            Assert.That(bitboard.IsNone, Is.EqualTo(value == 0));
            Assert.That(bitboard.IsAny, Is.EqualTo(value != 0));
        }

        [Test]
        [TestCase(0L, new string[0])]
        [TestCase(1L, new[] { "a1" })]
        [TestCase(1L << 1, new[] { "b1" })]
        [TestCase(1L << 49, new[] { "b7" })]
        [TestCase((1L << 49) | (1L << 23), new[] { "b7", "h3" })]
        [TestCase((1L << 1) | (1L << 59), new[] { "b1", "d8" })]
        public void TestConstructionFromSquares(long expectedValue, params string[] squareNotations)
        {
            Assert.That(squareNotations, Is.Not.Null);
            var squares = squareNotations.Select(Square.FromAlgebraic).ToArray();

            var bitboard = new Bitboard(squares);
            Assert.That(bitboard.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void TestFindFirstSquareIndexAndFindFirstSquareIndexInternalWhenNoBitsAreSet()
        {
            Assert.That(Bitboard.NoSquareIndex, Is.LessThan(0));

            const long Value = 0L;
            var bitboard = new Bitboard(Value);

            var firstSquareIndex = bitboard.FindFirstSquareIndex();
            Assert.That(firstSquareIndex, Is.EqualTo(Bitboard.NoSquareIndex));

            var firstSquareIndexInternal = Bitboard.FindFirstSquareIndexInternal(Value);
            Assert.That(firstSquareIndexInternal, Is.EqualTo(Bitboard.NoSquareIndex));
        }

        [Test]
        public void TestFindFirstSquareIndexAndFindFirstSquareIndexInternalWhenSingleBitIsSet()
        {
            for (var index = 0; index < ChessConstants.SquareCount; index++)
            {
                var value = 1L << index;
                var bitboard = new Bitboard(value);

                var firstSquareIndex = bitboard.FindFirstSquareIndex();
                Assert.That(firstSquareIndex, Is.EqualTo(index), "Failed for the bit {0}.", index);

                var firstSquareIndexInternal = Bitboard.FindFirstSquareIndexInternal((ulong)value);
                Assert.That(firstSquareIndexInternal, Is.EqualTo(index), "Failed for the bit {0}.", index);
            }
        }

        [Test]
        [TestCase((1L << 0) | (1L << 17), 0)]
        [TestCase((1L << 49) | (1L << 23), 23)]
        public void TestFindFirstSquareIndexAndFindFirstSquareIndexInternal(long value, int expectedResult)
        {
            var bitboard = new Bitboard(value);

            var firstSquareIndex = bitboard.FindFirstSquareIndex();
            Assert.That(firstSquareIndex, Is.EqualTo(expectedResult));

            var firstSquareIndexInternal = Bitboard.FindFirstSquareIndexInternal((ulong)value);
            Assert.That(firstSquareIndexInternal, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(0L, new int[0])]
        [TestCase(1L, new[] { 0 })]
        [TestCase(1L << 1, new[] { 1 })]
        [TestCase(1L << 49, new[] { 49 })]
        [TestCase((1L << 49) | (1L << 23), new[] { 49, 23 })]
        [TestCase((1L << 1) | (1L << 59), new[] { 1, 59 })]
        [TestCase((1L << 2) | (1L << 11) | (1L << 34), new[] { 2, 11, 34 })]
        public void TestGetSquaresAndGetFirstSquareAndGetSquareCountAndIsExactlyOneSquare(
            long value,
            params int[] expectedIndexesResult)
        {
            Assert.That(expectedIndexesResult, Is.Not.Null);

            var expectedGetSquares = expectedIndexesResult.Select(squareIndex => new Square(squareIndex)).ToArray();
            var expectedGetBitSetCount = expectedGetSquares.Length;
            var expectedIsExactlyOneBitSet = expectedIndexesResult.Length == 1;

            var expectedGetFirstSquareIndex = expectedIndexesResult.Length == 0
                ? default(int?)
                : expectedIndexesResult.Min();

            var bitboard = new Bitboard(value);

            Assert.That(bitboard.GetSquares(), Is.EquivalentTo(expectedGetSquares));
            Assert.That(bitboard.GetSquareCount(), Is.EqualTo(expectedGetBitSetCount));
            Assert.That(bitboard.IsExactlyOneSquare(), Is.EqualTo(expectedIsExactlyOneBitSet));

            Assert.That(
                () => bitboard.GetFirstSquare().SquareIndex,
                expectedGetFirstSquareIndex.HasValue
                    ? (IResolveConstraint)Is.EqualTo(expectedGetFirstSquareIndex)
                    : Throws.InvalidOperationException);
        }

        [Test]
        [TestCase(0L, 0L)]
        [TestCase(1L << 1, 1L << 1)]
        [TestCase(1L << 49, 1L << 49)]
        [TestCase((1L << 49) | (1L << 23), 1L << 23)]
        [TestCase((1L << 1) | (1L << 59), 1L << 1)]
        public void TestIsolateFirstSquareAndIsolateFirstSquareInternal(long value, long expectedResult)
        {
            var bitboard = new Bitboard(value);
            var result = bitboard.IsolateFirstSquare();
            Assert.That(result.Value, Is.EqualTo(expectedResult));

            var isolatedFirstSquareInternalValue = Bitboard.IsolateFirstSquareInternal((ulong)value);
            Assert.That(isolatedFirstSquareInternalValue, Is.EqualTo((ulong)expectedResult));
        }

        [Test]
        [TestCase("a1", ShiftDirection.North, "a2")]
        [TestCase("a1", ShiftDirection.NorthEast, "b2")]
        [TestCase("a1", ShiftDirection.East, "b1")]
        [TestCase("a1", ShiftDirection.SouthEast, null)]
        [TestCase("a1", ShiftDirection.South, null)]
        [TestCase("a1", ShiftDirection.SouthWest, null)]
        [TestCase("a1", ShiftDirection.West, null)]
        [TestCase("a1", ShiftDirection.NorthWest, null)]
        [TestCase("a8", ShiftDirection.North, null)]
        [TestCase("a8", ShiftDirection.NorthEast, null)]
        [TestCase("a8", ShiftDirection.East, "b8")]
        [TestCase("a8", ShiftDirection.SouthEast, "b7")]
        [TestCase("a8", ShiftDirection.South, "a7")]
        [TestCase("a8", ShiftDirection.SouthWest, null)]
        [TestCase("a8", ShiftDirection.West, null)]
        [TestCase("a8", ShiftDirection.NorthWest, null)]
        [TestCase("h8", ShiftDirection.North, null)]
        [TestCase("h8", ShiftDirection.NorthEast, null)]
        [TestCase("h8", ShiftDirection.East, null)]
        [TestCase("h8", ShiftDirection.SouthEast, null)]
        [TestCase("h8", ShiftDirection.South, "h7")]
        [TestCase("h8", ShiftDirection.SouthWest, "g7")]
        [TestCase("h8", ShiftDirection.West, "g8")]
        [TestCase("h8", ShiftDirection.NorthWest, null)]
        [TestCase("h1", ShiftDirection.North, "h2")]
        [TestCase("h1", ShiftDirection.NorthEast, null)]
        [TestCase("h1", ShiftDirection.East, null)]
        [TestCase("h1", ShiftDirection.SouthEast, null)]
        [TestCase("h1", ShiftDirection.South, null)]
        [TestCase("h1", ShiftDirection.SouthWest, null)]
        [TestCase("h1", ShiftDirection.West, "g1")]
        [TestCase("h1", ShiftDirection.NorthWest, "g2")]
        [TestCase("e2", ShiftDirection.North, "e3")]
        [TestCase("e2", ShiftDirection.NorthEast, "f3")]
        [TestCase("e2", ShiftDirection.East, "f2")]
        [TestCase("e2", ShiftDirection.SouthEast, "f1")]
        [TestCase("e2", ShiftDirection.South, "e1")]
        [TestCase("e2", ShiftDirection.SouthWest, "d1")]
        [TestCase("e2", ShiftDirection.West, "d2")]
        [TestCase("e2", ShiftDirection.NorthWest, "d3")]
        public void TestShiftAndShiftInternal(
            string squareNotation,
            ShiftDirection direction,
            string expectedResultSquareNotation)
        {
            var bitboard = Square.FromAlgebraic(squareNotation).Bitboard;
            var resultBitboard = bitboard.Shift(direction);
            var resultValue = Bitboard.ShiftInternal(bitboard.InternalValue, direction);

            if (expectedResultSquareNotation is null)
            {
                Assert.That(resultBitboard.IsNone, Is.True);
                Assert.That(resultValue, Is.EqualTo(0UL));
                return;
            }

            var expectedResultBitboard = Square.FromAlgebraic(expectedResultSquareNotation).Bitboard;
            Assert.That(resultBitboard.Value, Is.EqualTo(expectedResultBitboard.Value));
            Assert.That(resultValue, Is.EqualTo(expectedResultBitboard.InternalValue));
        }

        [Test]
        [TestCase((ShiftDirection)int.MinValue)]
        [TestCase((ShiftDirection)0)]
        [TestCase((ShiftDirection)int.MaxValue)]
        public void TestShiftAndShiftInternalNegativeCases(ShiftDirection direction)
        {
            var bitboard = Square.FromAlgebraic("a1").Bitboard;

            const string ParameterName = "direction";

            Assert.That(
                () => bitboard.Shift(direction),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With
                    .Property(nameof(ArgumentOutOfRangeException.ParamName))
                    .EqualTo(ParameterName));

            Assert.That(
                () => Bitboard.ShiftInternal(bitboard.InternalValue, direction),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With
                    .Property(nameof(ArgumentOutOfRangeException.ParamName))
                    .EqualTo(ParameterName));
        }

        [Test]
        [TestCase(0x123456789ABCDEFul, 0x123456789ABCDEFul, true)]
        [TestCase(0x0ul, 0x0ul, true)]
        [TestCase(0x0ul, 0x1ul, false)]
        [TestCase(0x123456789ABCDEFul, 0x123456789ABCDEEul, false)]
        [TestCase(0x123456789ABCDEFul, 0x223456789ABCDEFul, false)]
        public void TestEquality(ulong value1, ulong value2, bool shouldBeEqual)
        {
            var bitboard1 = new Bitboard(value1);
            var bitboard2 = new Bitboard(value2);
            var expectation = shouldBeEqual
                ? AssertEqualityExpectation.EqualAndCannotBeSame
                : AssertEqualityExpectation.NotEqual;

            NUnitFactotum.AssertEquality(bitboard1, bitboard2, expectation);
            Assert.That(Bitboard.Equals(bitboard1, bitboard2), Is.EqualTo(shouldBeEqual));
            Assert.That(bitboard1 == bitboard2, Is.EqualTo(shouldBeEqual));
            Assert.That(bitboard1 != bitboard2, Is.Not.EqualTo(shouldBeEqual));
        }

        [Test]
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(-1L)]
        [TestCase(0x123456789aL)]
        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        public void TestExplicitCastOperator(long value)
        {
            var bitboard = (Bitboard)value;
            Assert.That(bitboard.Value, Is.EqualTo(value));
        }

        [Test]
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(-1L)]
        [TestCase(0xa987654321L)]
        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        public void TestBitwiseNotOperator(long value)
        {
            var bitboard = new Bitboard(value);
            var notBitboard = ~bitboard;

            var expectedValue = ~value;
            Assert.That(notBitboard.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(0L, 0L)]
        [TestCase(0L, 1L)]
        [TestCase(1L, 2L)]
        [TestCase(-1L, 0x1234L)]
        [TestCase(0xa987654321L, 0x123456789aL)]
        [TestCase(long.MinValue, 0x1357924680L)]
        [TestCase(long.MaxValue, 0x1357924680L)]
        public void TestBitwiseAndOperator(long operand1, long operand2)
        {
            var expectedValue = operand1 & operand2;

            var bitboard1 = new Bitboard(operand1);
            var bitboard2 = new Bitboard(operand2);

            var resultBitboard12 = bitboard1 & bitboard2;
            Assert.That(resultBitboard12.Value, Is.EqualTo(expectedValue));

            var resultBitboard21 = bitboard2 & bitboard1;
            Assert.That(resultBitboard21.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(0L, 0L)]
        [TestCase(0L, 1L)]
        [TestCase(1L, 2L)]
        [TestCase(-1L, 0x1234L)]
        [TestCase(0xa987654321L, 0x123456789aL)]
        [TestCase(long.MinValue, 0x1357924680L)]
        [TestCase(long.MaxValue, 0x1357924680L)]
        public void TestBitwiseOrOperator(long operand1, long operand2)
        {
            var expectedValue = operand1 | operand2;

            var bitboard1 = new Bitboard(operand1);
            var bitboard2 = new Bitboard(operand2);

            var resultBitboard12 = bitboard1 | bitboard2;
            Assert.That(resultBitboard12.Value, Is.EqualTo(expectedValue));

            var resultBitboard21 = bitboard2 | bitboard1;
            Assert.That(resultBitboard21.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(0L, 0L)]
        [TestCase(0L, 1L)]
        [TestCase(1L, 2L)]
        [TestCase(-1L, 0x1234L)]
        [TestCase(0xa987654321L, 0x123456789aL)]
        [TestCase(long.MinValue, 0x1357924680L)]
        [TestCase(long.MaxValue, 0x1357924680L)]
        public void TestBitwiseExclusiveOrOperator(long operand1, long operand2)
        {
            var expectedValue = operand1 ^ operand2;

            var bitboard1 = new Bitboard(operand1);
            var bitboard2 = new Bitboard(operand2);

            var resultBitboard12 = bitboard1 ^ bitboard2;
            Assert.That(resultBitboard12.Value, Is.EqualTo(expectedValue));

            var resultBitboard21 = bitboard2 ^ bitboard1;
            Assert.That(resultBitboard21.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void TestFromSquareIndexAndFromSquareIndexInternal()
        {
            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var expectedValue = 1UL << squareIndex;

                var bitboard = Bitboard.FromSquareIndex(squareIndex);
                Assert.That(bitboard.InternalValue, Is.EqualTo(expectedValue));

                var internalValue = Bitboard.FromSquareIndexInternal(squareIndex);
                Assert.That(internalValue, Is.EqualTo(expectedValue));
            }
        }

        [Test]
        [TestCase(-1)]
        [TestCase(ChessConstants.MaxSquareIndex + 1)]
        public void TestFromSquareIndexAndFromSquareIndexInternalNegativeCases(int squareIndex)
        {
            const string ParameterName = "squareIndex";

            Assert.That(
                () => Bitboard.FromSquareIndex(squareIndex),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With
                    .Property(nameof(ArgumentOutOfRangeException.ParamName))
                    .EqualTo(ParameterName));

            Assert.That(
                () => Bitboard.FromSquareIndexInternal(squareIndex),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With
                    .Property(nameof(ArgumentOutOfRangeException.ParamName))
                    .EqualTo(ParameterName));
        }

        [Test]
        public void TestPopFirstSquareIndexAndPopFirstSquareBitboardAndTheirInternals()
        {
            const int Bit1 = 2;
            const int Bit2 = 23;
            const int Bit3 = 49;

            var bitboard1 = new Bitboard((1L << Bit1) | (1L << Bit2) | (1L << Bit3));
            var bitboard2 = bitboard1;

            var internalValue1 = bitboard1.InternalValue;
            var internalValue2 = internalValue1;

            Assert.That(Bitboard.PopFirstSquareIndex(ref bitboard1), Is.EqualTo(Bit1));
            Assert.That(Bitboard.PopFirstSquareIndexInternal(ref internalValue1), Is.EqualTo(Bit1));
            Assert.That(Bitboard.PopFirstSquareBitboard(ref bitboard2).InternalValue, Is.EqualTo(1UL << Bit1));
            Assert.That(Bitboard.PopFirstSquareBitboardInternal(ref internalValue2), Is.EqualTo(1UL << Bit1));

            Assert.That(Bitboard.PopFirstSquareIndex(ref bitboard1), Is.EqualTo(Bit2));
            Assert.That(Bitboard.PopFirstSquareIndexInternal(ref internalValue1), Is.EqualTo(Bit2));
            Assert.That(Bitboard.PopFirstSquareBitboard(ref bitboard2).InternalValue, Is.EqualTo(1UL << Bit2));
            Assert.That(Bitboard.PopFirstSquareBitboardInternal(ref internalValue2), Is.EqualTo(1UL << Bit2));

            Assert.That(Bitboard.PopFirstSquareIndex(ref bitboard1), Is.EqualTo(Bit3));
            Assert.That(Bitboard.PopFirstSquareIndexInternal(ref internalValue1), Is.EqualTo(Bit3));
            Assert.That(Bitboard.PopFirstSquareBitboard(ref bitboard2).InternalValue, Is.EqualTo(1UL << Bit3));
            Assert.That(Bitboard.PopFirstSquareBitboardInternal(ref internalValue2), Is.EqualTo(1UL << Bit3));

            Assert.That(bitboard1.IsNone, Is.True);
            Assert.That(internalValue1, Is.EqualTo(0UL));
            Assert.That(bitboard2.IsNone, Is.True);
            Assert.That(internalValue2, Is.EqualTo(0UL));

            Assert.That(Bitboard.PopFirstSquareIndex(ref bitboard1), Is.EqualTo(Bitboard.NoSquareIndex));
            Assert.That(Bitboard.PopFirstSquareIndexInternal(ref internalValue1), Is.EqualTo(Bitboard.NoSquareIndex));
            Assert.That(Bitboard.PopFirstSquareBitboard(ref bitboard2).InternalValue, Is.EqualTo(0UL));
            Assert.That(Bitboard.PopFirstSquareBitboardInternal(ref internalValue2), Is.EqualTo(0UL));

            Assert.That(bitboard1.IsNone, Is.True);
            Assert.That(internalValue1, Is.EqualTo(0UL));
            Assert.That(bitboard2.IsNone, Is.True);
            Assert.That(internalValue2, Is.EqualTo(0UL));
        }

        [Test]
        [Explicit]
        public void TestPerformance()
        {
            const int Count = 1000 * 1000 * 1000;
            var value1 = Bitboard.FromSquareIndexInternal(1);
            var value2 = Bitboard.FromSquareIndexInternal(2);
            var value3 = value1 | value2;

            var bitboardStopwatch = Stopwatch.StartNew();
            for (var index = 0; index < Count; index++)
            {
                var bitboard1 = new Bitboard(value1);
                var bitboard2 = new Bitboard(value2);
                var bitboard3 = new Bitboard(value3);

                var bitboard = (bitboard1 | bitboard2) & ~bitboard3;
                if (bitboard.IsAny)
                {
                    Assert.Fail();
                }
            }

            bitboardStopwatch.Stop();
            Console.WriteLine(@"Bitboard performance @ {0}: {1}", Count, bitboardStopwatch.Elapsed);

            var valueStopwatch = Stopwatch.StartNew();
            for (var index = 0; index < Count; index++)
            {
                var value = (value1 | value2) & ~value3;
                if (value != 0)
                {
                    Assert.Fail();
                }
            }

            valueStopwatch.Stop();
            Console.WriteLine(@"Value performance @ {0}: {1}", Count, valueStopwatch.Elapsed);

            var ratio = (double)bitboardStopwatch.ElapsedTicks / valueStopwatch.ElapsedTicks;
            Console.WriteLine(@"Ratio: {0}", ratio);
        }
    }
}