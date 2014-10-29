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

        public const int FileCount = 8;
        public const int RankCount = 8;

        public const int SquareCount = FileCount * RankCount;

        public const int WhitePawnPromotionRank = RankCount - 1;
        public const int BlackPawnPromotionRank = 0;

        public const int FullMoveCountBy50MoveRule = 50;

        public const string DefaultInitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static readonly ValueRange<int> FileRange = ValueRange.Create(0, FileCount - 1);
        public static readonly ValueRange<int> RankRange = ValueRange.Create(0, RankCount - 1);

        public static readonly ReadOnlySet<PieceType> ValidPromotions =
            GetValidPromotions().ToHashSet().AsReadOnly();

        public static readonly ReadOnlyCollection<Piece> BothKings =
            new[] { Piece.WhiteKing, Piece.BlackKing }.AsReadOnly();

        public static readonly ReadOnlyCollection<PieceColor> PieceColors =
            new[] { PieceColor.White, PieceColor.Black }.AsReadOnly();

        public static readonly ReadOnlySet<PieceType> PieceTypes =
            EnumFactotum.GetAllValues<PieceType>().ToHashSet().AsReadOnly();

        public static readonly ReadOnlySet<PieceType> PieceTypesExceptNone =
            PieceTypes.Where(item => item != PieceType.None).ToHashSet().AsReadOnly();

        public static readonly ReadOnlySet<PieceType> PieceTypesExceptNoneAndKing =
            PieceTypes.Where(item => item != PieceType.None && item != PieceType.King).ToHashSet().AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<Piece>> ColorToPiecesMap =
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

        public static readonly ReadOnlySet<Piece> PiecesExceptNone =
            Pieces.Where(item => item != Piece.None).ToHashSet().AsReadOnly();

        public static readonly Position WhiteKingInitialPosition = "e1";
        public static readonly Position BlackKingInitialPosition = "e8";

        public static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, DoublePushInfo> ColorToEnPassantInfoMap =
            PieceColors.ToDictionary(Factotum.Identity, item => new DoublePushInfo(item)).AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, string> ColorToFenSnippetMap =
            PieceColors
                .ToDictionary(
                    Factotum.Identity,
                    item => FenCharAttribute.Get(item).ToString(CultureInfo.InvariantCulture))
                .AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<string, PieceColor> FenSnippetToColorMap =
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

        public static readonly Omnifactotum.ReadOnlyDictionary<CastlingOptions, char> CastlingOptionToFenCharMap =
            FenRelatedCastlingOptions
                .ToDictionary(Factotum.Identity, item => FenCharAttribute.Get(item))
                .AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<char, CastlingOptions> FenCharCastlingOptionMap =
            FenRelatedCastlingOptions
                .ToDictionary(item => FenCharAttribute.Get(item), Factotum.Identity)
                .AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<PieceType, char> PieceTypeToFenCharMap =
            typeof(PieceType)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(item => new { Item = item, FenChar = FenCharAttribute.TryGet(item) })
                .Where(obj => obj.FenChar.HasValue)
                .ToDictionary(
                    obj => (PieceType)obj.Item.GetValue(null),
                    obj => obj.FenChar.Value)
                .AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<char, PieceType> FenCharToPieceTypeMap =
            PieceTypeToFenCharMap.ToDictionary(pair => pair.Value, pair => pair.Key).AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<Piece, char> PieceToFenCharMap =
            typeof(Piece)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(item => new { Item = item, FenChar = FenCharAttribute.TryGet(item) })
                .Where(obj => obj.FenChar.HasValue)
                .ToDictionary(
                    obj => (Piece)obj.Item.GetValue(null),
                    obj => obj.FenChar.Value)
                .AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<char, Piece> FenCharToPieceMap =
            PieceToFenCharMap.ToDictionary(pair => pair.Value, pair => pair.Key).AsReadOnly();

        public static readonly ReadOnlyCollection<CastlingInfo> AllCastlingInfos =
            new ReadOnlyCollection<CastlingInfo>(
                new[]
                {
                    new CastlingInfo(
                        CastlingType.WhiteKingSide,
                        new GameMove(WhiteKingInitialPosition, "g1"),
                        new GameMove("h1", "f1"),
                        "f1",
                        "g1"),
                    new CastlingInfo(
                        CastlingType.WhiteQueenSide,
                        new GameMove(WhiteKingInitialPosition, "c1"),
                        new GameMove("a1", "d1"),
                        "b1",
                        "c1",
                        "d1"),
                    new CastlingInfo(
                        CastlingType.BlackKingSide,
                        new GameMove(BlackKingInitialPosition, "g8"),
                        new GameMove("h8", "f8"),
                        "f8",
                        "g8"),
                    new CastlingInfo(
                        CastlingType.BlackQueenSide,
                        new GameMove(BlackKingInitialPosition, "c8"),
                        new GameMove("a8", "d8"),
                        "b8",
                        "c8",
                        "d8")
                });

        internal const int X88Length = FileCount * RankCount * 2;

        internal const int MaxPieceCountPerColor = 16;
        internal const int MaxPawnCountPerColor = 8;

        internal const int FenSnippetCount = 6;
        internal const string NoneCastlingOptionsFenSnippet = "-";
        internal const string NoEnPassantCaptureFenSnippet = "-";
        internal const char FenRankSeparator = '/';
        internal const string FenSnippetSeparator = " ";

        #endregion

        #region Internal Methods

        internal static PieceType[] GetValidPromotions()
        {
            return new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
        }

        #endregion
    }
}