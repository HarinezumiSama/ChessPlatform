using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public struct Bitboard : IEquatable<Bitboard>
    {
        #region Constants and Fields

        public const int NoBitSetIndex = -1;

        private const ulong NoneValue = 0UL;

        public static readonly Bitboard None = new Bitboard(NoneValue);

        public static readonly Bitboard Everything = new Bitboard(~NoneValue);

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

        private readonly ulong _value;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> structure
        ///     using the specified value.
        /// </summary>
        public Bitboard(long value)
        {
            _value = (ulong)value;
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
                throw new ArgumentNullException("positions");
            }

            #endregion

            _value = positions.Aggregate(NoneValue, (accumulator, position) => accumulator | position.Bitboard._value);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> structure
        ///     using the specified value.
        /// </summary>
        private Bitboard(ulong value)
        {
            _value = value;
        }

        #endregion

        #region Public Properties

        public long Value
        {
            [DebuggerStepThrough]
            get
            {
                return (long)_value;
            }
        }

        public bool IsNone
        {
            get
            {
                return _value == NoneValue;
            }
        }

        public bool IsAny
        {
            get
            {
                return _value != NoneValue;
            }
        }

        #endregion

        #region Operators

        public static bool operator ==(Bitboard left, Bitboard right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Bitboard left, Bitboard right)
        {
            return !(left == right);
        }

        public static explicit operator Bitboard(long value)
        {
            return new Bitboard(value);
        }

        public static Bitboard operator ~(Bitboard obj)
        {
            return new Bitboard(~obj._value);
        }

        public static Bitboard operator &(Bitboard left, Bitboard right)
        {
            return new Bitboard(left._value & right._value);
        }

        public static Bitboard operator |(Bitboard left, Bitboard right)
        {
            return new Bitboard(left._value | right._value);
        }

        public static Bitboard operator ^(Bitboard left, Bitboard right)
        {
            return new Bitboard(left._value ^ right._value);
        }

        #endregion

        #region Public Methods

        public static bool Equals(Bitboard left, Bitboard right)
        {
            return left._value == right._value;
        }

        public static Bitboard FromSquareIndex(int squareIndex)
        {
            return new Bitboard(1L << squareIndex);
        }

        public static int PopFirstBitSet(ref Bitboard bitboard)
        {
            var value = bitboard._value;
            bitboard = new Bitboard(unchecked(value & (value - 1)));
            return FindFirstBitSetIndexInternal(value);
        }

        public override bool Equals(object obj)
        {
            return obj is Bitboard && Equals((Bitboard)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            var squares = _value == NoneValue
                ? "<none>"
                : GetPositions().Select(item => item.ToString()).OrderBy(Factotum.Identity).Join(", ");

            return string.Format(CultureInfo.InvariantCulture, "{{ {0:X16} : {1} }}", _value, squares);
        }

        public int FindFirstBitSetIndex()
        {
            return FindFirstBitSetIndexInternal(_value);
        }

        public bool IsExactlyOneBitSet()
        {
            return _value != NoneValue && IsolateFirstBitSet(_value) == _value;
        }

        public Bitboard IsolateFirstBitSet()
        {
            return new Bitboard(IsolateFirstBitSet(_value));
        }

        public Bitboard Shift(ShiftDirection direction)
        {
            switch (direction)
            {
                case ShiftDirection.North:
                    return new Bitboard((_value & ~Bitboards.Rank8._value) << 8);

                case ShiftDirection.NorthEast:
                    return new Bitboard((_value & ~Bitboards.Rank8WithFileH._value) << 9);

                case ShiftDirection.East:
                    return new Bitboard((_value & ~Bitboards.FileH._value) << 1);

                case ShiftDirection.SouthEast:
                    return new Bitboard((_value & ~Bitboards.Rank1WithFileH._value) >> 7);

                case ShiftDirection.South:
                    return new Bitboard((_value & ~Bitboards.Rank1._value) >> 8);

                case ShiftDirection.SouthWest:
                    return new Bitboard((_value & ~Bitboards.Rank1WithFileA._value) >> 9);

                case ShiftDirection.West:
                    return new Bitboard((_value & ~Bitboards.FileA._value) >> 1);

                case ShiftDirection.NorthWest:
                    return new Bitboard((_value & ~Bitboards.Rank8WithFileA._value) << 7);

                default:
                    return None;
            }
        }

        public Position[] GetPositions()
        {
            var resultList = new List<Position>(ChessConstants.SquareCount);

            var currentValue = _value;

            int index;
            while ((index = FindFirstBitSetIndexInternal(currentValue)) >= 0)
            {
                resultList.Add(Position.FromSquareIndex(index));
                currentValue &= ~(1UL << index);
            }

            return resultList.ToArray();
        }

        public Position GetFirstPosition()
        {
            var squareIndex = FindFirstBitSetIndex();
            return Position.FromSquareIndex(squareIndex);
        }

        public int GetCount()
        {
            var result = 0;

            var currentValue = _value;

            int index;
            while ((index = FindFirstBitSetIndexInternal(currentValue)) >= 0)
            {
                result++;
                currentValue &= ~(1UL << index);
            }

            return result;
        }

        #endregion

        #region IEquatable<Bitboard> Members

        public bool Equals(Bitboard other)
        {
            return Equals(this, other);
        }

        #endregion

        #region Private Methods

        private static ulong IsolateFirstBitSet(ulong value)
        {
            return unchecked(value & (ulong)(-(long)value));
        }

        private static int FindFirstBitSetIndexInternal(ulong value)
        {
            if (value == NoneValue)
            {
                return NoBitSetIndex;
            }

            const long Debruijn64 = 0x03F79D71B4CB0A89L;
            const int MagicShift = 58;

            var isolatedBit = IsolateFirstBitSet(value);

            var result = Index64[isolatedBit * Debruijn64 >> MagicShift];
            return result;
        }

        #endregion
    }
}