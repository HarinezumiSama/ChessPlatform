using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public struct Bitboard : IEquatable<Bitboard>
    {
        #region Constants and Fields

        public const int NoBitSetIndex = -1;

        public static readonly Bitboard Zero = new Bitboard(0L);

        public static readonly Bitboard Everything = new Bitboard(~0L);

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

        private readonly long _value;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> structure
        ///     using the specified value.
        /// </summary>
        public Bitboard(long value)
        {
            _value = value;
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

            _value = positions.Aggregate(0L, (accumulator, position) => accumulator | position.Bitboard._value);
        }

        #endregion

        #region Public Properties

        public long Value
        {
            [DebuggerStepThrough]
            get
            {
                return _value;
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

        public static implicit operator Bitboard(long value)
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
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ {0:X16} : {1} }}",
                _value,
                _value == 0 ? "<none>" : GetPositions().Select(item => item.ToString()).Join(", "));
        }

        public bool IsZero()
        {
            return _value == 0;
        }

        public int FindFirstBitSet()
        {
            return FindFirstBitSetInternal(_value);
        }

        public bool IsExactlyOneBitSet()
        {
            return IsExactlyOneBitSetInternal(_value);
        }

        public Position[] GetPositions()
        {
            var resultList = new List<Position>(ChessConstants.SquareCount);

            var currentValue = _value;

            int index;
            while ((index = FindFirstBitSetInternal(currentValue)) >= 0)
            {
                resultList.Add(Position.FromSquareIndex(index));
                currentValue &= ~(1L << index);
            }

            return resultList.ToArray();
        }

        public int GetCount()
        {
            var result = 0;

            var currentValue = _value;

            int index;
            while ((index = FindFirstBitSetInternal(currentValue)) >= 0)
            {
                result++;
                currentValue &= ~(1L << index);
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

        private static int FindFirstBitSetInternal(long value)
        {
            if (value == 0)
            {
                return NoBitSetIndex;
            }

            const long Debruijn64 = 0x03F79D71B4CB0A89L;

            var firstBitOnly = value & -value;

            var result = Index64[((ulong)(firstBitOnly * Debruijn64)) >> 58];
            return result;
        }

        private static bool IsExactlyOneBitSetInternal(long value)
        {
            return value != 0 && ((value & -value) == value);
        }

        #endregion
    }
}