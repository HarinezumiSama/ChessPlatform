using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChessPlatform
{
    public static class Bitboards
    {
        public static readonly Bitboard Rank1 = new Bitboard(Square.GenerateRank(0));
        public static readonly Bitboard Rank2 = new Bitboard(Square.GenerateRank(1));
        public static readonly Bitboard Rank3 = new Bitboard(Square.GenerateRank(2));
        public static readonly Bitboard Rank4 = new Bitboard(Square.GenerateRank(3));
        public static readonly Bitboard Rank5 = new Bitboard(Square.GenerateRank(4));
        public static readonly Bitboard Rank6 = new Bitboard(Square.GenerateRank(5));
        public static readonly Bitboard Rank7 = new Bitboard(Square.GenerateRank(6));
        public static readonly Bitboard Rank8 = new Bitboard(Square.GenerateRank(7));

        public static readonly Bitboard FileA = new Bitboard(Square.GenerateFile('a'));
        public static readonly Bitboard FileB = new Bitboard(Square.GenerateFile('b'));
        public static readonly Bitboard FileC = new Bitboard(Square.GenerateFile('c'));
        public static readonly Bitboard FileD = new Bitboard(Square.GenerateFile('d'));
        public static readonly Bitboard FileE = new Bitboard(Square.GenerateFile('e'));
        public static readonly Bitboard FileF = new Bitboard(Square.GenerateFile('f'));
        public static readonly Bitboard FileG = new Bitboard(Square.GenerateFile('g'));
        public static readonly Bitboard FileH = new Bitboard(Square.GenerateFile('h'));

        public static readonly Bitboard Rank1WithFileA = Rank1 | FileA;
        public static readonly Bitboard Rank1WithFileH = Rank1 | FileH;

        public static readonly Bitboard Rank8WithFileA = Rank8 | FileA;
        public static readonly Bitboard Rank8WithFileH = Rank8 | FileH;

        public static readonly ReadOnlyCollection<Bitboard> Files =
            Enumerable
                .Range(0, ChessConstants.FileCount)
                .Select(index => new Bitboard(Square.GenerateFile(index)))
                .ToArray()
                .AsReadOnly();

        public static readonly ReadOnlyCollection<Bitboard> Ranks =
            Enumerable
                .Range(0, ChessConstants.RankCount)
                .Select(index => new Bitboard(Square.GenerateRank(index)))
                .ToArray()
                .AsReadOnly();

        public static readonly Bitboard LightSquares = Enumerable
            .Range(0, ChessConstants.SquareCount)
            .Select(squareIndex => new Square(squareIndex))
            .Where(square => !square.IsDark())
            .Aggregate(Bitboard.None, (accumulator, square) => accumulator | square.Bitboard);

        public static readonly Bitboard DarkSquares = Enumerable
            .Range(0, ChessConstants.SquareCount)
            .Select(squareIndex => new Square(squareIndex))
            .Where(square => square.IsDark())
            .Aggregate(Bitboard.None, (accumulator, square) => accumulator | square.Bitboard);

        internal static readonly ulong Rank1Value = Rank1.InternalValue;
        internal static readonly ulong Rank8Value = Rank8.InternalValue;

        internal static readonly ulong Rank1WithFileAValue = Rank1WithFileA.InternalValue;
        internal static readonly ulong Rank1WithFileHValue = Rank1WithFileH.InternalValue;

        internal static readonly ulong Rank8WithFileAValue = Rank8WithFileA.InternalValue;
        internal static readonly ulong Rank8WithFileHValue = Rank8WithFileH.InternalValue;

        internal static readonly ulong FileAValue = FileA.InternalValue;
        internal static readonly ulong FileHValue = FileH.InternalValue;
    }
}