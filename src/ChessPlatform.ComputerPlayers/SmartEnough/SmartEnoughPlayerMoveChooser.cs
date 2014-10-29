using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ChessPlatform.GamePlay;
using ChessPlatform.Utilities;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class SmartEnoughPlayerMoveChooser
    {
        #region Constants and Fields

        public const int MaxPlyDepthLowerLimit = 2;

        private const int KingTropismNormingFactor = 14;
        private const int KingTropismRelativeFactor = 5;

        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMaterialWeightMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMaterialWeightMap());

        // ReSharper disable once UnusedMember.Local
        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMobilityWeightMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMobilityWeightMap());

        private static readonly EnumFixedSizeDictionary<Piece, PositionDictionary<int>> PieceToPositionWeightMap =
            new EnumFixedSizeDictionary<Piece, PositionDictionary<int>>(CreatePieceToPositionWeightMap());

        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToKingTropismWeightMap =
            CreatePieceTypeToKingTropismWeightMap();

        private readonly GameBoard _rootBoard;
        private readonly int _maxPlyDepth;
        private readonly BestMoveInfo _previousIterationBestMoveInfo;
        private readonly CancellationToken _cancellationToken;
        private readonly SimpleTranspositionTable _transpositionTable;
        private readonly BoardCache _boardCache;
        private readonly ScoreCache _previousIterationScoreCache;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmartEnoughPlayerMoveChooser"/> class.
        /// </summary>
        internal SmartEnoughPlayerMoveChooser(
            [NotNull] GameBoard rootBoard,
            int maxPlyDepth,
            [NotNull] BoardCache boardCache,
            [CanBeNull] ScoreCache previousIterationScoreCache,
            [CanBeNull] BestMoveInfo previousIterationBestMoveInfo,
            CancellationToken cancellationToken)
        {
            #region Argument Check

            if (rootBoard == null)
            {
                throw new ArgumentNullException("rootBoard");
            }

            if (maxPlyDepth < MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    "maxPlyDepth",
                    maxPlyDepth,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The value must be at least {0}.",
                        MaxPlyDepthLowerLimit));
            }

            #endregion

            _rootBoard = rootBoard;
            _maxPlyDepth = maxPlyDepth;
            _boardCache = boardCache;
            _previousIterationScoreCache = previousIterationScoreCache;
            _previousIterationBestMoveInfo = previousIterationBestMoveInfo;
            _cancellationToken = cancellationToken;

            _transpositionTable = new SimpleTranspositionTable(0); // Disabled for now due to bug
            this.ScoreCache = new ScoreCache(rootBoard);
        }

        #endregion

        #region Public Properties

        public long NodeCount
        {
            [DebuggerStepThrough]
            get
            {
                return _boardCache.TotalRequestCount;
            }
        }

        public ScoreCache ScoreCache
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public BestMoveInfo GetBestMove()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var stopwatch = Stopwatch.StartNew();
            var result = GetBestMoveInternal();
            stopwatch.Stop();

            Trace.TraceInformation(
                @"[{0}] Result: {1}, {2} spent, PV: {{ {3} }}, TT {{hits {4}/{5}, size {6}/{7}}}"
                    + @", B-Cache {{hits {8}/{9}, size {10}/{11}}}, for ""{12}"".",
                currentMethodName,
                result,
                stopwatch.Elapsed,
                result.PrincipalVariationMoves.Select(item => item.ToString()).Join(", "),
                _transpositionTable.HitCount,
                _transpositionTable.TotalRequestCount,
                _transpositionTable.ItemCount,
                _transpositionTable.MaximumItemCount,
                _boardCache.HitCount,
                _boardCache.TotalRequestCount,
                _boardCache.ItemCount,
                _boardCache.MaximumItemCount,
                _rootBoard.GetFen());

            return result;
        }

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetMaterialWeight(PieceType pieceType)
        {
            return PieceTypeToMaterialWeightMap[pieceType];
        }

        #endregion

        #region Private Methods

        private static Dictionary<PieceType, int> CreatePieceTypeToMaterialWeightMap()
        {
            return new Dictionary<PieceType, int>
            {
                { PieceType.King, 20000 },
                { PieceType.Queen, 900 },
                { PieceType.Rook, 500 },
                { PieceType.Bishop, 330 },
                { PieceType.Knight, 320 },
                { PieceType.Pawn, 100 },
                { PieceType.None, 0 }
            };
        }

        private static Dictionary<PieceType, int> CreatePieceTypeToMobilityWeightMap()
        {
            return new Dictionary<PieceType, int>
            {
                { PieceType.King, 20 },
                { PieceType.Queen, 10 },
                { PieceType.Rook, 6 },
                { PieceType.Bishop, 5 },
                { PieceType.Knight, 5 },
                { PieceType.Pawn, 4 }
            };
        }

        private static EnumFixedSizeDictionary<PieceType, int> CreatePieceTypeToKingTropismWeightMap()
        {
            var result = new EnumFixedSizeDictionary<PieceType, int>(PieceTypeToMaterialWeightMap);
            result[PieceType.King] = 0;
            return result;
        }

        private static PositionDictionary<int> ToPositionWeightMap(PieceColor color, int[,] weights)
        {
            #region Argument Check

            if (weights == null)
            {
                throw new ArgumentNullException("weights");
            }

            if (weights.Length != ChessConstants.SquareCount)
            {
                throw new ArgumentException(@"Invalid array length.", "weights");
            }

            #endregion

            var result = new PositionDictionary<int>();

            var startRank = color == PieceColor.White
                ? ChessConstants.RankRange.Upper
                : ChessConstants.RankRange.Lower;
            var rankIncrement = color == PieceColor.White ? -1 : 1;

            for (int rank = startRank, rankIndex = ChessConstants.RankRange.Lower;
                rankIndex <= ChessConstants.RankRange.Upper;
                rankIndex++, rank += rankIncrement)
            {
                for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
                {
                    var weight = weights[rankIndex, file];
                    result.Add(new Position(file, rank), weight);
                }
            }

            return result;
        }

        private static Dictionary<Piece, PositionDictionary<int>> CreatePieceToPositionWeightMap()
        {
            var weightGetters =
                new Dictionary<PieceType, Func<int[,]>>
                {
                    { PieceType.Pawn, CreatePawnPositionWeightMap },
                    { PieceType.Knight, CreateKnightPositionWeightMap },
                    { PieceType.Bishop, CreateBishopPositionWeightMap },
                    { PieceType.Rook, CreateRookPositionWeightMap },
                    { PieceType.Queen, CreateQueenPositionWeightMap },
                    { PieceType.King, CreateKingPositionWeightMap },
                };

            var result = weightGetters
                .SelectMany(
                    pair =>
                        ChessConstants.PieceColors.Select(
                            color =>
                                new
                                {
                                    Piece = pair.Key.ToPiece(color),
                                    Weights = ToPositionWeightMap(color, pair.Value())
                                }))
                .ToDictionary(obj => obj.Piece, obj => obj.Weights);

            return result;
        }

        private static int[,] CreatePawnPositionWeightMap()
        {
            var weights = new[,]
            {
                { 000, 000, 000, 000, 000, 000, 000, 000 },
                { +50, +50, +50, +50, +50, +50, +50, +50 },
                { +10, +10, +20, +30, +30, +20, +10, +10 },
                { +05, +05, +10, +25, +25, +10, +05, +05 },
                { 000, 000, 000, +20, +20, 000, 000, 000 },
                { +05, -05, -10, 000, 000, -10, -05, +05 },
                { +05, +10, +10, -20, -20, +10, +10, +05 },
                { 000, 000, 000, 000, 000, 000, 000, 000 }
            };

            return weights;
        }

        private static int[,] CreateKnightPositionWeightMap()
        {
            var weights = new[,]
            {
                { -50, -40, -30, -30, -30, -30, -40, -50 },
                { -40, -20, 000, 000, 000, 000, -20, -40 },
                { -30, 000, +10, +15, +15, +10, 000, -30 },
                { -30, +05, +15, +20, +20, +15, +05, -30 },
                { -30, 000, +15, +20, +20, +15, 000, -30 },
                { -30, +05, +10, +15, +15, +10, +05, -30 },
                { -40, -20, 000, +05, +05, 000, -20, -40 },
                { -50, -40, -30, -30, -30, -30, -40, -50 }
            };

            return weights;
        }

        private static int[,] CreateBishopPositionWeightMap()
        {
            var weights = new[,]
            {
                { -20, -10, -10, -10, -10, -10, -10, -20 },
                { -10, 0, 0, 0, 0, 0, 0, -10 },
                { -10, 0, 5, 10, 10, 5, 0, -10 },
                { -10, 5, 5, 10, 10, 5, 5, -10 },
                { -10, 0, 10, 10, 10, 10, 0, -10 },
                { -10, 10, 10, 10, 10, 10, 10, -10 },
                { -10, 5, 0, 0, 0, 0, 5, -10 },
                { -20, -10, -10, -10, -10, -10, -10, -20 }
            };

            return weights;
        }

        private static int[,] CreateRookPositionWeightMap()
        {
            var weights = new[,]
            {
                { 000, 000, 000, 000, 000, 000, 000, 000 },
                { +05, +10, +10, +10, +10, +10, +10, +05 },
                { -05, 000, 000, 000, 000, 000, 000, -05 },
                { -05, 000, 000, 000, 000, 000, 000, -05 },
                { -05, 000, 000, 000, 000, 000, 000, -05 },
                { -05, 000, 000, 000, 000, 000, 000, -05 },
                { -05, 000, 000, 000, 000, 000, 000, -05 },
                { 000, 000, 000, +05, +05, 000, 000, 000 }
            };

            return weights;
        }

        private static int[,] CreateQueenPositionWeightMap()
        {
            var weights = new[,]
            {
                { -20, -10, -10, -05, -05, -10, -10, -20 },
                { -10, 000, 000, 000, 000, 000, 000, -10 },
                { -10, 000, +05, +05, +05, +05, 000, -10 },
                { -05, 000, +05, +05, +05, +05, 000, -05 },
                { 000, 000, +05, +05, +05, +05, 000, -05 },
                { -10, +05, +05, +05, +05, +05, 000, -10 },
                { -10, 000, +05, 000, 000, 000, 000, -10 },
                { -20, -10, -10, -05, -05, -10, -10, -20 }
            };

            return weights;
        }

        private static int[,] CreateKingPositionWeightMap()
        {
            // King middle game
            var weights = new[,]
            {
                { -30, -40, -40, -50, -50, -40, -40, -30 },
                { -30, -40, -40, -50, -50, -40, -40, -30 },
                { -30, -40, -40, -50, -50, -40, -40, -30 },
                { -30, -40, -40, -50, -50, -40, -40, -30 },
                { -20, -30, -30, -40, -40, -30, -30, -20 },
                { -10, -20, -20, -20, -20, -20, -20, -10 },
                { +20, +20, 000, 000, 000, 000, +20, +20 },
                { +20, +30, +10, 000, 000, +10, +30, +20 }
            };

            ////// King end game
            ////var weights = new[,]
            ////{
            ////    { -50, -40, -30, -20, -20, -30, -40, -50 },
            ////    { -30, -20, -10, 000, 000, -10, -20, -30 },
            ////    { -30, -10, +20, +30, +30, +20, -10, -30 },
            ////    { -30, -10, +30, +40, +40, +30, -10, -30 },
            ////    { -30, -10, +30, +40, +40, +30, -10, -30 },
            ////    { -30, -10, +20, +30, +30, +20, -10, -30 },
            ////    { -30, -30, 000, 000, 000, 000, -30, -30 },
            ////    { -50, -30, -30, -30, -30, -30, -30, -50 }
            ////};

            return weights;
        }

        // ReSharper disable once UnusedParameter.Local - Temporary
        private static GamePhase GetGamePhase([NotNull] GameBoard board)
        {
            //////// TODO [vmcl] Think up a good idea of determining the game phase

            return GamePhase.Undetermined;

            ////var endGameScoreLimit = PieceTypeToMaterialWeightMap[PieceType.Rook] * 2;

            ////var whiteMaterialScore = EvaluateMaterialAndItsPositionByColor(board, PieceColor.White, null);
            ////var blackMaterialScore = EvaluateMaterialAndItsPositionByColor(board, PieceColor.Black, null);

            ////return whiteMaterialScore <= endGameScoreLimit || blackMaterialScore <= endGameScoreLimit
            ////    ? GamePhase.Endgame
            ////    : GamePhase.Middlegame;
        }

        private static int EvaluateMaterialAndItsPositionByColor(
            [NotNull] GameBoard board,
            PieceColor color,
            GamePhase? gamePhase)
        {
            var result = 0;

            if (gamePhase.HasValue)
            {
                var king = PieceType.King.ToPiece(color);
                var position = board.GetBitboard(king).GetFirstPosition();
                var positionWeightMap = PieceToPositionWeightMap[king];
                var positionScore = positionWeightMap[position];
                result += positionScore;
            }

            foreach (var pieceType in ChessConstants.PieceTypesExceptNoneAndKing)
            {
                var piece = pieceType.ToPiece(color);
                var pieceBitboard = board.GetBitboard(piece);
                if (pieceBitboard.IsNone)
                {
                    continue;
                }

                var materialWeight = GetMaterialWeight(pieceType);
                if (!gamePhase.HasValue)
                {
                    var pieceCount = pieceBitboard.GetBitSetCount();
                    result += materialWeight * pieceCount;
                    continue;
                }

                var positionWeightMap = PieceToPositionWeightMap[piece];

                var remainingBitboard = pieceBitboard;
                int currentSquareIndex;
                while ((currentSquareIndex = Bitboard.PopFirstBitSetIndex(ref remainingBitboard)) >= 0)
                {
                    result += materialWeight;

                    var position = Position.FromSquareIndex(currentSquareIndex);
                    var positionScore = positionWeightMap[position];
                    result += positionScore;
                }
            }

            return result;
        }

        private static int EvaluateMaterialAndItsPosition([NotNull] GameBoard board, GamePhase? gamePhase)
        {
            var activeScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor, gamePhase);
            var inactiveScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor.Invert(), gamePhase);

            return activeScore - inactiveScore;
        }

        // ReSharper disable once UnusedMember.Local
        private static int EvaluateBoardMobility([NotNull] GameBoard board)
        {
            var result = board
                .ValidMoves
                .Keys
                .Sum(move => PieceTypeToMobilityWeightMap[board[move.From].GetPieceType()]);

            return result;
        }

        private static GameMove GetCheapestAttackerMove([NotNull] GameBoard board, Position position)
        {
            //// TODO [vmcl] Consider en passant capture

            var cheapestAttackerMove = board
                .ValidMoves
                .Where(pair => pair.Key.To == position && pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderBy(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .FirstOrDefault();

            return cheapestAttackerMove;
        }

        private static int GetKingTropismDistance(Position attackerPosition, Position kingPosition)
        {
            var result = Math.Abs(attackerPosition.Rank - kingPosition.Rank)
                - Math.Abs(attackerPosition.File - kingPosition.File);

            return result;
        }

        private static int GetKingTropismScore(
            [NotNull] GameBoard board,
            Position attackerPosition,
            Position kingPosition)
        {
            var proximity = KingTropismNormingFactor - GetKingTropismDistance(attackerPosition, kingPosition);
            var attackerPieceType = board[attackerPosition].GetPieceType();
            var score = proximity * PieceTypeToKingTropismWeightMap[attackerPieceType] / KingTropismNormingFactor;

            return score;
        }

        private static int EvaluateKingTropism([NotNull] GameBoard board, PieceColor kingColor)
        {
            var king = PieceType.King.ToPiece(kingColor);
            var kingPosition = board.GetBitboard(king).GetFirstPosition();
            var allAttackersBitboard = board.GetBitboard(kingColor.Invert());

            var result = 0;

            var remainingAttackers = allAttackersBitboard;
            int attackerSquareIndex;
            while ((attackerSquareIndex = Bitboard.PopFirstBitSetIndex(ref remainingAttackers)) >= 0)
            {
                var attackerPosition = Position.FromSquareIndex(attackerSquareIndex);
                var score = GetKingTropismScore(board, attackerPosition, kingPosition);
                result -= score;
            }

            return result / KingTropismRelativeFactor;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private GameMove[] OrderMoves([NotNull] GameBoard board, int plyDistance)
        {
            const string InternalLogicErrorInMoveOrdering = "Internal logic error in move ordering procedure.";

            var resultList = new List<GameMove>(board.ValidMoves.Count);

            var validMoves = board.ValidMoves.ToArray();

            if (_previousIterationScoreCache != null && plyDistance == 0)
            {
                var movesOrderedByScore = _previousIterationScoreCache.OrderMovesByScore();
                resultList.AddRange(movesOrderedByScore.Select(pair => pair.Key));

                if (resultList.Count != board.ValidMoves.Count)
                {
                    throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
                }

                return resultList.ToArray();
            }

            if (_previousIterationBestMoveInfo != null
                && plyDistance < _previousIterationBestMoveInfo.PrincipalVariationMoves.Count)
            {
                var principalVariationMove = _previousIterationBestMoveInfo.PrincipalVariationMoves[plyDistance];
                if (board.ValidMoves.ContainsKey(principalVariationMove))
                {
                    resultList.Add(principalVariationMove);
                    validMoves = validMoves.Where(pair => pair.Key != principalVariationMove).ToArray();
                }
            }

            var opponentKing = PieceType.King.ToPiece(board.ActiveColor.Invert());
            var opponentKingPosition = board.GetBitboard(opponentKing).GetFirstPosition();

            var capturingMoves = validMoves
                .Where(pair => pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderByDescending(move => GetMaterialWeight(board[move.To].GetPieceType()))
                .ThenBy(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            resultList.AddRange(capturingMoves);

            var nonCapturingMoves = validMoves
                .Where(pair => !pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderBy(move => GetKingTropismDistance(move.To, opponentKingPosition))
                ////.OrderByDescending(move => GetKingTropismScore(board, move.To, opponentKingPosition))
                .ThenByDescending(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            resultList.AddRange(nonCapturingMoves);

            if (resultList.Count != board.ValidMoves.Count)
            {
                throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
            }

            return resultList.ToArray();
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        // ReSharper disable once UnusedParameter.Local
        private int EvaluateMobility([NotNull] GameBoard board)
        {
            return 0;

            ////if (!board.CanMakeNullMove)
            ////{
            ////    return 0;
            ////}

            ////var nullMoveBoard = _boardCache.MakeNullMove(board);

            ////var mobility = EvaluateBoardMobility(board);
            ////var opponentMobility = EvaluateBoardMobility(nullMoveBoard);

            ////var result = mobility - opponentMobility;
            ////return result;
        }

        private int EvaluatePositionScore([NotNull] GameBoard board, int plyDistance)
        {
            switch (board.State)
            {
                case GameState.Checkmate:
                    return -LocalConstants.MateScoreAbs + plyDistance;

                case GameState.Stalemate:
                    return 0;

                default:
                    {
                        var autoDrawType = board.GetAutoDrawType();
                        if (autoDrawType != AutoDrawType.None)
                        {
                            return 0;
                        }
                    }

                    break;
            }

            var gamePhase = GetGamePhase(board);
            var materialAndItsPosition = EvaluateMaterialAndItsPosition(board, gamePhase);
            var mobility = EvaluateMobility(board);
            var kingTropism = EvaluateKingTropism(board, board.ActiveColor)
                - EvaluateKingTropism(board, board.ActiveColor.Invert());

            var result = materialAndItsPosition + mobility + kingTropism;
            return result;
        }

        private int ComputeStaticExchangeEvaluationScore(
            [NotNull] GameBoard board,
            Position position,
            [CanBeNull] GameMove move)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var actualMove = move ?? GetCheapestAttackerMove(board, position);
            if (actualMove == null)
            {
                return 0;
            }

            var currentBoard = _boardCache.MakeMove(board, actualMove);
            var weight = GetMaterialWeight(currentBoard.LastCapturedPiece.GetPieceType());

            var result = weight - ComputeStaticExchangeEvaluationScore(currentBoard, position, null);

            if (move == null && result < 0)
            {
                // If it's not the root move, then the side to move has an option to stand pat
                result = 0;
            }

            return result;
        }

        private int Quiesce([NotNull] GameBoard board, int alpha, int beta, int plyDistance)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var standPatScore = EvaluatePositionScore(board, plyDistance);
            if (beta <= standPatScore)
            {
                return beta;
            }

            if (alpha < standPatScore)
            {
                alpha = standPatScore;
            }

            var captureMoves = board.ValidMoves.Where(pair => pair.Value.IsCapture).Select(pair => pair.Key).ToArray();
            foreach (var captureMove in captureMoves)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var seeScore = ComputeStaticExchangeEvaluationScore(board, captureMove.To, captureMove);
                if (seeScore < 0)
                {
                    continue;
                }

                var currentBoard = _boardCache.MakeMove(board, captureMove);
                var score = -Quiesce(currentBoard, -beta, -alpha, plyDistance);

                if (beta <= score)
                {
                    // Fail-hard beta-cutoff
                    return beta;
                }

                if (alpha < score)
                {
                    alpha = score;
                }
            }

            return alpha;
        }

        private AlphaBetaScore ComputeAlphaBeta(
            [NotNull] GameBoard board,
            int plyDistance,
            AlphaBetaScore alpha,
            AlphaBetaScore beta)
        {
            #region Argument Check

            if (plyDistance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "plyDistance",
                    plyDistance,
                    @"The value must be positive.");
            }

            #endregion

            _cancellationToken.ThrowIfCancellationRequested();

            var cachedScore = _transpositionTable.GetScore(board, plyDistance);
            if (cachedScore != null)
            {
                return cachedScore;
            }

            var plyDepth = _maxPlyDepth - plyDistance;
            if (plyDepth == 0 || board.ValidMoves.Count == 0)
            {
                var quiesceScore = Quiesce(board, alpha.Value, beta.Value, plyDistance);
                var score = new AlphaBetaScore(quiesceScore);
                _transpositionTable.SaveScore(board, plyDistance, score);
                return score;
            }

            var bestScore = alpha;

            var orderedMoves = OrderMoves(board, plyDistance);
            foreach (var move in orderedMoves)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var currentBoard = _boardCache.MakeMove(board, move);
                var score = -ComputeAlphaBeta(currentBoard, plyDistance + 1, -beta, -alpha);

                if (score.Value >= beta.Value)
                {
                    // Fail-hard beta-cutoff
                    var betaScore = beta;
                    _transpositionTable.SaveScore(board, plyDistance, betaScore);
                    return betaScore;
                }

                if (score.Value > alpha.Value)
                {
                    alpha = score;
                    bestScore = move + score;
                }
            }

            _transpositionTable.SaveScore(board, plyDistance, bestScore);

            return bestScore;
        }

        private BestMoveInfo ComputeAlphaBetaRoot(GameBoard board)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var orderedMoves = OrderMoves(board, 0);
            if (orderedMoves.Length == 0)
            {
                throw new InvalidOperationException(@"No moves to evaluate.");
            }

            GameMove bestMove = null;
            AlphaBetaScore bestAlphaBetaScore = null;
            var bestMoveLocalScore = int.MinValue;
            var stopwatch = new Stopwatch();

            foreach (var move in orderedMoves)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                stopwatch.Restart();
                var currentBoard = _boardCache.MakeMove(board, move);
                var localScore = -EvaluatePositionScore(currentBoard, 1);
                var score =
                    -ComputeAlphaBeta(currentBoard, 1, LocalConstants.RootAlphaScore, -LocalConstants.RootAlphaScore);
                stopwatch.Stop();

                var alphaBetaScore = move + score;
                this.ScoreCache[move] = alphaBetaScore;

                Trace.TraceInformation(
                    "[{0}] PV {1} (local {2}). Time spent: {3}",
                    currentMethodName,
                    alphaBetaScore,
                    localScore,
                    stopwatch.Elapsed);

                if (bestAlphaBetaScore != null && score.Value <= bestAlphaBetaScore.Value)
                {
                    continue;
                }

                bestMove = move;
                bestMoveLocalScore = localScore;
                bestAlphaBetaScore = alphaBetaScore;
            }

            Trace.TraceInformation(
                "[{0}] Best move {1}: {2} (local {3}).",
                currentMethodName,
                bestMove,
                bestAlphaBetaScore == null ? "?" : bestAlphaBetaScore.Value.ToString(CultureInfo.InvariantCulture),
                bestMoveLocalScore);

            var principalVariationMoves = bestAlphaBetaScore.EnsureNotNull().Moves.AsEnumerable().ToArray();
            return new BestMoveInfo(principalVariationMoves);
        }

        private BestMoveInfo GetBestMoveInternal()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var result = ComputeAlphaBetaRoot(_rootBoard);
            return result.EnsureNotNull();
        }

        #endregion
    }
}