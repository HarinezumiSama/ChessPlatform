﻿using System;
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

        internal const byte X88Length = FileCount * RankCount * 2;

        internal const int MaxPieceCountPerColor = 16;
        internal const int MaxPawnCountPerColor = 8;

        public static readonly ValueRange<int> FileRange = ValueRange.Create(0, FileCount - 1);
        public static readonly ValueRange<int> RankRange = ValueRange.Create(0, RankCount - 1);

        public static readonly ReadOnlySet<PieceType> ValidPromotions =
            new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight }.ToHashSet().AsReadOnly();

        public static readonly ReadOnlyCollection<Piece> BothKings =
            new[] { Piece.WhiteKing, Piece.BlackKing }.AsReadOnly();

        public static readonly ReadOnlyCollection<PieceColor> PieceColors =
            new[] { PieceColor.White, PieceColor.Black }.AsReadOnly();

        public static readonly Position WhiteKingInitialPosition = "e1";
        public static readonly Position BlackKingInitialPosition = "e8";

        public static readonly ReadOnlyCollection<CastlingInfo> AllCastlingInfos =
            new ReadOnlyCollection<CastlingInfo>(
                new[]
                {
                    new CastlingInfo(
                        CastlingOptions.WhiteKingSide,
                        new PieceMove(WhiteKingInitialPosition, "g1"),
                        new PieceMove("h1", "f1"),
                        "f1",
                        "g1"),
                    new CastlingInfo(
                        CastlingOptions.WhiteQueenSide,
                        new PieceMove(WhiteKingInitialPosition, "c1"),
                        new PieceMove("a1", "d1"),
                        "b1",
                        "c1",
                        "d1"),
                    new CastlingInfo(
                        CastlingOptions.BlackKingSide,
                        new PieceMove(BlackKingInitialPosition, "g8"),
                        new PieceMove("h8", "f8"),
                        "f8",
                        "g8"),
                    new CastlingInfo(
                        CastlingOptions.BlackQueenSide,
                        new PieceMove(BlackKingInitialPosition, "c8"),
                        new PieceMove("a8", "d8"),
                        "b8",
                        "c8",
                        "d8")
                });

        public static readonly ReadOnlyDictionary<PieceColor, EnPassantInfo> ColorToEnPassantInfoMap =
            new ReadOnlyDictionary<PieceColor, EnPassantInfo>(
                new Dictionary<PieceColor, EnPassantInfo>
                {
                    { PieceColor.White, new EnPassantInfo(true) },
                    { PieceColor.Black, new EnPassantInfo(false) }
                });

        #endregion
    }
}