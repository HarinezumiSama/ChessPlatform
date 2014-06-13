using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class SmartEnoughPlayerMoveChooser
    {
        #region Constants and Fields

        public const int MinimumMaxPlyDepth = 2;

        private const int MateScoreAbs = Int32.MaxValue / 2;

        private static readonly Dictionary<PieceType, int> PieceTypeToMaterialWeightMap =
            CreatePieceTypeToMaterialWeightMap();

        // ReSharper disable once UnusedMember.Local
        private static readonly Dictionary<PieceType, int> PieceTypeToMobilityWeightMap =
            CreatePieceTypeToMobilityWeightMap();

        private static readonly Dictionary<Piece, Dictionary<Position, int>> PieceToPositionWeightMap =
            CreatePieceToPositionWeightMap();

        private static readonly Dictionary<PieceType, int> PieceTypeToKingTropismWeightMap =
            new Dictionary<PieceType, int>(PieceTypeToMaterialWeightMap);

        private readonly IGameBoard _rootBoard;
        private readonly int _maxPlyDepth;
        private readonly PieceMove _previousIterationBestMove;
        private readonly CancellationToken _cancellationToken;
        private readonly SimpleTranspositionTable _transpositionTable;
        private readonly BoardCache _boardCache;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmartEnoughPlayerMoveChooser"/> class.
        /// </summary>
        internal SmartEnoughPlayerMoveChooser(
            [NotNull] IGameBoard rootBoard,
            int maxPlyDepth,
            [NotNull] BoardCache boardCache,
            [CanBeNull] PieceMove previousIterationBestMove,
            CancellationToken cancellationToken)
        {
            #region Argument Check

            if (rootBoard == null)
            {
                throw new ArgumentNullException("rootBoard");
            }

            if (maxPlyDepth < MinimumMaxPlyDepth)
            {
                throw new ArgumentOutOfRangeException(
                    "maxPlyDepth",
                    maxPlyDepth,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The value must be at least {0}.",
                        MinimumMaxPlyDepth));
            }

            #endregion

            _rootBoard = rootBoard;
            _maxPlyDepth = maxPlyDepth;
            _boardCache = boardCache;
            _previousIterationBestMove = previousIterationBestMove;
            _cancellationToken = cancellationToken;

            _transpositionTable = new SimpleTranspositionTable(1000000);
        }

        #endregion

        #region Public Methods

        public PieceMove GetBestMove()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var stopwatch = Stopwatch.StartNew();
            var result = GetBestMoveInternal();
            stopwatch.Stop();

            Trace.TraceInformation(
                @"[{0}] Result: {1}, {2} spent, TT {{hits {3}/{4}, size {5}/{6}}}"
                    + @", B-Cache {{hits {7}/{8}, size {9}/{10}}}, for ""{11}"".",
                currentMethodName,
                result,
                stopwatch.Elapsed,
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

        private static Dictionary<Position, int> ToPositionWeightMap(PieceColor color, int[,] weights)
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

            var result = new Dictionary<Position, int>();

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

        private static Dictionary<Piece, Dictionary<Position, int>> CreatePieceToPositionWeightMap()
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
        private static GamePhase GetGamePhase([NotNull] IGameBoard board)
        {
            //////// TODO [vmcl] Think up  a good idea of determining the game phase

            return GamePhase.Undetermined;

            ////var endGameScoreLimit = PieceTypeToMaterialWeightMap[PieceType.Rook] * 2;

            ////var whiteMaterialScore = EvaluateMaterialAndItsPositionByColor(board, PieceColor.White, null);
            ////var blackMaterialScore = EvaluateMaterialAndItsPositionByColor(board, PieceColor.Black, null);

            ////return whiteMaterialScore <= endGameScoreLimit || blackMaterialScore <= endGameScoreLimit
            ////    ? GamePhase.Endgame
            ////    : GamePhase.Middlegame;
        }

        private static int EvaluateMaterialAndItsPositionByColor(
            [NotNull] IGameBoard board,
            PieceColor color,
            GamePhase? gamePhase)
        {
            var result = 0;

            foreach (var pieceType in ChessConstants.PieceTypesExceptNone)
            {
                var piece = pieceType.ToPiece(color);
                var piecePositions = board.GetPiecePositions(piece);
                if (piecePositions.Length == 0)
                {
                    continue;
                }

                if (pieceType != PieceType.King)
                {
                    var materialWeight = PieceTypeToMaterialWeightMap[pieceType];
                    var materialScore = piecePositions.Length * materialWeight;
                    result += materialScore;
                }

                if (!gamePhase.HasValue)
                {
                    continue;
                }

                var positionWeightMap = PieceToPositionWeightMap[piece];
                var positionScore = piecePositions.Sum(position => positionWeightMap[position]);
                result += positionScore;
            }

            return result;
        }

        private static int EvaluateMaterialAndItsPosition([NotNull] IGameBoard board, GamePhase? gamePhase)
        {
            var activeScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor, gamePhase);
            var inactiveScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor.Invert(), gamePhase);

            return activeScore - inactiveScore;
        }

        // ReSharper disable once UnusedParameter.Local
        private static int EvaluateBoardMobility([NotNull] IGameBoard board)
        {
            return 0;

            ////var result = board
            ////    .ValidMoves
            ////    .Keys
            ////    .Sum(move => PieceTypeToMobilityWeightMap[board[move.From].GetPieceType()]);

            ////return result;
        }

        private static PieceMove GetCheapestAttackerMove([NotNull] IGameBoard board, Position position)
        {
            //// TODO [vmcl] Consider en passant capture

            var cheapestAttackerMove = board
                .ValidMoves
                .Where(pair => pair.Key.To == position && pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderBy(move => PieceTypeToMaterialWeightMap[board[move.From].GetPieceType()])
                .ThenByDescending(move => PieceTypeToMaterialWeightMap[move.PromotionResult])
                .FirstOrDefault();

            return cheapestAttackerMove;
        }

        private static int EvaluateKingTropism([NotNull] IGameBoard board, PieceColor kingColor)
        {
            var king = PieceType.King.ToPiece(kingColor);
            var kingPosition = board.GetPiecePositions(king).Single();
            var attackerPositions = board.GetPiecePositions(kingColor.Invert());

            const int NormingFactor = 14;

            var result = 0;
            foreach (var attackerPosition in attackerPositions)
            {
                var distance = NormingFactor
                    - (Math.Abs(attackerPosition.Rank - kingPosition.Rank)
                        + Math.Abs(attackerPosition.File - kingPosition.File));

                var attackerPieceType = board[attackerPosition].GetPieceType();
                var score = distance * PieceTypeToKingTropismWeightMap[attackerPieceType] / NormingFactor;

                result -= score;
            }

            return result;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private PieceMove[] OrderMoves([NotNull] IGameBoard board, int plyDistance)
        {
            var result = new List<PieceMove>(board.ValidMoves.Count);

            var validMoves = board.ValidMoves.ToArray();

            if (_previousIterationBestMove != null && plyDistance == 0)
            {
                if (!board.ValidMoves.ContainsKey(_previousIterationBestMove))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Invalid PV move ({0}).",
                            _previousIterationBestMove));
                }

                result.Add(_previousIterationBestMove);
                validMoves = validMoves.Where(pair => pair.Key != _previousIterationBestMove).ToArray();
            }

            var capturingMoves = validMoves
                .Where(pair => pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderByDescending(move => PieceTypeToMaterialWeightMap[board[move.To].GetPieceType()])
                .ThenBy(move => PieceTypeToMaterialWeightMap[board[move.From].GetPieceType()])
                .ThenByDescending(move => PieceTypeToMaterialWeightMap[move.PromotionResult])
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            result.AddRange(capturingMoves);

            var nonCapturingMoves = validMoves
                .Where(pair => !pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderByDescending(move => PieceTypeToMaterialWeightMap[board[move.From].GetPieceType()])
                .ThenByDescending(move => PieceTypeToMaterialWeightMap[move.PromotionResult])
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            result.AddRange(nonCapturingMoves);

            if (result.Count != board.ValidMoves.Count)
            {
                throw new InvalidOperationException("Internal logic error in move ordering procedure.");
            }

            return result.ToArray();
        }

        private int EvaluateMobility([NotNull] IGameBoard board)
        {
            if (!board.CanMakeNullMove)
            {
                return 0;
            }

            var nullMoveBoard = _boardCache.MakeNullMove(board);

            var mobility = EvaluateBoardMobility(board);
            var opponentMobility = EvaluateBoardMobility(nullMoveBoard);

            var result = mobility - opponentMobility;
            return result;
        }

        private int EvaluatePositionScore([NotNull] IGameBoard board, int plyDistance)
        {
            switch (board.State)
            {
                case GameState.Checkmate:
                    return -MateScoreAbs + plyDistance;

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

        private IGameBoard MakeMoveOptimized([NotNull] IGameBoard board, [NotNull] PieceMove move)
        {
            var result = _boardCache.MakeMove(board, move);
            return result;
        }

        private int ComputeStaticExchangeEvaluationScore(
            [NotNull] IGameBoard board,
            Position position,
            [CanBeNull] PieceMove move)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var actualMove = move ?? GetCheapestAttackerMove(board, position);
            if (actualMove == null)
            {
                return 0;
            }

            var currentBoard = MakeMoveOptimized(board, actualMove);
            var weight = PieceTypeToMaterialWeightMap[currentBoard.LastCapturedPiece.GetPieceType()];

            var result = weight - ComputeStaticExchangeEvaluationScore(currentBoard, position, null);

            if (move == null && result < 0)
            {
                // If it's not the root move, then the side to move has an option to stand pat
                result = 0;
            }

            return result;
        }

        private int Quiesce([NotNull] IGameBoard board, int alpha, int beta, int plyDistance)
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

                var currentBoard = MakeMoveOptimized(board, captureMove);
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

        private int ComputeAlphaBeta([NotNull] IGameBoard board, int plyDistance, int alpha, int beta)
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
            if (cachedScore.HasValue)
            {
                return cachedScore.Value;
            }

            var plyDepth = _maxPlyDepth - plyDistance;
            if (plyDepth == 0 || board.ValidMoves.Count == 0)
            {
                var result = Quiesce(board, alpha, beta, plyDistance);
                _transpositionTable.SaveScore(board, plyDistance, result);
                return result;
            }

            var orderedMoves = OrderMoves(board, plyDistance);
            foreach (var move in orderedMoves)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var currentBoard = MakeMoveOptimized(board, move);

                var score = -ComputeAlphaBeta(currentBoard, plyDistance + 1, -beta, -alpha);

                if (score >= beta)
                {
                    // Fail-hard beta-cutoff
                    _transpositionTable.SaveScore(board, plyDistance, beta);
                    return beta;
                }

                if (score > alpha)
                {
                    alpha = score;
                }
            }

            _transpositionTable.SaveScore(board, plyDistance, alpha);
            return alpha;
        }

        private PieceMove ComputeAlphaBetaRoot(IGameBoard board)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var orderedMoves = OrderMoves(board, 0);

            const int RootAlpha = checked(-MateScoreAbs - 1);

            PieceMove bestMove = null;
            var bestMoveLocalScore = Int32.MinValue;
            var alpha = RootAlpha;
            var stopwatch = new Stopwatch();

            foreach (var move in orderedMoves)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                stopwatch.Restart();

                var currentBoard = MakeMoveOptimized(board, move);
                var localScore = -EvaluatePositionScore(currentBoard, 1);

                var score = -ComputeAlphaBeta(currentBoard, 1, RootAlpha, -alpha);

                stopwatch.Stop();

                Trace.TraceInformation(
                    "[{0}] Move {1}: {2} (local {3}). Time spent: {4}",
                    currentMethodName,
                    move,
                    score,
                    localScore,
                    stopwatch.Elapsed);

                if (score <= alpha)
                {
                    continue;
                }

                alpha = score;
                bestMove = move;
                bestMoveLocalScore = localScore;
            }

            Trace.TraceInformation(
                "[{0}] Best move {1}: {2} (local {3}).",
                currentMethodName,
                bestMove,
                alpha,
                bestMoveLocalScore);

            return bestMove;
        }

        private PieceMove FindMateMove()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            //// TODO [vmcl] Ideally this method has to search for a guaranteed mate in a number of moves (rather than in mate-in-one only)

            var mateMoves = _rootBoard
                .ValidMoves
                .Keys
                .Where(
                    move =>
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var currentBoard = MakeMoveOptimized(_rootBoard, move);
                        return currentBoard.State == GameState.Checkmate;
                    })
                .ToArray();

            if (mateMoves.Length == 0)
            {
                return null;
            }

            return mateMoves
                .OrderBy(move => PieceTypeToMaterialWeightMap[_rootBoard[move.From].GetPieceType()])
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ThenBy(move => move.PromotionResult)
                .First();
        }

        private PieceMove GetBestMoveInternal()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var mateMove = FindMateMove();
            if (mateMove != null)
            {
                return mateMove;
            }

            var result = ComputeAlphaBetaRoot(_rootBoard);
            return result.EnsureNotNull();
        }

        #endregion
    }
}