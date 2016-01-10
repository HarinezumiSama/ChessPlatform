using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ChessPlatform.Internal;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public static class ChessHelper
    {
        #region Constants and Fields

        public const double DefaultZeroTolerance = 1E-7d;

        public static readonly string PlatformVersion = typeof(ChessHelper)
            .Assembly
            .GetSingleCustomAttribute<AssemblyInformationalVersionAttribute>(false)
            .InformationalVersion;

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

        public static readonly ReadOnlyCollection<Square> AllSquares =
            Enumerable
                .Range(0, ChessConstants.SquareCount)
                .Select(squareIndex => new Square(squareIndex))
                .ToArray()
                .AsReadOnly();

        public static readonly PieceType DefaultPromotion = PieceType.Queen;

        internal const int MaxSlidingPieceDistance = 8;
        internal const int MaxPawnAttackOrMoveDistance = 1;
        internal const int MaxKingMoveOrAttackDistance = 1;

        internal static readonly ReadOnlyCollection<SquareShift> StraightRays =
            new ReadOnlyCollection<SquareShift>(
                new[]
                {
                    new SquareShift(0, 1),
                    new SquareShift(1, 0),
                    new SquareShift(0, -1),
                    new SquareShift(-1, 0)
                });

        internal static readonly ReadOnlyCollection<SquareShift> DiagonalRays =
            new ReadOnlyCollection<SquareShift>(
                new[]
                {
                    new SquareShift(1, 1),
                    new SquareShift(1, -1),
                    new SquareShift(-1, 1),
                    new SquareShift(-1, -1)
                });

        internal static readonly ReadOnlyCollection<SquareShift> AllRays =
            new ReadOnlyCollection<SquareShift>(StraightRays.Concat(DiagonalRays).ToArray());

        ////internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, SquareShift> PawnMoveRayMap =
        ////    new Omnifactotum.ReadOnlyDictionary<PieceColor, SquareShift>(
        ////        new Dictionary<PieceColor, SquareShift>
        ////        {
        ////            { PieceColor.White, new RayInfo(0x10, true) },
        ////            { PieceColor.Black, new RayInfo(0xF0, true) }
        ////        });

        ////internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, RayInfo> PawnEnPassantMoveRayMap =
        ////    new Omnifactotum.ReadOnlyDictionary<PieceColor, RayInfo>(
        ////        new Dictionary<PieceColor, RayInfo>
        ////        {
        ////            { PieceColor.White, new RayInfo(0x20, true) },
        ////            { PieceColor.Black, new RayInfo(0xE0, true) }
        ////        });

        ////internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>> PawnAttackRayMap =
        ////    new Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>(
        ////        new Dictionary<PieceColor, ReadOnlySet<RayInfo>>
        ////        {
        ////            {
        ////                PieceColor.White,
        ////                new[] { new RayInfo(0x0F, false), new RayInfo(0x11, false) }.ToHashSet().AsReadOnly()
        ////            },
        ////            {
        ////                PieceColor.Black,
        ////                new[] { new RayInfo(0xEF, false), new RayInfo(0xF1, false) }.ToHashSet().AsReadOnly()
        ////            }
        ////        });

        ////internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>
        ////    PawnReverseAttackRayMap =
        ////        new Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<RayInfo>>(
        ////            new Dictionary<PieceColor, ReadOnlySet<RayInfo>>
        ////            {
        ////                {
        ////                    PieceColor.White,
        ////                    new[] { new RayInfo(0xEF, false), new RayInfo(0xF1, false) }.ToHashSet().AsReadOnly()
        ////                },
        ////                {
        ////                    PieceColor.Black,
        ////                    new[] { new RayInfo(0x0F, false), new RayInfo(0x11, false) }.ToHashSet().AsReadOnly()
        ////                }
        ////            });

        ////internal static readonly Omnifactotum.ReadOnlyDictionary<PieceColor, ReadOnlySet<byte>> PawnAttackOffsetMap =
        ////    PawnAttackRayMap.ToDictionary(
        ////        pair => pair.Key,
        ////        pair => pair.Value.Select(item => item.Offset).ToHashSet().AsReadOnly()).AsReadOnly();

        ////internal static readonly ReadOnlySet<RayInfo> KingAttackRays = AllRays.ToHashSet().AsReadOnly();

        ////internal static readonly ReadOnlySet<byte> KingAttackOrMoveOffsets =
        ////    KingAttackRays.Select(item => item.Offset).ToHashSet().AsReadOnly();

        internal static readonly ReadOnlySet<SquareShift> KnightAttackOrMoveOffsets =
            new ReadOnlySet<SquareShift>(
                new HashSet<SquareShift>(
                    new[]
                    {
                        new SquareShift(+2, +1),
                        new SquareShift(+1, +2),
                        new SquareShift(+2, -1),
                        new SquareShift(+1, -2),
                        new SquareShift(-2, +1),
                        new SquareShift(-1, +2),
                        new SquareShift(-2, -1),
                        new SquareShift(-1, -2)
                    }));

        internal static readonly ReadOnlyCollection<PieceType> NonDefaultPromotions =
            ChessConstants.ValidPromotions.Except(DefaultPromotion.AsArray()).ToArray().AsReadOnly();

        internal static readonly Omnifactotum.ReadOnlyDictionary<SquareBridgeKey, Bitboard> SquareBridgeMap =
            GenerateSquareBridgeMap();

        internal static readonly Bitboard InvalidPawnSquaresBitboard =
            new Bitboard(Square.GenerateRanks(ChessConstants.RankRange.Lower, ChessConstants.RankRange.Upper));

        private const string FenRankRegexSnippet = @"[1-8KkQqRrBbNnPp]{1,8}";

        private const string MoveSeparator = ", ";

        private static readonly Omnifactotum.ReadOnlyDictionary<Square, Square[]>
            KnightMoveSquareMap =
                AllSquares
                    .ToDictionary(Factotum.Identity, GetKnightMoveSquaresNonCached)
                    .AsReadOnly();

        private static readonly Regex ValidFenRegex = new Regex(
            string.Format(
                @"^ \s* {0}/{0}/{0}/{0}/{0}/{0}/{0}/{0} \s+ (?:w|b) \s+ (?:[KkQq]+|\-) \s+ (?:[a-h][1-8]|\-) \s+ \d+ \s+ \d+ \s* $",
                FenRankRegexSnippet),
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        #endregion

        #region Public Methods

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

        public static string GetStandardAlgebraicNotation(
            [NotNull] this GameBoard board,
            [NotNull] ICollection<GameMove> moves)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            var resultBuilder = new StringBuilder();

            var currentBoard = board;
            foreach (var move in moves)
            {
                if (resultBuilder.Length != 0)
                {
                    resultBuilder.Append(MoveSeparator);
                }

                var notation = currentBoard.GetStandardAlgebraicNotationInternal(move, out currentBoard);
                resultBuilder.Append(notation);
            }

            return resultBuilder.ToString();
        }

        public static string ToUciNotation([NotNull] this GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            var isPromotion = move.PromotionResult != PieceType.None;

            var chars = new[]
            {
                move.From.FileChar,
                move.From.RankChar,
                move.To.FileChar,
                move.To.RankChar,
                isPromotion ? char.ToLowerInvariant(move.PromotionResult.GetFenChar()) : '\0'
            };

            return new string(chars, 0, isPromotion ? chars.Length : chars.Length - 1);
        }

        public static string ToUciNotation([NotNull] this ICollection<GameMove> moves)
        {
            #region Argument Check

            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            if (moves.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", nameof(moves));
            }

            #endregion

            return moves.Select(ToUciNotation).Join(MoveSeparator);
        }

        public static Square[] GetOnboardSquares(Square square, IEnumerable<SquareShift> shifts)
        {
            #region Argument Check

            if (shifts == null)
            {
                throw new ArgumentNullException(nameof(shifts));
            }

            #endregion

            return shifts.Select(shift => square + shift).Where(p => p.HasValue).Select(p => p.Value).ToArray();
        }

        #endregion

        #region Internal Methods

        internal static Square[] GetKnightMoveSquares(Square square)
        {
            return KnightMoveSquareMap[square];
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
                throw new ArgumentException($@"Invalid FEN character ({fenChar}).", nameof(fenChar));
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

                    var competitorSquares = board
                        .ValidMoves
                        .Keys
                        .Where(
                            obj => obj != move && obj.To == move.To && board[obj.From].GetPieceType() == pieceType)
                        .Select(obj => obj.From)
                        .ToArray();

                    if (competitorSquares.Length != 0)
                    {
                        var onSameFile = competitorSquares.Any(square => square.File == move.From.File);
                        var onSameRank = competitorSquares.Any(square => square.Rank == move.From.Rank);

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

        private static Square[] GetKnightMoveSquaresNonCached(Square square)
        {
            return GetOnboardSquares(square, KnightAttackOrMoveOffsets);
        }

        private static Square[] GetMoveSquareArraysByRays(
            Square sourceSquare,
            IEnumerable<SquareShift> rays,
            int maxDistance)
        {
            var resultList = new List<Square>(AllSquares.Count);

            foreach (var ray in rays)
            {
                var distance = 1;
                for (var square = sourceSquare + ray;
                    square.HasValue && distance <= maxDistance;
                    square = square.Value + ray, distance++)
                {
                    resultList.Add(square.Value);
                }
            }

            return resultList.ToArray();
        }

        private static Omnifactotum.ReadOnlyDictionary<SquareBridgeKey, Bitboard> GenerateSquareBridgeMap()
        {
            var resultMap = new Dictionary<SquareBridgeKey, Bitboard>(AllSquares.Count * AllSquares.Count);

            var allSquares = AllSquares.ToArray();
            for (var outerIndex = 0; outerIndex < allSquares.Length; outerIndex++)
            {
                var first = allSquares[outerIndex];
                for (var innerIndex = outerIndex + 1; innerIndex < allSquares.Length; innerIndex++)
                {
                    var second = allSquares[innerIndex];

                    foreach (var ray in AllRays)
                    {
                        var squares = GetMoveSquareArraysByRays(first, ray.AsArray(), MaxSlidingPieceDistance);
                        var index = Array.IndexOf(squares, second);
                        if (index < 0)
                        {
                            continue;
                        }

                        var key = new SquareBridgeKey(first, second);
                        var squareBridge = new Bitboard(squares.Take(index));
                        resultMap.Add(key, squareBridge);
                        break;
                    }
                }
            }

            return resultMap.AsReadOnly();
        }

        #endregion
    }
}