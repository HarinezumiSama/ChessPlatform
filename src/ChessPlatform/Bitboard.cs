using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum;

namespace ChessPlatform
{
    [DebuggerDisplay("{ToString(),nq}")]
    public struct Bitboard : IEquatable<Bitboard>
    {
        #region Constants and Fields

        public const int NoBitSetIndex = -1;

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

        #endregion

        #region Constructors

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
        ///     using the specified positions.
        /// </summary>
        public Bitboard(IEnumerable<Position> positions)
        {
            #region Argument Check

            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            #endregion

            InternalValue = positions.Aggregate(NoneValue, (accumulator, position) => accumulator | position.Bitboard.InternalValue);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> structure
        ///     using the specified value.
        /// </summary>
        internal Bitboard(ulong value)
        {
            InternalValue = value;
        }

        #endregion

        #region Public Properties

        public long Value
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (long)InternalValue;
            }
        }

        public bool IsNone
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return InternalValue == NoneValue;
            }
        }

        public bool IsAny
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return InternalValue != NoneValue;
            }
        }

        #endregion

        #region Internal Properties

        internal ulong InternalValue
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        #endregion

        #region Operators

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

        #endregion

        #region Public Methods

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
        public static int PopFirstBitSetIndex(ref Bitboard bitboard)
        {
            var value = bitboard.InternalValue;
            bitboard = new Bitboard(unchecked(value & (value - 1)));
            return FindFirstBitSetIndexInternal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitboard PopFirstBitSet(ref Bitboard bitboard)
        {
            var value = bitboard.InternalValue;
            bitboard = new Bitboard(unchecked(value & (value - 1)));
            return new Bitboard(IsolateFirstBitSetInternal(value));
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
                : GetPositions().Select(item => item.ToString()).OrderBy(Factotum.Identity).Join(", ");

            return $@"{{ {InternalValue:X16} : {squares} }}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindFirstBitSetIndex()
        {
            return FindFirstBitSetIndexInternal(InternalValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExactlyOneBitSet()
        {
            return InternalValue != NoneValue && IsolateFirstBitSetInternal(InternalValue) == InternalValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard IsolateFirstBitSet()
        {
            return new Bitboard(IsolateFirstBitSetInternal(InternalValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard Shift(ShiftDirection direction)
        {
            return new Bitboard(ShiftInternal(InternalValue, direction));
        }

        public Position[] GetPositions()
        {
            var resultList = new List<Position>(ChessConstants.SquareCount);

            var currentValue = InternalValue;

            int index;
            while ((index = FindFirstBitSetIndexInternal(currentValue)) >= 0)
            {
                resultList.Add(Position.FromSquareIndex(index));
                currentValue &= ~(1UL << index);
            }

            return resultList.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Position GetFirstPosition()
        {
            var squareIndex = FindFirstBitSetIndex();
            return Position.FromSquareIndex(squareIndex);
        }

        public int GetBitSetCount()
        {
            var result = 0;

            var currentValue = InternalValue;
            while (PopFirstBitSetIndexInternal(ref currentValue) >= 0)
            {
                result++;
            }

            return result;
        }

        #endregion

        #region IEquatable<Bitboard> Members

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Bitboard other)
        {
            return Equals(this, other);
        }

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong FromSquareIndexInternal(int squareIndex)
        {
            return 1UL << squareIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ShiftInternal(ulong value, ShiftDirection direction)
        {
            //// Using if-s instead of a single switch for optimization

            if (direction == ShiftDirection.North)
            {
                return (value & ~Bitboards.Rank8Value) << 8;
            }

            if (direction == ShiftDirection.NorthEast)
            {
                return (value & ~Bitboards.Rank8WithFileHValue) << 9;
            }

            if (direction == ShiftDirection.East)
            {
                return (value & ~Bitboards.FileHValue) << 1;
            }

            if (direction == ShiftDirection.SouthEast)
            {
                return (value & ~Bitboards.Rank1WithFileHValue) >> 7;
            }

            if (direction == ShiftDirection.South)
            {
                return (value & ~Bitboards.Rank1Value) >> 8;
            }

            if (direction == ShiftDirection.SouthWest)
            {
                return (value & ~Bitboards.Rank1WithFileAValue) >> 9;
            }

            if (direction == ShiftDirection.West)
            {
                return (value & ~Bitboards.FileAValue) >> 1;
            }

            if (direction == ShiftDirection.NorthWest)
            {
                return (value & ~Bitboards.Rank8WithFileAValue) << 7;
            }

            return NoneValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int PopFirstBitSetIndexInternal(ref ulong bitboard)
        {
            var value = bitboard;
            bitboard = unchecked(value & (value - 1));
            return FindFirstBitSetIndexInternal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong IsolateFirstBitSetInternal(ulong value)
        {
            return unchecked(value & (ulong)(-(long)value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong PopFirstBitSetInternal(ref ulong bitboard)
        {
            var value = bitboard;
            bitboard = unchecked(value & (value - 1));
            return IsolateFirstBitSetInternal(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FindFirstBitSetIndexInternal(ulong value)
        {
            if (value == NoneValue)
            {
                return NoBitSetIndex;
            }

            const long Debruijn64 = 0x03F79D71B4CB0A89L;
            const int MagicShift = 58;

            var isolatedBit = IsolateFirstBitSetInternal(value);
            return Index64[unchecked(isolatedBit * Debruijn64) >> MagicShift];
        }

        #endregion
    }
}