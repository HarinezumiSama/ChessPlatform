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
        public const int MaxFileIndex = FileCount - 1;

        public const int RankCount = 8;
        public const int MaxRankIndex = RankCount - 1;

        public const int SquareCount = FileCount * RankCount;
        public const int MaxSquareIndex = SquareCount - 1;

        public const int WhitePawnPromotionRank = RankCount - 1;
        public const int BlackPawnPromotionRank = 0;

        public const int FullMoveCountBy50MoveRule = 50;

        public const string DefaultInitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public const char CaptureChar = 'x';
        public const char PromotionPrefixChar = '=';

        public static readonly string CaptureCharString = CaptureChar.ToString(CultureInfo.InvariantCulture);

        public static readonly string PromotionPrefixCharString =
            PromotionPrefixChar.ToString(CultureInfo.InvariantCulture);

        public static readonly ValueRange<int> FileRange = ValueRange.Create(0, FileCount - 1);
        public static readonly ValueRange<int> RankRange = ValueRange.Create(0, RankCount - 1);

        public static readonly ReadOnlySet<PieceType> ValidPromotions =
            OmnifactotumCollectionExtensions.ToHashSet(GetValidPromotions()).AsReadOnly();

        public static readonly ReadOnlyCollection<Piece> BothKings =
            new[] { Piece.WhiteKing, Piece.BlackKing }.AsReadOnly();

        public static readonly ReadOnlyCollection<GameSide> GameSides =
            new[] { GameSide.White, GameSide.Black }.AsReadOnly();

        public static readonly ReadOnlySet<PieceType> PieceTypes =
            OmnifactotumCollectionExtensions.ToHashSet(EnumFactotum.GetAllValues<PieceType>()).AsReadOnly();

        public static readonly ReadOnlySet<PieceType> PieceTypesExceptNone =
            OmnifactotumCollectionExtensions.ToHashSet(PieceTypes.Where(item => item != PieceType.None)).AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<GameSide, ReadOnlySet<Piece>> GameSideToPiecesMap =
            GameSides
                .ToDictionary(
                    Factotum.Identity,
                    side =>
                        OmnifactotumCollectionExtensions
                            .ToHashSet(
                                PieceTypes
                                    .Where(item => item != PieceType.None)
                                    .Select(item => item.ToPiece(side)))
                            .AsReadOnly())
                .AsReadOnly();

        public static readonly ReadOnlySet<Piece> Pieces =
            OmnifactotumCollectionExtensions
                .ToHashSet(GameSides.SelectMany(side => PieceTypes.Select(item => item.ToPiece(side))))
                .AsReadOnly();

        public static readonly ReadOnlySet<Piece> PiecesExceptNone =
            OmnifactotumCollectionExtensions.ToHashSet(Pieces.Where(item => item != Piece.None)).AsReadOnly();

        public static readonly Square WhiteKingInitialSquare = "e1";
        public static readonly Square BlackKingInitialSquare = "e8";

        public static readonly Omnifactotum.ReadOnlyDictionary<GameSide, DoublePushInfo> GameSideToDoublePushInfoMap =
            GameSides.ToDictionary(Factotum.Identity, item => new DoublePushInfo(item)).AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<GameSide, string> GameSideToFenSnippetMap =
            GameSides
                .ToDictionary(
                    Factotum.Identity,
                    item => FenCharAttribute.Get(item).ToString(CultureInfo.InvariantCulture))
                .AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<string, GameSide> FenSnippetToGameSideMap =
            GameSides
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
                        new GameMove(WhiteKingInitialSquare, "g1"),
                        new GameMove("h1", "f1"),
                        "f1",
                        "g1"),
                    new CastlingInfo(
                        CastlingType.WhiteQueenSide,
                        new GameMove(WhiteKingInitialSquare, "c1"),
                        new GameMove("a1", "d1"),
                        "b1",
                        "c1",
                        "d1"),
                    new CastlingInfo(
                        CastlingType.BlackKingSide,
                        new GameMove(BlackKingInitialSquare, "g8"),
                        new GameMove("h8", "f8"),
                        "f8",
                        "g8"),
                    new CastlingInfo(
                        CastlingType.BlackQueenSide,
                        new GameMove(BlackKingInitialSquare, "c8"),
                        new GameMove("a8", "d8"),
                        "b8",
                        "c8",
                        "d8")
                });

        public static readonly ReadOnlyCollection<CastlingInfo2> AllCastlingInfos2 =
            new ReadOnlyCollection<CastlingInfo2>(
                new[]
                {
                    new CastlingInfo2(
                        CastlingType.WhiteKingSide,
                        new GameMove2(WhiteKingInitialSquare, "g1"),
                        new GameMove2("h1", "f1"),
                        "f1",
                        "g1"),
                    new CastlingInfo2(
                        CastlingType.WhiteQueenSide,
                        new GameMove2(WhiteKingInitialSquare, "c1"),
                        new GameMove2("a1", "d1"),
                        "b1",
                        "c1",
                        "d1"),
                    new CastlingInfo2(
                        CastlingType.BlackKingSide,
                        new GameMove2(BlackKingInitialSquare, "g8"),
                        new GameMove2("h8", "f8"),
                        "f8",
                        "g8"),
                    new CastlingInfo2(
                        CastlingType.BlackQueenSide,
                        new GameMove2(BlackKingInitialSquare, "c8"),
                        new GameMove2("a8", "d8"),
                        "b8",
                        "c8",
                        "d8")
                });

        internal const int MaxPieceCountPerSide = 16;
        internal const int MaxPawnCountPerSide = 8;

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