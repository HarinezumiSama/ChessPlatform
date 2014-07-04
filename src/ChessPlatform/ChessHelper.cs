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

namespace ChessPlatform
{
    public static class ChessHelper
    {
        #region Constants and Fields

        public const double DefaultZeroTolerance = 1E-7d;

        public static readonly ReadOnlyDictionary<CastlingOptions, CastlingInfo> CastlingOptionToInfoMap =
            ChessConstants.AllCastlingInfos.ToDictionary(obj => obj.Option).AsReadOnly();

        public static readonly ReadOnlyDictionary<GameMove, CastlingInfo> KingMoveToCastlingInfoMap =
            ChessConstants.AllCastlingInfos.ToDictionary(obj => obj.KingMove).AsReadOnly();

        public static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<CastlingOptions>>
            ColorToCastlingOptionSetMap =
                new ReadOnlyDictionary<PieceColor, ReadOnlySet<CastlingOptions>>(
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

        public static readonly ReadOnlyDictionary<PieceColor, CastlingOptions> ColorToCastlingOptionsMap =
            ColorToCastlingOptionSetMap
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Aggregate(CastlingOptions.None, (a, item) => a | item))
                .AsReadOnly();

        public static readonly ReadOnlyDictionary<PieceColor, int> ColorToPawnPromotionRankMap =
            new ReadOnlyDictionary<PieceColor, int>(
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

        internal static readonly ReadOnlyDictionary<PieceColor, RayInfo> PawnMoveRayMap =
            new ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x10, true) },
                    { PieceColor.Black, new RayInfo(0xF0, true) }
                });

        internal static readonly ReadOnlyDictionary<PieceColor, RayInfo> PawnEnPassantMoveRayMap =
            new ReadOnlyDictionary<PieceColor, RayInfo>(
                new Dictionary<PieceColor, RayInfo>
                {
                    { PieceColor.White, new RayInfo(0x20, true) },
                    { PieceColor.Black, new RayInfo(0xE0, true) }
                });

        internal static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>> PawnAttackRayMap =
            new ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>(
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

        internal static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>> PawnReverseAttackRayMap =
            new ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>(
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

        internal static readonly ReadOnlyDictionary<PieceColor, ReadOnlySet<byte>> PawnAttackOffsetMap =
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

        internal static readonly ReadOnlyDictionary<AttackInfoKey, AttackInfo> TargetPositionToAttackInfoMap =
            GenerateTargetPositionToAttackInfoMap();

        internal static readonly ReadOnlyDictionary<PositionBridgeKey, Bitboard> PositionBridgeMap =
            GeneratePositionBridgeMap();

        internal static readonly Bitboard InvalidPawnPositionsBitboard =
            new Bitboard(Position.GenerateRanks(ChessConstants.RankRange.Lower, ChessConstants.RankRange.Upper));

        private const string FenRankRegexSnippet = @"[1-8KkQqRrBbNnPp]{1,8}";

        private static readonly ReadOnlyDictionary<Position, ReadOnlyCollection<Position>> KnightMovePositionMap =
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
                resultBuilder.AppendFormat(CultureInfo.InvariantCulture, " [rev. '{0}']", PlatformRevisionId);
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

        #endregion

        #region Internal Methods

        internal static Position[] GetOnboardPositions(Position position, ICollection<byte> x88Offsets)
        {
            #region Argument Check

            if (x88Offsets == null)
            {
                throw new ArgumentNullException("x88Offsets");
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
                throw new ArgumentNullException("hashSet");
            }

            if (collection == null)
            {
                throw new ArgumentNullException("collection");
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
                    "fenChar");
            }

            return result;
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

        private static Position[] GetMovePositionArraysByRays(
            Position sourcePosition,
            PieceType pieceType,
            int maxDistance)
        {
            var rays = new List<RayInfo>(AllRays.Count);

            if (pieceType.IsSlidingStraight())
            {
                rays.AddRange(StraightRays);
            }

            if (pieceType.IsSlidingDiagonally())
            {
                rays.AddRange(DiagonalRays);
            }

            if (rays.Count == 0)
            {
                throw new InvalidOperationException("The method is intended for sliding pieces.");
            }

            var result = GetMovePositionArraysByRays(sourcePosition, rays, maxDistance);
            return result;
        }

        private static ReadOnlyDictionary<AttackInfoKey, AttackInfo> GenerateTargetPositionToAttackInfoMap()
        {
            var resultMap = AllPositions
                .SelectMany(item => ChessConstants.PieceColors.Select(color => new AttackInfoKey(item, color)))
                .ToDictionary(
                    Factotum.Identity,
                    item => new Dictionary<PieceType, PieceAttackInfo>());

            Action<Position, PieceColor, PieceType, ICollection<Position>, bool> addAttack =
                (targetPosition, attackingColor, pieceType, positions, isDirectAttack) =>
                {
                    if (positions.Count != 0)
                    {
                        var key = new AttackInfoKey(targetPosition, attackingColor);
                        resultMap[key].Add(pieceType, new PieceAttackInfo(positions, isDirectAttack));
                    }
                };

            Action<Position, PieceType, ICollection<Position>, bool> addAllColorsAttack =
                (targetPosition, pieceType, positions, isDirectAttack) =>
                    ChessConstants.PieceColors.DoForEach(
                        attackingColor =>
                            addAttack(targetPosition, attackingColor, pieceType, positions, isDirectAttack));

            var slidingPieceTypes = new[] { PieceType.Bishop, PieceType.Rook, PieceType.Queen };

            foreach (var currentPosition in AllPositions)
            {
                var kingPositions = GetMovePositionArraysByRays(
                    currentPosition,
                    KingAttackRays,
                    MaxKingMoveOrAttackDistance);

                addAllColorsAttack(currentPosition, PieceType.King, kingPositions, true);

                var knightPositions = GetKnightMovePositionsNonCached(currentPosition);
                addAllColorsAttack(currentPosition, PieceType.Knight, knightPositions, true);

                foreach (var pieceType in slidingPieceTypes)
                {
                    var positions = GetMovePositionArraysByRays(
                        currentPosition,
                        pieceType,
                        MaxSlidingPieceDistance);

                    addAllColorsAttack(currentPosition, pieceType, positions, false);
                }
            }

            foreach (var position in AllPositions)
            {
                foreach (var pieceColor in ChessConstants.PieceColors)
                {
                    var rays = PawnReverseAttackRayMap[pieceColor];
                    var positions = GetMovePositionArraysByRays(position, rays, MaxPawnAttackOrMoveDistance)
                        .Where(item => AllPawnPositions.Contains(item))
                        .ToArray();

                    addAttack(position, pieceColor, PieceType.Pawn, positions, true);
                }
            }

            var result = resultMap.ToDictionary(pair => pair.Key, pair => new AttackInfo(pair.Value)).AsReadOnly();
            return result;
        }

        private static ReadOnlyDictionary<PositionBridgeKey, Bitboard> GeneratePositionBridgeMap()
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