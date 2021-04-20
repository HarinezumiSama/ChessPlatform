using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct Bitboard : IEquatable<Bitboard>
    {
        public const int NoSquareIndex = -1;

        public static readonly Bitboard None = new Bitboard(NoneValue);

        public static readonly Bitboard Everything = new Bitboard(~NoneValue);

        internal const ulong NoneValue = 0UL;

        #region Index64

        private static readonly int[] Index64 =
        {
            0,
            1,
            48,
            2,
            57,
            49,
            28,
            3,
            61,
            58,
            50,
            42,
            38,
            29,
            17,
            4,
            62,
            55,
            59,
            36,
            53,
            51,
            43,
            22,
            45,
            39,
            33,
            30,
            24,
            18,
            12,
            5,
            63,
            47,
            56,
            27,
            60,
            41,
            37,
            16,
            54,
            35,
            52,
            21,
            44,
            32,
            23,
            11,
            46,
            26,
            40,
            15,
            34,
            20,
            31,
            10,
            25,
            14,
            19,
            9,
            13,
            8,
            7,
            6
        };

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> structure
        ///     using the specified value.
        /// </summary>
        public Bitboard(long value)
        {
            InternalValue = (ulong)value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> structure
        ///     using the specified squares.
        /// </summary>
        public Bitboard(IEnumerable<Square> squares)
        {
            if (squares is null)
            {
                throw new ArgumentNullException(nameof(squares));
            }

            InternalValue = squares.Aggregate(
                NoneValue,
                (accumulator, square) => accumulator | square.Bitboard.InternalValue);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> structure
        ///     using the specified value.
        /// </summary>
        internal Bitboard(ulong value)
        {
            InternalValue = value;
        }

        public long Value
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (long)InternalValue;
        }

        public bool IsNone
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalValue == NoneValue;
        }

        public bool IsAny
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => InternalValue != NoneValue;
        }

        internal ulong InternalValue
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Bitboard left, Bitboard right)
        {
            return Equals(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Bitboard left, Bitboard right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Bitboard(long value)
        {
            return new Bitboard(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator ~(Bitboard obj)
        {
            return new Bitboard(~obj.InternalValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator &(Bitboard left, Bitboard right)
        {
            return new Bitboard(left.InternalValue & right.InternalValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator |(Bitboard left, Bitboard right)
        {
            return new Bitboard(left.InternalValue | right.InternalValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard operator ^(Bitboard left, Bitboard right)
        {
            return new Bitboard(left.InternalValue ^ right.InternalValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(Bitboard left, Bitboard right)
        {
            return left.InternalValue == right.InternalValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard FromSquareIndex(int squareIndex)
        {
            return new Bitboard(FromSquareIndexInternal(squareIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopFirstSquareIndex(ref Bitboard bitboard)
        {
            var value = bitboard.InternalValue;
            bitboard = new Bitboard(unchecked(value & (value - 1)));
            return FindFirstSquareIndexInternal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard PopFirstSquareBitboard(ref Bitboard bitboard)
        {
            var value = bitboard.InternalValue;
            bitboard = new Bitboard(unchecked(value & (value - 1)));
            return new Bitboard(IsolateFirstSquareInternal(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is Bitboard && Equals((Bitboard)obj);
        }

        public override int GetHashCode()
        {
            return InternalValue.GetHashCode();
        }

        public override string ToString()
        {
            var squares = InternalValue == NoneValue
                ? "<none>"
                : GetSquares().OrderBy(square => square.SquareIndex).Select(item => item.ToString()).Join(", ");

            return $@"{{ 0x{InternalValue:X16} : {squares} }}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindFirstSquareIndex()
        {
            return FindFirstSquareIndexInternal(InternalValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExactlyOneSquare()
        {
            return InternalValue != NoneValue && IsolateFirstSquareInternal(InternalValue) == InternalValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard IsolateFirstSquare()
        {
            return new Bitboard(IsolateFirstSquareInternal(InternalValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard Shift(ShiftDirection direction)
        {
            return new Bitboard(ShiftInternal(InternalValue, direction));
        }

        public Square[] GetSquares()
        {
            var resultList = new List<Square>(ChessConstants.SquareCount);

            var currentValue = InternalValue;

            int index;
            while ((index = FindFirstSquareIndexInternal(currentValue)) >= 0)
            {
                resultList.Add(new Square(index));
                currentValue &= ~(1UL << index);
            }

            return resultList.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Square GetFirstSquare()
        {
            if (IsNone)
            {
                throw new InvalidOperationException("The bitboard represents no squares.");
            }

            var squareIndex = FindFirstSquareIndex();
            return new Square(squareIndex);
        }

        public int GetSquareCount()
        {
            var result = 0;

            var currentValue = InternalValue;
            while (PopFirstSquareIndexInternal(ref currentValue) >= 0)
            {
                result++;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Bitboard other)
        {
            return Equals(this, other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong FromSquareIndexInternal(int squareIndex)
        {
            Square.EnsureValidSquareIndex(squareIndex);

            return 1UL << squareIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ShiftInternal(ulong value, ShiftDirection direction)
        {
            switch (direction)
            {
                case ShiftDirection.North:
                    return (value & ~Bitboards.Rank8Value) << 8;

                case ShiftDirection.NorthEast:
                    return (value & ~Bitboards.Rank8WithFileHValue) << 9;

                case ShiftDirection.East:
                    return (value & ~Bitboards.FileHValue) << 1;

                case ShiftDirection.SouthEast:
                    return (value & ~Bitboards.Rank1WithFileHValue) >> 7;

                case ShiftDirection.South:
                    return (value & ~Bitboards.Rank1Value) >> 8;

                case ShiftDirection.SouthWest:
                    return (value & ~Bitboards.Rank1WithFileAValue) >> 9;

                case ShiftDirection.West:
                    return (value & ~Bitboards.FileAValue) >> 1;

                case ShiftDirection.NorthWest:
                    return (value & ~Bitboards.Rank8WithFileAValue) << 7;

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid shift direction.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int PopFirstSquareIndexInternal(ref ulong bitboard)
        {
            var value = bitboard;
            bitboard = unchecked(value & (value - 1));
            return FindFirstSquareIndexInternal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong IsolateFirstSquareInternal(ulong value)
        {
            //// ReSharper disable once ArrangeRedundantParentheses
            return unchecked(value & (ulong)(-(long)value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong PopFirstSquareBitboardInternal(ref ulong bitboard)
        {
            var value = bitboard;
            bitboard = unchecked(value & (value - 1));
            return IsolateFirstSquareInternal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FindFirstSquareIndexInternal(ulong value)
        {
            if (value == NoneValue)
            {
                return NoSquareIndex;
            }

            const long Debruijn64 = 0x03F79D71B4CB0A89L;
            const int MagicShift = 58;

            var isolatedBit = IsolateFirstSquareInternal(value);
            return Index64[unchecked(isolatedBit * Debruijn64) >> MagicShift];
        }
    }
}