using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public struct Position : IEquatable<Position>
    {
        #region Constants and Fields

        private readonly int _x88Value;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class
        ///     using the specified file and rank.
        /// </summary>
        [DebuggerNonUserCode]
        public Position(int file, int rank)
            : this(true, file, rank)
        {
            // Nothing to do
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class
        ///     using the specified 0x88 board representation value.
        /// </summary>
        [DebuggerNonUserCode]
        internal Position(int x88Value)
        {
            _x88Value = x88Value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        [DebuggerNonUserCode]
        internal Position(bool checkArguments, int file, int rank)
        {
            #region Argument Check

            if (checkArguments)
            {
                if ((file & ~0x07) != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "file",
                        file,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"The value is out of the valid range {0}.",
                            ChessConstants.FileRange));
                }

                if ((rank & ~0x07) != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "rank",
                        rank,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"The value is out of the valid range {0}.",
                            ChessConstants.FileRange));
                }
            }

            #endregion

            _x88Value = (byte)((rank << 4) | file);
        }

        #endregion

        #region Public Properties

        public int File
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _x88Value & 0x07;
            }
        }

        public int Rank
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _x88Value >> 4;
            }
        }

        public int SquareIndex
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (this.Rank << 3) | this.File;
            }
        }

        public Bitboard Bitboard
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Bitboard.FromSquareIndex(this.SquareIndex);
            }
        }

        #endregion

        #region Internal Properties

        internal int X88Value
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _x88Value;
            }
        }

        #endregion

        #region Operators

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Position(string algebraicNotation)
        {
            return FromAlgebraic(algebraicNotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Position left, Position right)
        {
            return Equals(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(Position left, Position right)
        {
            return left._x88Value == right._x88Value;
        }

        [DebuggerNonUserCode]
        public static Position FromAlgebraic(string algebraicNotation)
        {
            var position = TryFromAlgebraic(algebraicNotation);
            if (!position.HasValue)
            {
                throw new ArgumentException("Invalid algebraic notation.", "algebraicNotation");
            }

            return position.Value;
        }

        [DebuggerNonUserCode]
        public static Position? TryFromAlgebraic(string algebraicNotation)
        {
            if (algebraicNotation == null)
            {
                return null;
            }

            if (algebraicNotation.Length != 2)
            {
                return null;
            }

            var file = char.ToLowerInvariant(algebraicNotation[0]) - 'a';
            var rank = algebraicNotation[1] - '1';

            return ChessConstants.FileRange.Contains(file) && ChessConstants.RankRange.Contains(rank)
                ? new Position(false, file, rank)
                : null;
        }

        public static Position[] GenerateFile(int file)
        {
            #region Argument Check

            if (!ChessConstants.FileRange.Contains(file))
            {
                throw new ArgumentOutOfRangeException(
                    "file",
                    file,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range {0}.",
                        ChessConstants.FileRange));
            }

            #endregion

            return Enumerable
                .Range(0, ChessConstants.RankCount)
                .Select(rank => new Position(false, file, (byte)rank))
                .ToArray();
        }

        public static Position[] GenerateFile(char file)
        {
            var fileValue = file - 'a';
            return GenerateFile(Convert.ToByte(fileValue));
        }

        public static Position[] GenerateRank(int rank)
        {
            #region Argument Check

            if (!ChessConstants.RankRange.Contains(rank))
            {
                throw new ArgumentOutOfRangeException(
                    "rank",
                    rank,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range {0}.",
                        ChessConstants.FileRange));
            }

            #endregion

            return Enumerable
                .Range(0, ChessConstants.FileCount)
                .Select(file => new Position(false, (byte)file, rank))
                .ToArray();
        }

        public static Position[] GenerateRanks(params int[] ranks)
        {
            #region Argument Check

            if (ranks == null)
            {
                throw new ArgumentNullException("ranks");
            }

            #endregion

            return ranks.SelectMany(GenerateRank).ToArray();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", (char)('a' + this.File), this.Rank + 1);
        }

        public override bool Equals(object obj)
        {
            return obj is Position && Equals((Position)obj);
        }

        public override int GetHashCode()
        {
            return _x88Value;
        }

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValidX88Value(int x88Value)
        {
            return (x88Value & 0xFFFFFF88) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Position FromSquareIndex(int squareIndex)
        {
            #region Argument Check

            if ((squareIndex & ~0x3F) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    "squareIndex",
                    squareIndex,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range ({0} .. {1}).",
                        0,
                        ChessConstants.SquareCount - 1));
            }

            #endregion

            var x88Value = (byte)(((squareIndex & 0x38) << 1) | (squareIndex & 7));
            return new Position(x88Value);
        }

        #endregion

        #region IEquatable<Position> Members

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Position other)
        {
            return Equals(this, other);
        }

        #endregion
    }
}