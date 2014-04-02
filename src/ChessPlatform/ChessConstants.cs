using System;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public static class ChessConstants
    {
        #region Constants and Fields

        public const int FileCount = 8;
        public const int RankCount = 8;

        internal const int X88Length = FileCount * RankCount * 2;

        public static readonly ValueRange<int> FileRange = ValueRange.Create(0, FileCount - 1);
        public static readonly ValueRange<int> RankRange = ValueRange.Create(0, RankCount - 1);

        #endregion
    }
}