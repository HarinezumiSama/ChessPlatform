using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public struct Position : IEquatable<Position>
    {
        #region Constants and Fields

        internal const int MaxBitboardBitIndex = sizeof(long) * 8 - 1;

        private readonly byte _x88Value;

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
        internal Position(byte x88Value)
        {
            #region Argument Check

            if (!IsValidX88Value(x88Value))
            {
                throw new ArgumentException("Out of the board 0x88 value.", "x88Value");
            }

            #endregion

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
            }

            #endregion

            _x88Value = (byte)((rank << 4) | file);
        }

        #endregion

        #region Public Properties

        public byte File
        {
            [DebuggerStepThrough]
            get
            {
                return (byte)(_x88Value & 0x07);
            }
        }

        public byte Rank
        {
            [DebuggerStepThrough]
            get
            {
                return (byte)(_x88Value >> 4);
            }
        }

        public int SquareIndex
        {
            [DebuggerNonUserCode]
            get
            {
                return this.File | (this.Rank << 3);
            }
        }

        #endregion

        #region Internal Properties

        internal byte X88Value
        {
            [DebuggerNonUserCode]
            get
            {
                return _x88Value;
            }
        }

        internal Bitboard Bitboard
        {
            [DebuggerStepThrough]
            get
            {
                return new Bitboard(1L << this.SquareIndex);
            }
        }

        #endregion

        #region Operators

        [DebuggerNonUserCode]
        public static implicit operator Position(string algebraicNotation)
        {
            return FromAlgebraic(algebraicNotation);
        }

        public static bool operator ==(Position left, Position right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        #endregion

        #region Public Methods

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

        public static Position[] GenerateFile(byte file)
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

        public static Position[] GenerateRank(byte rank)
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

        public override string ToString()
        {
            //// TODO [vmcl] Use global cache
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

        internal static bool IsValidX88Value(byte x88Value)
        {
            return (x88Value & 0x88) == 0;
        }

        internal static Position FromBitboardBitIndex(int index)
        {
            #region Argument Check

            if (index < 0 || index > MaxBitboardBitIndex)
            {
                throw new ArgumentOutOfRangeException(
                    "index",
                    index,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range ({0} .. {1}).",
                        0,
                        MaxBitboardBitIndex));
            }

            #endregion

            var x88Value = (byte)(((index & 0x38) << 1) | (index & 7));
            return new Position(x88Value);
        }

        #endregion

        #region IEquatable<Position> Members

        public bool Equals(Position other)
        {
            return Equals(other, this);
        }

        #endregion
    }
}