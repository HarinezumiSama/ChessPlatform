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

        private readonly int _file;
        private readonly int _rank;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        public Position(int file, int rank)
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

            _file = file;
            _rank = rank;
        }

        #endregion

        #region Public Properties

        public int File
        {
            [DebuggerStepThrough]
            get
            {
                return _file;
            }
        }

        public int Rank
        {
            [DebuggerStepThrough]
            get
            {
                return _rank;
            }
        }

        #endregion

        #region Operators

        public static implicit operator Position(string algebraicNotation)
        {
            return FromAlgebraic(algebraicNotation);
        }

        public static bool operator ==(Position left, Position right)
        {
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        #endregion

        #region Public Methods

        public static Position FromAlgebraic(string algebraicNotation)
        {
            #region Argument Check

            if (algebraicNotation == null)
            {
                throw new ArgumentNullException("algebraicNotation");
            }

            if (algebraicNotation.Length != 2)
            {
                throw new ArgumentException("Invalid algebraic notation length.", "algebraicNotation");
            }

            #endregion

            var file = algebraicNotation[0] - 'a';
            var rank = algebraicNotation[1] - '1';
            return new Position(file, rank);
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

            return Enumerable.Range(0, ChessConstants.RankCount).Select(rank => new Position(file, rank)).ToArray();
        }

        public static Position[] GenerateFile(char file)
        {
            var fileValue = file - 'a';
            return GenerateFile(fileValue);
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

            return Enumerable.Range(0, ChessConstants.FileCount).Select(file => new Position(file, rank)).ToArray();
        }

        public override string ToString()
        {
            //// TODO [vmcl] Cache, if needed
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", (char)('a' + this.File), this.Rank + 1);
        }

        public override bool Equals(object obj)
        {
            return obj is Position && Equals((Position)obj);
        }

        public override int GetHashCode()
        {
            return _file.CombineHashCodes(_rank);
        }

        #endregion

        #region IEquatable<Position> Members

        public bool Equals(Position other)
        {
            return other._file == _file && other._rank == _rank;
        }

        #endregion
    }
}