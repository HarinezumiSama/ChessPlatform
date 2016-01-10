using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    //// TODO [vmcl] Rename Position to Square or GameBoardSquare

    public struct Position : IEquatable<Position>
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> structure
        ///     using the specified square index.
        /// </summary>
        [DebuggerNonUserCode]
        public Position(int squareIndex)
            : this(true, squareIndex)
        {
            // Nothing to do
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> structure
        ///     using the specified file and rank.
        /// </summary>
        [DebuggerNonUserCode]
        public Position(int file, int rank)
            : this(true, file, rank)
        {
            // Nothing to do
        }

        [DebuggerNonUserCode]
        internal Position(bool checkArguments, int squareIndex)
        {
            #region Argument Check

            if (checkArguments)
            {
                if ((squareIndex & ~ChessConstants.MaxSquareIndex) != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(squareIndex),
                        squareIndex,
                        $@"The value is out of the valid range ({0} .. {ChessConstants.MaxSquareIndex}).");
                }
            }

            #endregion

            SquareIndex = squareIndex;
        }

        [DebuggerNonUserCode]
        internal Position(bool checkArguments, int file, int rank)
        {
            #region Argument Check

            if (checkArguments)
            {
                if ((file & ~0x07) != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(file),
                        file,
                        $@"The value is out of the valid range {ChessConstants.FileRange}.");
                }

                if ((rank & ~0x07) != 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rank),
                        rank,
                        $@"The value is out of the valid range {ChessConstants.FileRange}.");
                }
            }

            #endregion

            SquareIndex = (rank << 3) | file;
        }

        #endregion

        #region Public Properties

        public int File
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return SquareIndex & 0x07;
            }
        }

        public int Rank
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (SquareIndex >> 3) & 0x07;
            }
        }

        public int SquareIndex
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public Bitboard Bitboard
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Bitboard.FromSquareIndex(SquareIndex);
            }
        }

        public char FileChar
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get
            {
                return (char)('a' + File);
            }
        }

        public char RankChar
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get
            {
                return (char)('1' + Rank);
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

        [DebuggerNonUserCode]
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

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Position? operator +(Position left, SquareShift right)
        {
            var file = left.File + right.FileOffset;
            var rank = left.Rank + right.RankOffset;

            return (file & ~0x07) == 0 && (rank & ~0x07) == 0 ? new Position(false, file, rank) : default(Position?);
        }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(Position left, Position right)
        {
            return left.SquareIndex == right.SquareIndex;
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFileIndex(char file)
        {
            return checked(file - 'a');
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRankIndex(char rank)
        {
            return checked(rank - '1');
        }

        [DebuggerNonUserCode]
        public static Position FromAlgebraic(string algebraicNotation)
        {
            var position = TryFromAlgebraic(algebraicNotation);
            if (!position.HasValue)
            {
                throw new ArgumentException(
                    $@"Invalid algebraic notation '{algebraicNotation}'.",
                    nameof(algebraicNotation));
            }

            return position.Value;
        }

        [DebuggerNonUserCode]
        public static Position? TryFromAlgebraic(string algebraicNotation)
        {
            if (algebraicNotation?.Length != 2)
            {
                return null;
            }

            var file = GetFileIndex(char.ToLowerInvariant(algebraicNotation[0]));
            var rank = GetRankIndex(algebraicNotation[1]);

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
                    nameof(file),
                    file,
                    $@"The value is out of the valid range {ChessConstants.FileRange}.");
            }

            #endregion

            return Enumerable
                .Range(0, ChessConstants.RankCount)
                .Select(rank => new Position(false, file, (byte)rank))
                .ToArray();
        }

        public static Position[] GenerateFile(char file)
        {
            var fileIndex = GetFileIndex(file);
            return GenerateFile(Convert.ToByte(fileIndex));
        }

        public static Position[] GenerateRank(int rank)
        {
            #region Argument Check

            if (!ChessConstants.RankRange.Contains(rank))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rank),
                    rank,
                    $@"The value is out of the valid range {ChessConstants.FileRange}.");
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
                throw new ArgumentNullException(nameof(ranks));
            }

            #endregion

            return ranks.SelectMany(GenerateRank).ToArray();
        }

        public override string ToString()
        {
            return new string(new[] { FileChar, RankChar });
        }

        public override bool Equals(object obj)
        {
            return obj is Position && Equals((Position)obj);
        }

        public override int GetHashCode()
        {
            return SquareIndex;
        }

        #endregion

        #region IEquatable<Position> Members

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Position other)
        {
            return Equals(this, other);
        }

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValidX88Value(int x88Value)
        {
            return (x88Value & 0xFFFFFF88) == 0;
        }

        #endregion
    }
}