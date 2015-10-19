using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ChessPlatform.Annotations;
using ChessPlatform.Internal;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class ChessHelper
    {
        #region Constants and Fields

        public const double DefaultZeroTolerance = 1E-7d;

        public static readonly Omnifactotum.ReadOnlyDictionary<CastlingType, CastlingInfo> CastlingTypeToInfoMap =
            ChessConstants.AllCastlingInfos.ToDictionary(obj => obj.CastlingType).AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<CastlingOptions, CastlingInfo>
            CastlingOptionToInfoMap =
                ChessConstants.AllCastlingInfos.ToDictionary(obj => obj.CastlingType.ToOption()).AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<GameMove, CastlingInfo> KingMoveToCastlingInfoMap =
            ChessConstants.AllCastlingInfos.ToDictionary(obj => obj.KingMove).AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<CastlingOptions>>
            ColorToCastlingOptionSetMap =
                new Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<CastlingOptions>>(
                    new Dictionary<PieceColor, ReadOnlySet<CastlingOptions>>
                    {
                        {
                            PieceColor.White,
                            new[] { CastlingOptions.WhiteKingSide, CastlingOptions.WhiteQueenSide }
                                .ToHashSet()
                                .AsReadOnly()
                        },
                        {
                            PieceColor.Black,
                            new[] { CastlingOptions.BlackKingSide, CastlingOptions.BlackQueenSide }
                                .ToHashSet()
                                .AsReadOnly()
                        }
                    });

        public static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, CastlingOptions>
            ColorToCastlingOptionsMap =
                ColorToCastlingOptionSetMap
                    .ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value.Aggregate(CastlingOptions.None, (a, item) => a | item))
                    .AsReadOnly();

        public static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, int> ColorToPawnPromotionRankMap =
            new Omnifactotum.ReadOnlyDictionary<PieceColor, int>(
                new EnumFixedSizeDictionary<PieceColor, int>
                {
                    { PieceColor.White, ChessConstants.WhitePawnPromotionRank },
                    { PieceColor.Black, ChessConstants.BlackPawnPromotionRank }
                });

        public static readonly ReadOnlySet<Position> AllPositions =
            Enumerable
                .Range(0, ChessConstants.FileCount)
                .SelectMany(rank => Position.GenerateRank((byte)rank))
                .ToHashSet()
                .AsReadOnly();

        public static readonly PieceType DefaultPromotion = PieceType.Queen;

        internal const int MaxSlidingPieceDistance = 8;
        internal const int MaxPawnAttackOrMoveDistance = 1;
        internal const int MaxKingMoveOrAttackDistance = 1;

        internal static readonly ReadOnlyCollection<RayInfo> StraightRays =
            new ReadOnlyCollection<RayInfo>(
                new[]
                {
                    new RayInfo(0xFF, true),
                    new RayInfo(0x01, true),
                    new RayInfo(0xF0, true),
                    new RayInfo(0x10, true)
                });

        internal static readonly ReadOnlyCollection<RayInfo> DiagonalRays =
            new ReadOnlyCollection<RayInfo>(
                new[]
                {
                    new RayInfo(0x0F, false),
                    new RayInfo(0x11, false),
                    new RayInfo(0xEF, false),
                    new RayInfo(0xF1, false)
                });

        internal static readonly ReadOnlyCollection<RayInfo> AllRays =
            new ReadOnlyCollection<RayInfo>(StraightRays.Concat(DiagonalRays).ToArray());

        internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, RayInfo> PawnMoveRayMap =
            new Omnifactotum.ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x10, true) },
                    { PieceColor.Black, new RayInfo(0xF0, true) }
                });

        internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, RayInfo> PawnEnPassantMoveRayMap =
            new Omnifactotum.ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x20, true) },
                    { PieceColor.Black, new RayInfo(0xE0, true) }
                });

        internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>> PawnAttackRayMap =
            new Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>(
                new Dictionary<PieceColor, ReadOnlySet<RayInfo>>
                {
                    {
                        PieceColor.White,
                        new[] { new RayInfo(0x0F, false), new RayInfo(0x11, false) }.ToHashSet().AsReadOnly()
                    },
                    {
                        PieceColor.Black,
                        new[] { new RayInfo(0xEF, false), new RayInfo(0xF1, false) }.ToHashSet().AsReadOnly()
                    }
                });

        internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>
            PawnReverseAttackRayMap =
                new Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>(
                    new Dictionary<PieceColor, ReadOnlySet<RayInfo>>
                    {
                        {
                            PieceColor.White,
                            new[] { new RayInfo(0xEF, false), new RayInfo(0xF1, false) }.ToHashSet().AsReadOnly()
                        },
                        {
                            PieceColor.Black,
                            new[] { new RayInfo(0x0F, false), new RayInfo(0x11, false) }.ToHashSet().AsReadOnly()
                        }
                    });

        internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<byte>> PawnAttackOffsetMap =
            PawnAttackRayMap.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(item => item.Offset).ToHashSet().AsReadOnly()).AsReadOnly();

        internal static readonly ReadOnlySet<RayInfo> KingAttackRays = AllRays.ToHashSet().AsReadOnly();

        internal static readonly ReadOnlySet<byte> KingAttackOrMoveOffsets =
            KingAttackRays.Select(item => item.Offset).ToHashSet().AsReadOnly();

        internal static readonly ReadOnlySet<byte> KnightAttackOrMoveOffsets =
            new byte[] { 0x21, 0x1F, 0xE1, 0xDF, 0x12, 0x0E, 0xEE, 0xF2 }.ToHashSet().AsReadOnly();

        internal static readonly ReadOnlyCollection<PieceType> NonDefaultPromotions =
            ChessConstants.ValidPromotions.Except(DefaultPromotion.AsArray()).ToArray().AsReadOnly();

        internal static readonly ReadOnlySet<Position> AllPawnPositions =
            Enumerable
                .Range(1, ChessConstants.RankCount - 2)
                .SelectMany(rank => Position.GenerateRank(checked((byte)rank)))
                .ToHashSet()
                .AsReadOnly();

        internal static readonly Omnifactotum.ReadOnlyDictionary<PositionBridgeKey, Bitboard> PositionBridgeMap =
            GeneratePositionBridgeMap();

        internal static readonly Bitboard InvalidPawnPositionsBitboard =
            new Bitboard(Position.GenerateRanks(ChessConstants.RankRange.Lower, ChessConstants.RankRange.Upper));

        private const string FenRankRegexSnippet = @"[1-8KkQqRrBbNnPp]{1,8}";

        private static readonly Omnifactotum.ReadOnlyDictionary<Position, ReadOnlyCollection<Position>>
            KnightMovePositionMap =
                AllPositions
                    .ToDictionary(
                        Factotum.Identity,
                        position => GetKnightMovePositionsNonCached(position).AsReadOnly())
                    .AsReadOnly();

        private static readonly Assembly PlatformAssembly = typeof(ChessHelper).Assembly;
        private static readonly Version PlatformVersion = PlatformAssembly.GetName().Version;

        private static readonly string PlatformRevisionId =
            PlatformAssembly.GetSingleCustomAttribute<RevisionIdAttribute>(false).RevisionId.EnsureNotNull();

        private static readonly Regex ValidFenRegex = new Regex(
            string.Format(
                CultureInfo.InvariantCulture,
                @"^ \s* {0}/{0}/{0}/{0}/{0}/{0}/{0}/{0} \s+ (?:w|b) \s+ (?:[KkQq]+|\-) \s+ (?:[a-h][1-8]|\-) \s+ \d+ \s+ \d+ \s* $",
                FenRankRegexSnippet),
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        #endregion

        #region Public Methods

        public static string GetPlatformVersion(bool fullVersion)
        {
            var resultBuilder = new StringBuilder(PlatformVersion.ToString());

            if (fullVersion)
            {
                resultBuilder.AppendFormat(CultureInfo.InvariantCulture, " (rev. {0})", PlatformRevisionId);
            }

            return resultBuilder.ToString();
        }

        public static ReadOnlyCollection<Position> GetKnightMovePositions(Position position)
        {
            return KnightMovePositionMap[position];
        }

        public static bool IsZero(this double value, double tolerance = DefaultZeroTolerance)
        {
            return Math.Abs(value) <= DefaultZeroTolerance;
        }

        public static int ToSign(this bool value)
        {
            return value ? 1 : -1;
        }

        public static bool IsValidFenFormat(string fen)
        {
            return !fen.IsNullOrEmpty() && ValidFenRegex.IsMatch(fen);
        }

        public static string GetStandardAlgebraicNotation([NotNull] this GameBoard board, [NotNull] GameMove move)
        {
            GameBoard nextBoard;
            return GetStandardAlgebraicNotationInternal(board, move, out nextBoard);
        }

        public static string ToStandardAlgebraicNotation([NotNull] this GameMove move, [NotNull] GameBoard board)
            => GetStandardAlgebraicNotation(board, move);

        #endregion

        #region Internal Methods

        internal static Position[] GetOnboardPositions(Position position, ICollection<byte> x88Offsets)
        {
            #region Argument Check

            if (x88Offsets == null)
            {
                throw new ArgumentNullException(nameof(x88Offsets));
            }

            #endregion

            var result = new List<Position>(x88Offsets.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var x88Offset in x88Offsets)
            {
                if (x88Offset == 0)
                {
                    continue;
                }

                var x88Value = (byte)(position.X88Value + x88Offset);
                if (Position.IsValidX88Value(x88Value))
                {
                    result.Add(new Position(x88Value));
                }
            }

            return result.ToArray();
        }

        internal static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        internal static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> collection)
        {
            #region Argument Check

            if (hashSet == null)
            {
                throw new ArgumentNullException(nameof(hashSet));
            }

            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            #endregion

            collection.DoForEach(item => hashSet.Add(item));
        }

        internal static PieceType ToPieceType(this char fenChar)
        {
            PieceType result;
            if (!ChessConstants.FenCharToPieceTypeMap.TryGetValue(fenChar, out result))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Invalid FEN character ({0}).", fenChar),
                    nameof(fenChar));
            }

            return result;
        }

        internal static string GetStandardAlgebraicNotationInternal(
            [NotNull] this GameBoard board,
            [NotNull] GameMove move,
            [NotNull] out GameBoard nextBoard)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            GameMoveInfo moveInfo;
            if (!board.ValidMoves.TryGetValue(move, out moveInfo))
            {
                throw new ArgumentException($@"Invalid move {move} for the board '{board.GetFen()}'.", nameof(move));
            }

            var resultBuilder = new StringBuilder();

            if (moveInfo.IsKingCastling)
            {
                var castlingInfo = board.CheckCastlingMove(move).EnsureNotNull();
                var isKingSide = (castlingInfo.Option & CastlingOptions.KingSideMask) != 0;
                resultBuilder.Append(isKingSide ? "O-O" : "O-O-O");
            }
            else
            {
                var pieceType = board[move.From].GetPieceType();
                if (pieceType == PieceType.None)
                {
                    throw new InvalidOperationException(
                        $@"Invalid move {move} for the board '{board.GetFen()}': no piece at the source square.");
                }

                if (pieceType == PieceType.Pawn)
                {
                    if (moveInfo.IsAnyCapture)
                    {
                        resultBuilder.Append(move.From.FileChar);
                    }
                }
                else
                {
                    resultBuilder.Append(pieceType.GetFenChar());

                    var competitorPositions = board
                        .ValidMoves
                        .Keys
                        .Where(
                            obj => obj != move && obj.To == move.To && board[obj.From].GetPieceType() == pieceType)
                        .Select(obj => obj.From)
                        .ToArray();

                    if (competitorPositions.Length != 0)
                    {
                        var onSameFile = competitorPositions.Any(position => position.File == move.From.File);
                        var onSameRank = competitorPositions.Any(position => position.Rank == move.From.Rank);

                        if (onSameFile)
                        {
                            if (onSameRank)
                            {
                                resultBuilder.Append(move.From.FileChar);
                            }

                            resultBuilder.Append(move.From.RankChar);
                        }
                        else
                        {
                            resultBuilder.Append(move.From.FileChar);
                        }
                    }
                }

                if (moveInfo.IsAnyCapture)
                {
                    resultBuilder.Append(ChessConstants.CaptureChar);
                }

                resultBuilder.Append(move.To);
            }

            if (moveInfo.IsPawnPromotion)
            {
                resultBuilder.Append(ChessConstants.PromotionPrefixChar);
                resultBuilder.Append(move.PromotionResult.GetFenChar());
            }

            nextBoard = board.MakeMove(move);
            if (nextBoard.State == GameState.Checkmate)
            {
                resultBuilder.Append("#");
            }
            else if (nextBoard.State.IsCheck())
            {
                resultBuilder.Append("+");
            }

            return resultBuilder.ToString();
        }

        #endregion

        #region Private Methods

        private static Position[] GetKnightMovePositionsNonCached(Position position)
        {
            return GetOnboardPositions(position, KnightAttackOrMoveOffsets);
        }

        private static Position[] GetMovePositionArraysByRays(
            Position sourcePosition,
            IEnumerable<RayInfo> rays,
            int maxDistance)
        {
            var resultList = new List<Position>(AllPositions.Count);

            foreach (var ray in rays)
            {
                for (byte currentX88Value = (byte)(sourcePosition.X88Value + ray.Offset), distance = 1;
                    Position.IsValidX88Value(currentX88Value) && distance <= maxDistance;
                    currentX88Value += ray.Offset, distance++)
                {
                    var currentPosition = new Position(currentX88Value);
                    resultList.Add(currentPosition);
                }
            }

            return resultList.ToArray();
        }

        private static Omnifactotum.ReadOnlyDictionary<PositionBridgeKey, Bitboard> GeneratePositionBridgeMap()
        {
            var resultMap = new Dictionary<PositionBridgeKey, Bitboard>(AllPositions.Count * AllPositions.Count);

            var allPositions = AllPositions.ToArray();
            for (var outerIndex = 0; outerIndex < allPositions.Length; outerIndex++)
            {
                var first = allPositions[outerIndex];
                for (var innerIndex = outerIndex + 1; innerIndex < allPositions.Length; innerIndex++)
                {
                    var second = allPositions[innerIndex];

                    foreach (var ray in AllRays)
                    {
                        var positions = GetMovePositionArraysByRays(first, ray.AsArray(), MaxSlidingPieceDistance);
                        var index = Array.IndexOf(positions, second);
                        if (index < 0)
                        {
                            continue;
                        }

                        var positionBridgeKey = new PositionBridgeKey(first, second);
                        var positionBridge = new Bitboard(positions.Take(index));
                        resultMap.Add(positionBridgeKey, positionBridge);
                        break;
                    }
                }
            }

            return resultMap.AsReadOnly();
        }

        #endregion
    }
}