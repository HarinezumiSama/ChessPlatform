using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class BitboardHelper
    {
        #region Constants and Fields

        public const int NoBitSetIndex = -1;

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

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long FromSquareIndex(int squareIndex)
        {
            return (long)(1UL << squareIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long IsolateFirstBitSet(long bitboard)
        {
            return (long)IsolateFirstBitSetInternal((ulong)bitboard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindFirstBitSetIndex(long bitboard)
        {
            if (bitboard == Bitboards.None)
            {
                return NoBitSetIndex;
            }

            const long Debruijn64 = 0x03F79D71B4CB0A89L;
            const int MagicShift = 58;

            var isolatedBit = IsolateFirstBitSetInternal((ulong)bitboard);
            return Index64[unchecked(isolatedBit * Debruijn64) >> MagicShift];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopFirstBitSetIndex(ref long bitboard)
        {
            var value = (ulong)bitboard;
            bitboard = (long)unchecked(value & (value - 1));
            return FindFirstBitSetIndex((long)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long PopFirstBitSet(ref long bitboard)
        {
            var value = (ulong)bitboard;
            bitboard = (long)unchecked(value & (value - 1));
            return (long)IsolateFirstBitSetInternal(value);
        }

        public static string ToBitboardString(this long bitboard)
        {
            var squares = bitboard == Bitboards.None
                ? "<none>"
                : GetPositions(bitboard).Select(item => item.ToString()).OrderBy(Factotum.Identity).Join(", ");

            return string.Format(CultureInfo.InvariantCulture, "{{ {0:X16} : {1} }}", bitboard, squares);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsExactlyOneBitSet(long bitboard)
        {
            return bitboard != Bitboards.None && IsolateFirstBitSetInternal((ulong)bitboard) == (ulong)bitboard;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Shift(this long bitboard, ShiftDirection direction)
        {
            return (long)ShiftInternal((ulong)bitboard, direction);
        }

        public static Position[] GetPositions(long bitboard)
        {
            var resultList = new List<Position>(ChessConstants.SquareCount);

            var currentValue = (ulong)bitboard;

            int index;
            while ((index = FindFirstBitSetIndex((long)currentValue)) >= 0)
            {
                resultList.Add(Position.FromSquareIndex(index));
                currentValue &= ~(1UL << index);
            }

            return resultList.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToBitboard([NotNull] this IEnumerable<Position> positions)
        {
            #region Argument Check

            if (positions == null)
            {
                throw new ArgumentNullException("positions");
            }

            #endregion

            return positions.Aggregate(Bitboards.None, (accumulator, position) => accumulator | position.Bitboard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position GetFirstPosition(long bitboard)
        {
            var squareIndex = FindFirstBitSetIndex(bitboard);
            return Position.FromSquareIndex(squareIndex);
        }

        public static int GetBitSetCount(long bitboard)
        {
            var result = 0;

            var currentValue = (ulong)bitboard;
            while (PopFirstBitSetIndexInternal(ref currentValue) >= 0)
            {
                result++;
            }

            return result;
        }

        #endregion

        #region Internal Methods

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

            return Bitboards.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int PopFirstBitSetIndexInternal(ref ulong bitboard)
        {
            var value = bitboard;
            bitboard = unchecked(value & (value - 1));
            return FindFirstBitSetIndex((long)value);
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

        #endregion
    }
}