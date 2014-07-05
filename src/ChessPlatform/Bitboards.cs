using System;
using System.Linq;

namespace ChessPlatform
{
    public static class Bitboards
    {
        #region Constants and Fields

        public static readonly Bitboard Rank1 = new Bitboard(Position.GenerateRank(0));
        public static readonly Bitboard Rank8 = new Bitboard(Position.GenerateRank(7));

        public static readonly Bitboard FileA = new Bitboard(Position.GenerateFile('a'));
        public static readonly Bitboard FileH = new Bitboard(Position.GenerateFile('h'));

        public static readonly Bitboard Rank1WithFileA = Rank1 | FileA;
        public static readonly Bitboard Rank1WithFileH = Rank1 | FileH;

        public static readonly Bitboard Rank8WithFileA = Rank8 | FileA;
        public static readonly Bitboard Rank8WithFileH = Rank8 | FileH;

        #endregion
    }
}