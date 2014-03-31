using System;
using System.Globalization;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public sealed class Position
    {
        #region Constants and Fields

        public static readonly ValueRange<int> FileRange = ValueRange.Create(0, 7);
        public static readonly ValueRange<int> RankRange = ValueRange.Create(0, 7);

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Position"/> class.
        /// </summary>
        public Position(int file, int rank)
        {
            #region Argument Check

            if (!FileRange.Contains(file))
            {
                throw new ArgumentOutOfRangeException(
                    "file",
                    file,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range {0}.",
                        FileRange));
            }

            if (!RankRange.Contains(rank))
            {
                throw new ArgumentOutOfRangeException(
                    "rank",
                    rank,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"The value is out of the valid range {0}.",
                        FileRange));
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
    }
}