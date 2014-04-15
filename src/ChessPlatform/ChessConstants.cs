using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public static class ChessConstants
    {
        #region Constants and Fields

        public const byte FileCount = 8;
        public const byte RankCount = 8;

        public const byte WhitePawnPromotionRank = RankCount - 1;
        public const byte BlackPawnPromotionRank = 0;

        public const byte WhiteEnPassantStartRank = 1;
        public const byte WhiteEnPassantTargetRank = WhiteEnPassantStartRank + 1;
        public const byte WhiteEnPassantEndRank = WhiteEnPassantTargetRank + 1;

        public const byte BlackEnPassantStartRank = RankCount - 2;
        public const byte BlackEnPassantTargetRank = BlackEnPassantStartRank - 1;
        public const byte BlackEnPassantEndRank = BlackEnPassantTargetRank - 1;

        internal const byte X88Length = FileCount * RankCount * 2;

        internal const int MaxPieceCountPerColor = 16;
        internal const int MaxPawnCountPerColor = 8;

        public static readonly ValueRange<int> FileRange = ValueRange.Create(0, FileCount - 1);
        public static readonly ValueRange<int> RankRange = ValueRange.Create(0, RankCount - 1);

        public static readonly ReadOnlySet<PieceType> ValidPromotions =
            new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight }.ToHashSet().AsReadOnly();

        public static readonly ReadOnlyCollection<Piece> BothKings =
            new[] { Piece.WhiteKing, Piece.BlackKing }.AsReadOnly();

        internal static readonly ReadOnlyCollection<PieceColor> PieceColors =
            new[] { PieceColor.White, PieceColor.Black }.AsReadOnly();

        #endregion
    }
}