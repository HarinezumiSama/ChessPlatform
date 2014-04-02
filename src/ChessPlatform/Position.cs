using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public sealed class Position
    {
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

            this.File = file;
            this.Rank = rank;
        }

        #endregion

        #region Public Properties

        public int File
        {
            get;
            private set;
        }

        public int Rank
        {
            get;
            private set;
        }

        #endregion

        #region Operators

        public static implicit operator Position(string algebraicNotation)
        {
            return FromAlgebraic(algebraicNotation);
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

        #endregion
    }
}