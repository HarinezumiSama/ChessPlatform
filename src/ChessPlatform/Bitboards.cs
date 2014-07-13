﻿using System;
using System.Linq;

namespace ChessPlatform
{
    public static class Bitboards
    {
        #region Constants and Fields

        public static readonly Bitboard Rank1 = new Bitboard(Position.GenerateRank(0));
        public static readonly Bitboard Rank2 = new Bitboard(Position.GenerateRank(1));
        public static readonly Bitboard Rank3 = new Bitboard(Position.GenerateRank(2));
        public static readonly Bitboard Rank6 = new Bitboard(Position.GenerateRank(5));
        public static readonly Bitboard Rank7 = new Bitboard(Position.GenerateRank(6));
        public static readonly Bitboard Rank8 = new Bitboard(Position.GenerateRank(7));

        public static readonly Bitboard FileA = new Bitboard(Position.GenerateFile('a'));
        public static readonly Bitboard FileH = new Bitboard(Position.GenerateFile('h'));

        public static readonly Bitboard Rank1WithFileA = Rank1 | FileA;
        public static readonly Bitboard Rank1WithFileH = Rank1 | FileH;

        public static readonly Bitboard Rank8WithFileA = Rank8 | FileA;
        public static readonly Bitboard Rank8WithFileH = Rank8 | FileH;

        internal static readonly ulong Rank1Value = Rank1.InternalValue;
        internal static readonly ulong Rank8Value = Rank8.InternalValue;

        internal static readonly ulong Rank1WithFileAValue = Rank1WithFileA.InternalValue;
        internal static readonly ulong Rank1WithFileHValue = Rank1WithFileH.InternalValue;

        internal static readonly ulong Rank8WithFileAValue = Rank8WithFileA.InternalValue;
        internal static readonly ulong Rank8WithFileHValue = Rank8WithFileH.InternalValue;

        internal static readonly ulong FileAValue = FileA.InternalValue;
        internal static readonly ulong FileHValue = FileH.InternalValue;

        #endregion
    }
}