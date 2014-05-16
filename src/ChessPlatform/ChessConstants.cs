using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ChessPlatform.Internal;
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

        internal const int FenSnippetCount = 6;
        internal const string NoneCastlingOptionsFenSnippet = "-";
        internal const string NoEnPassantCaptureFenSnippet = "-";
        internal const char FenRankSeparator = '/';
        internal const string FenSnippetSeparator = " ";

        public static readonly ValueRange<int> FileRange = ValueRange.Create(0, FileCount - 1);
        public static readonly ValueRange<int> RankRange = ValueRange.Create(0, RankCount - 1);

        public static readonly ReadOnlySet<PieceType> ValidPromotions =
            new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight }.ToHashSet().AsReadOnly();

        public static readonly ReadOnlyCollection<Piece> BothKings =
            new[] { Piece.WhiteKing, Piece.BlackKing }.AsReadOnly();

        public static readonly ReadOnlyCollection<PieceColor> PieceColors =
            new[] { PieceColor.White, PieceColor.Black }.AsReadOnly();

        public static readonly ReadOnlySet<PieceType> PieceTypes =
            EnumHelper.GetAllValues<PieceType>().ToHashSet().AsReadOnly();

        public static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<Piece>> ColorToPiecesMap =
            PieceColors
                .ToDictionary(
                    Factotum.Identity,
                    color =>
                        PieceTypes
                            .Where(item => item != PieceType.None)
                            .Select(item => item.ToPiece(color))
                            .ToHashSet()
                            .AsReadOnly())
                .AsReadOnly();

        public static readonly ReadOnlySet<Piece> Pieces =
            PieceColors
                .SelectMany(color => PieceTypes.Select(item => item.ToPiece(color)))
                .ToHashSet()
                .AsReadOnly();

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
            PieceColors.ToDictionary(Factotum.Identity, item => new EnPassantInfo(item)).AsReadOnly();

        public static readonly ReadOnlyDictionary<PieceColor, string> ColorToFenSnippetMap =
            PieceColors
                .ToDictionary(
                    Factotum.Identity,
                    item => FenCharAttribute.Get(item).ToString(CultureInfo.InvariantCulture))
                .AsReadOnly();

        public static readonly ReadOnlyDictionary<string, PieceColor> FenSnippetToColorMap =
            PieceColors
                .ToDictionary(
                    item => FenCharAttribute.Get(item).ToString(CultureInfo.InvariantCulture),
                    Factotum.Identity)
                .AsReadOnly();

        public static readonly ReadOnlyCollection<CastlingOptions> FenRelatedCastlingOptions =
            new ReadOnlyCollection<CastlingOptions>(
                new[]
                {
                    CastlingOptions.WhiteKingSide,
                    CastlingOptions.WhiteQueenSide,
                    CastlingOptions.BlackKingSide,
                    CastlingOptions.BlackQueenSide
                });

        public static readonly ReadOnlyDictionary<CastlingOptions, char> CastlingOptionToFenCharMap =
            FenRelatedCastlingOptions
                .ToDictionary(Factotum.Identity, item => FenCharAttribute.Get(item))
                .AsReadOnly();

        public static readonly ReadOnlyDictionary<char, CastlingOptions> FenCharCastlingOptionMap =
            FenRelatedCastlingOptions
                .ToDictionary(item => FenCharAttribute.Get(item), Factotum.Identity)
                .AsReadOnly();

        public static readonly ReadOnlyDictionary<PieceType, char> PieceTypeToFenCharMap =
            typeof(PieceType)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(item => new { Item = item, FenChar = FenCharAttribute.TryGet(item) })
                .Where(obj => obj.FenChar.HasValue)
                .ToDictionary(
                    obj => (PieceType)obj.Item.GetValue(null),
                    obj => obj.FenChar.Value)
                .AsReadOnly();

        public static readonly ReadOnlyDictionary<Piece, char> PieceToFenCharMap =
            typeof(Piece)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(item => new { Item = item, FenChar = FenCharAttribute.TryGet(item) })
                .Where(obj => obj.FenChar.HasValue)
                .ToDictionary(
                    obj => (Piece)obj.Item.GetValue(null),
                    obj => obj.FenChar.Value)
                .AsReadOnly();

        public static readonly ReadOnlyDictionary<char, Piece> FenCharToPieceMap =
            PieceToFenCharMap.ToDictionary(pair => pair.Value, pair => pair.Key).AsReadOnly();

        #endregion
    }
}