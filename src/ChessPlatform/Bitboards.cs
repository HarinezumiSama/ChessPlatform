using System;
using System.Linq;

namespace ChessPlatform
{
    public static class Bitboards
    {
        #region Constants and Fields

        public const long Everything = ~0L;
        public const long None = 0L;

        public static readonly long Rank1 = Position.GenerateRank(0).ToBitboard();
        public static readonly long Rank2 = Position.GenerateRank(1).ToBitboard();
        public static readonly long Rank3 = Position.GenerateRank(2).ToBitboard();
        public static readonly long Rank6 = Position.GenerateRank(5).ToBitboard();
        public static readonly long Rank7 = Position.GenerateRank(6).ToBitboard();
        public static readonly long Rank8 = Position.GenerateRank(7).ToBitboard();

        public static readonly long FileA = Position.GenerateFile('a').ToBitboard();
        public static readonly long FileH = Position.GenerateFile('h').ToBitboard();

        public static readonly long Rank1WithFileA = Rank1 | FileA;
        public static readonly long Rank1WithFileH = Rank1 | FileH;

        public static readonly long Rank8WithFileA = Rank8 | FileA;
        public static readonly long Rank8WithFileH = Rank8 | FileH;

        internal static readonly ulong Rank1Value = (ulong)Rank1;
        internal static readonly ulong Rank8Value = (ulong)Rank8;

        internal static readonly ulong Rank1WithFileAValue = (ulong)Rank1WithFileA;
        internal static readonly ulong Rank1WithFileHValue = (ulong)Rank1WithFileH;

        internal static readonly ulong Rank8WithFileAValue = (ulong)Rank8WithFileA;
        internal static readonly ulong Rank8WithFileHValue = (ulong)Rank8WithFileH;

        internal static readonly ulong FileAValue = (ulong)FileA;
        internal static readonly ulong FileHValue = (ulong)FileH;

        #endregion
    }
}