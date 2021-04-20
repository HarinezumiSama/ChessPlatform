using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

//// ReSharper disable LoopCanBeConvertedToQuery - Using simpler loops for speed optimization
//// ReSharper disable ForCanBeConvertedToForeach - Using simpler loops for speed optimization
//// ReSharper disable ReturnTypeCanBeEnumerable.Local - Using simpler types (such as arrays) for speed optimization

namespace ChessPlatform.Engine
{
    internal sealed class EnginePlayerMoveSearcher
    {
        private const int QuiesceDepth = -1;

        private const int MaxDepthExtension = 6;

        private static readonly EvaluationScore NullWindowOffset = new EvaluationScore(1);

        private readonly GameBoard _rootBoard;
        private readonly int _plyDepth;
        private readonly BoardHelper _boardHelper;

        [CanBeNull]
        private readonly TranspositionTable _transpositionTable;

        private readonly VariationLineCache _previousIterationVariationLineCache;
        private readonly GameControlInfo _gameControlInfo;
        private readonly bool _useMultipleProcessors;
        private readonly MoveHistoryStatistics _moveHistoryStatistics;
        private readonly Evaluator _evaluator;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EnginePlayerMoveSearcher"/> class
        ///     using the specified parameters.
        /// </summary>
        internal EnginePlayerMoveSearcher(
            [NotNull] GameBoard rootBoard,
            int plyDepth,
            [NotNull] BoardHelper boardHelper,
            [CanBeNull] TranspositionTable transpositionTable,
            [CanBeNull] VariationLineCache previousIterationVariationLineCache,
            [NotNull] GameControlInfo gameControlInfo,
            bool useMultipleProcessors,
            [NotNull] MoveHistoryStatistics moveHistoryStatistics)
        {
            if (plyDepth < CommonEngineConstants.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDepth),
                    plyDepth,
                    $@"The value must be at least {CommonEngineConstants.MaxPlyDepthLowerLimit}.");
            }

            _rootBoard = rootBoard ?? throw new ArgumentNullException(nameof(rootBoard));
            _plyDepth = plyDepth;
            _boardHelper = boardHelper;
            _transpositionTable = transpositionTable;
            _previousIterationVariationLineCache = previousIterationVariationLineCache;
            _gameControlInfo = gameControlInfo ?? throw new ArgumentNullException(nameof(gameControlInfo));
            _useMultipleProcessors = useMultipleProcessors;
            _moveHistoryStatistics = moveHistoryStatistics ?? throw new ArgumentNullException(nameof(moveHistoryStatistics));
            _evaluator = new Evaluator(gameControlInfo, boardHelper);

            VariationLineCache = new VariationLineCache(rootBoard);
        }

        public long NodeCount
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _boardHelper.LocalMoveCount;
        }

        public VariationLineCache VariationLineCache
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public VariationLine GetBestMove()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var stopwatch = Stopwatch.StartNew();
            var result = GetBestMoveInternal(_rootBoard);
            stopwatch.Stop();

            Trace.WriteLine(
                $@"{Environment.NewLine
                    }[{currentMethodName}] {LocalHelper.GetTimestamp()}{Environment.NewLine
                    }  Depth: {_plyDepth}{Environment.NewLine
                    }  Result: {result.ToStandardAlgebraicNotationString(_rootBoard)}{Environment.NewLine
                    }  Time: {stopwatch.Elapsed}{Environment.NewLine
                    }  Nodes: {_boardHelper.LocalMoveCount:#,##0}{Environment.NewLine
                    }  FEN: {_rootBoard.GetFen()}{Environment.NewLine}");

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetCaptureOrPromotionValue(
            [NotNull] GameBoard board,
            [NotNull] GameMove move,
            GameMoveFlags moveFlags)
        {
            var result = moveFlags.IsAnyCapture()
                ? _evaluator.ComputeStaticExchangeEvaluationScore(board, move.To, move)
                : 0;

            if (moveFlags.IsPawnPromotion())
            {
                result += Evaluator.GetMaterialWeight(move.PromotionResult)
                    - Evaluator.GetMaterialWeight(PieceType.Pawn);
            }

            return result;
        }

        private OrderedMove[] GetOrderedMoves([NotNull] GameBoard board, int plyDistance)
        {
            const string InternalLogicErrorInMoveOrdering = "Internal logic error in move ordering procedure.";

            var resultList = new List<OrderedMove>(board.ValidMoves.Count);

            if (plyDistance == 0 && _previousIterationVariationLineCache != null)
            {
                var movesOrderedByScore = _previousIterationVariationLineCache.GetOrderedByScore();

                var orderedMoves = movesOrderedByScore
                    .Select(pair => new OrderedMove(pair.Key, board.ValidMoves[pair.Key]))
                    .ToArray();

                resultList.AddRange(orderedMoves);

                if (resultList.Count != board.ValidMoves.Count)
                {
                    throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
                }

                return resultList.ToArray();
            }

            var remainingMoves = new Dictionary<GameMove, GameMoveFlags>(board.ValidMoves);

            var entryProbe = _transpositionTable?.Probe(board.ZobristKey);
            var ttBestMove = entryProbe?.BestMove;
            if (ttBestMove != null)
            {
                GameMoveFlags moveFlags;
                if (remainingMoves.TryGetValue(ttBestMove, out moveFlags))
                {
                    resultList.Add(new OrderedMove(ttBestMove, moveFlags));
                    remainingMoves.Remove(ttBestMove);
                }
            }

            var opponentKing = board.ActiveSide.Invert().ToPiece(PieceType.King);
            var opponentKingSquare = board.GetBitboard(opponentKing).GetFirstSquare();

            var allCaptureOrPromotionDatas = remainingMoves
                .Where(pair => !LocalHelper.IsQuietMove(pair.Value))
                .Select(
                    pair =>
                        new
                        {
                            Move = pair.Key,
                            MoveInfo = pair.Value,
                            Value = GetCaptureOrPromotionValue(board, pair.Key, pair.Value)
                        })
                .ToArray();

            var goodCaptureOrPromotionMoves = allCaptureOrPromotionDatas
                .Where(obj => obj.Value >= 0)
                .OrderByDescending(obj => obj.Value)
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
                .Select(obj => new OrderedMove(obj.Move, obj.MoveInfo))
                .ToArray();

            resultList.AddRange(goodCaptureOrPromotionMoves);
            goodCaptureOrPromotionMoves.DoForEach(obj => remainingMoves.Remove(obj.Move));

            _moveHistoryStatistics.AddKillerMoves(plyDistance, remainingMoves, resultList);

            var nonCapturingMoves = remainingMoves
                .Where(pair => LocalHelper.IsQuietMove(pair.Value))
                .Select(
                    pair =>
                        new
                        {
                            Move = pair.Key,
                            MoveInfo = pair.Value,
                            Value = _moveHistoryStatistics.GetHistoryValue(board, pair.Key)
                        })
                .OrderByDescending(obj => obj.Value)
                .ThenBy(obj => Evaluator.GetKingTropismDistance(obj.Move.To, opponentKingSquare))
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
                .Select(obj => new OrderedMove(obj.Move, obj.MoveInfo))
                .ToArray();

            resultList.AddRange(nonCapturingMoves);
            nonCapturingMoves.DoForEach(obj => remainingMoves.Remove(obj.Move));

            var badCapturingMoves = allCaptureOrPromotionDatas
                .Where(obj => obj.Value < 0 && remainingMoves.ContainsKey(obj.Move))
                .OrderByDescending(obj => obj.Value)
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
                .Select(obj => new OrderedMove(obj.Move, obj.MoveInfo))
                .ToArray();

            resultList.AddRange(badCapturingMoves);
            badCapturingMoves.DoForEach(obj => remainingMoves.Remove(obj.Move));

            if (resultList.Count != board.ValidMoves.Count || remainingMoves.Count != 0)
            {
                throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
            }

            return resultList.ToArray();
        }

        private EvaluationScore Quiesce(
            [NotNull] GameBoard board,
            int plyDistance,
            EvaluationScore alpha,
            EvaluationScore beta,
            bool isPrincipalVariation)
        {
            _gameControlInfo.CheckInterruptions();

            EvaluationScore bestScore;
            EvaluationScore localScore;

            var entryProbe = _transpositionTable?.Probe(board.ZobristKey);
            if (entryProbe.HasValue)
            {
                var ttScore = entryProbe.Value.Score.ConvertValueFromTT(plyDistance);
                var bound = entryProbe.Value.Bound;

                if (!isPrincipalVariation && entryProbe.Value.Depth >= 0
                    && (bound & (ttScore.Value >= beta.Value ? ScoreBound.Lower : ScoreBound.Upper)) != 0)
                {
                    return ttScore;
                }

                localScore = entryProbe.Value.LocalScore;
                bestScore = localScore.ToRelative(plyDistance);

                if ((bound & (ttScore.Value > bestScore.Value ? ScoreBound.Lower : ScoreBound.Upper)) != 0)
                {
                    bestScore = ttScore;
                }
            }
            else
            {
                localScore = _evaluator.EvaluatePositionScore(board);
                bestScore = localScore.ToRelative(plyDistance);
            }

            // Stand pat if local evaluation is at least beta
            if (bestScore.Value >= beta.Value)
            {
                if (!entryProbe.HasValue && _transpositionTable != null)
                {
                    var entry = new TranspositionTableEntry(
                        board.ZobristKey,
                        null,
                        bestScore.ConvertValueForTT(plyDistance),
                        localScore,
                        ScoreBound.Lower,
                        QuiesceDepth);

                    _transpositionTable.Save(ref entry);
                }

                return bestScore;
            }

            var localAlpha = alpha;
            if (isPrincipalVariation && bestScore.Value > localAlpha.Value)
            {
                localAlpha = bestScore;
            }

            var nonQuietMovePairs = board
                .ValidMoves
                .Where(pair => !LocalHelper.IsQuietMove(pair.Value))
                .ToArray();

            GameMove bestMove = null;
            foreach (var movePair in nonQuietMovePairs)
            {
                _gameControlInfo.CheckInterruptions();

                if (movePair.Value.IsAnyCapture())
                {
                    var seeScore = _evaluator.ComputeStaticExchangeEvaluationScore(
                        board,
                        movePair.Key.To,
                        movePair.Key);

                    if (seeScore < 0)
                    {
                        continue;
                    }
                }

                var currentBoard = _boardHelper.MakeMove(board, movePair.Key);
                var score = -Quiesce(currentBoard, plyDistance + 1, -beta, -localAlpha, isPrincipalVariation);

                if (score.Value >= beta.Value)
                {
                    // Fail-soft beta-cutoff

                    if (_transpositionTable != null)
                    {
                        var entry = new TranspositionTableEntry(
                            board.ZobristKey,
                            movePair.Key,
                            score.ConvertValueForTT(plyDistance),
                            localScore,
                            ScoreBound.Lower,
                            QuiesceDepth);

                        _transpositionTable.Save(ref entry);
                    }

                    return score;
                }

                if (score.Value > bestScore.Value)
                {
                    bestScore = score;
                }

                if (score.Value > localAlpha.Value)
                {
                    localAlpha = score;
                    bestMove = movePair.Key;
                }
            }

            if (_transpositionTable != null)
            {
                var entry = new TranspositionTableEntry(
                    board.ZobristKey,
                    bestMove,
                    bestScore.ConvertValueForTT(plyDistance),
                    localScore,
                    isPrincipalVariation && bestScore.Value > alpha.Value ? ScoreBound.Exact : ScoreBound.Upper,
                    QuiesceDepth);

                _transpositionTable.Save(ref entry);
            }

            return bestScore;
        }

        [NotNull]
        private VariationLine ComputeAlphaBeta(
            [NotNull] GameBoard board,
            int plyDistance,
            int requestedMaxDepth,
            EvaluationScore alpha,
            EvaluationScore beta,
            bool isPrincipalVariation,
            bool skipHeuristicPruning,
            int totalDepthExtension)
        {
            if (plyDistance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDistance),
                    plyDistance,
                    @"The value must be positive.");
            }

            _gameControlInfo.CheckInterruptions();

            var autoDrawType = board.GetAutoDrawType();
            if (autoDrawType != AutoDrawType.None)
            {
                return VariationLine.Zero;
            }

            // Mate distance pruning
            var localAlpha = EvaluationScore.Max(alpha, EvaluationScore.CreateGettingCheckmatedScore(plyDistance));
            var localBeta = EvaluationScore.Min(beta, EvaluationScore.CreateCheckmatingScore(plyDistance + 1));

            if (localAlpha.Value >= localBeta.Value)
            {
                return new VariationLine(localAlpha);
            }

            var defaultRemainingDepth = Math.Max(0, requestedMaxDepth - plyDistance);

            var depthExtension = 0;
            if (totalDepthExtension < MaxDepthExtension)
            {
                if (defaultRemainingDepth <= 0 && board.State.IsCheck())
                {
                    depthExtension++;
                }
            }

            var innerTotalDepthExtension = totalDepthExtension + depthExtension;
            var maxDepth = requestedMaxDepth + depthExtension;

            var correctedRemainingDepth = Math.Max(0, maxDepth - plyDistance);

            EvaluationScore localScore;
            if (isPrincipalVariation)
            {
                localScore = _evaluator.EvaluatePositionScore(board);
            }
            else
            {
                var entryProbe = _transpositionTable?.Probe(board.ZobristKey);
                if (entryProbe.HasValue && entryProbe.Value.Depth >= correctedRemainingDepth)
                {
                    var ttScore = entryProbe.Value.Score.ConvertValueFromTT(plyDistance);
                    var bound = entryProbe.Value.Bound;
                    localScore = entryProbe.Value.LocalScore;

                    if ((bound & (ttScore.Value >= beta.Value ? ScoreBound.Lower : ScoreBound.Upper)) != 0)
                    {
                        var move = entryProbe.Value.BestMove;

                        if (move != null && ttScore.Value >= beta.Value)
                        {
                            _moveHistoryStatistics.RecordCutOffMove(
                                board,
                                move,
                                plyDistance,
                                correctedRemainingDepth,
                                null);
                        }

                        return move is null ? new VariationLine(ttScore) : move | new VariationLine(ttScore);
                    }
                }
                else
                {
                    localScore = _evaluator.EvaluatePositionScore(board);
                }
            }

            if (plyDistance >= maxDepth || board.ValidMoves.Count == 0)
            {
                var quiesceScore = Quiesce(board, plyDistance, localAlpha, localBeta, isPrincipalVariation);
                var result = new VariationLine(quiesceScore);
                return result;
            }

            if (!skipHeuristicPruning && !board.State.IsAnyCheck())
            {
                if (!isPrincipalVariation
                    && correctedRemainingDepth >= 2
                    && localScore.Value >= localBeta.Value
                    && board.CanMakeNullMove
                    && board.HasNonPawnMaterial(board.ActiveSide))
                {
                    //// TODO [HarinezumiSama] IDEA (board.HasNonPawnMaterial): Check also that non-pawn pieces have at least one legal move (to avoid zugzwang more thoroughly)

                    var staticEvaluation = _evaluator.EvaluatePositionScore(board);
                    if (staticEvaluation.Value >= localBeta.Value)
                    {
                        var depthReduction = correctedRemainingDepth > 6 ? 4 : 3;

                        var nullMoveBoard = _boardHelper.MakeNullMove(board);

                        var nullMoveLine = -ComputeAlphaBeta(
                            nullMoveBoard,
                            plyDistance + 1,
                            maxDepth - depthReduction,
                            -localBeta,
                            -localBeta + NullWindowOffset,
                            false,
                            true,
                            innerTotalDepthExtension);

                        var nullMoveScore = nullMoveLine.Value;

                        if (nullMoveScore.Value >= localBeta.Value)
                        {
                            if (nullMoveScore.IsCheckmating())
                            {
                                nullMoveScore = localBeta;
                            }

                            var verificationLine = ComputeAlphaBeta(
                                board,
                                plyDistance,
                                maxDepth - depthReduction,
                                localBeta - NullWindowOffset,
                                localBeta,
                                false,
                                true,
                                innerTotalDepthExtension);

                            if (verificationLine.Value.Value >= localBeta.Value)
                            {
                                return new VariationLine(nullMoveScore);
                            }
                        }
                    }
                }
            }

            VariationLine best = null;

            var orderedMoves = GetOrderedMoves(board, plyDistance);
            var moveCount = orderedMoves.Length;
            GameMove bestMove = null;
            var processedQuietMoves = new List<GameMove>(moveCount);
            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                _gameControlInfo.CheckInterruptions();

                var orderedMove = orderedMoves[moveIndex];
                var move = orderedMove.Move;

                var currentBoard = _boardHelper.MakeMove(board, move);

                var useNullWindow = !isPrincipalVariation || moveIndex > 0;

                VariationLine variationLine = null;
                if (useNullWindow)
                {
                    variationLine =
                        -ComputeAlphaBeta(
                            currentBoard,
                            plyDistance + 1,
                            maxDepth,
                            -localAlpha - NullWindowOffset,
                            -localAlpha,
                            false,
                            false,
                            innerTotalDepthExtension);
                }

                if (isPrincipalVariation
                    && (variationLine is null || moveIndex == 0
                        || (variationLine.Value.Value > localAlpha.Value
                            && variationLine.Value.Value < localBeta.Value)))
                {
                    variationLine =
                        -ComputeAlphaBeta(
                            currentBoard,
                            plyDistance + 1,
                            maxDepth,
                            -localBeta,
                            -localAlpha,
                            true,
                            false,
                            innerTotalDepthExtension);
                }

                if (variationLine.Value.Value >= localBeta.Value)
                {
                    // Fail-soft beta-cutoff
                    best = move | variationLine;

                    _moveHistoryStatistics.RecordCutOffMove(
                        board,
                        move,
                        plyDistance,
                        correctedRemainingDepth,
                        processedQuietMoves);

                    break;
                }

                if (best is null || variationLine.Value.Value > best.Value.Value)
                {
                    best = move | variationLine;

                    if (variationLine.Value.Value > localAlpha.Value)
                    {
                        localAlpha = variationLine.Value;
                        bestMove = move;
                    }
                }

                if (LocalHelper.IsQuietMove(orderedMove.MoveFlags))
                {
                    processedQuietMoves.Add(move);
                }
            }

            best = best.EnsureNotNull();

            if (_transpositionTable != null)
            {
                var ttEntry = new TranspositionTableEntry(
                    board.ZobristKey,
                    best.FirstMove,
                    best.Value.ConvertValueForTT(plyDistance),
                    localScore,
                    best.Value.Value >= localBeta.Value
                        ? ScoreBound.Lower
                        : (isPrincipalVariation && bestMove != null ? ScoreBound.Exact : ScoreBound.Upper),
                    correctedRemainingDepth);

                _transpositionTable.Save(ref ttEntry);
            }

            return best;
        }

        private VariationLine ComputeAlphaBetaRoot(
            GameBoard board,
            GameMove move,
            int rootMoveIndex,
            int moveCount)
        {
            _gameControlInfo.CheckInterruptions();

            const string CurrentMethodName = nameof(ComputeAlphaBetaRoot);
            const int StartingDelta = 25;

            var moveOrderNumber = rootMoveIndex + 1;

            var stopwatch = Stopwatch.StartNew();
            var currentBoard = _boardHelper.MakeMove(board, move);
            var localScore = -_evaluator.EvaluatePositionScore(currentBoard);

            var alpha = EvaluationScore.NegativeInfinity;
            var beta = EvaluationScore.PositiveInfinity;
            var delta = EvaluationScore.PositiveInfinityValue;

            if (_plyDepth >= 5)
            {
                var previousPvi = _previousIterationVariationLineCache?[move];
                if (previousPvi != null)
                {
                    delta = StartingDelta;

                    var previousValue = previousPvi.Value.Value;

                    alpha = new EvaluationScore(
                        Math.Max(previousValue - delta, EvaluationScore.NegativeInfinityValue));

                    beta = new EvaluationScore(
                        Math.Min(previousValue + delta, EvaluationScore.PositiveInfinityValue));
                }
            }

            VariationLine innerVariationLine;
            while (true)
            {
                innerVariationLine = -ComputeAlphaBeta(currentBoard, 1, _plyDepth, -beta, -alpha, true, false, 0);
                if (innerVariationLine.Value.Value <= alpha.Value)
                {
                    beta = new EvaluationScore((alpha.Value + beta.Value) / 2);

                    alpha = new EvaluationScore(
                        Math.Max(
                            innerVariationLine.Value.Value - delta,
                            EvaluationScore.NegativeInfinityValue));
                }
                else if (innerVariationLine.Value.Value >= beta.Value)
                {
                    alpha = new EvaluationScore((alpha.Value + beta.Value) / 2);

                    beta = new EvaluationScore(
                        Math.Min(
                            innerVariationLine.Value.Value + delta,
                            EvaluationScore.PositiveInfinityValue));
                }
                else
                {
                    break;
                }

                delta += delta / 2;
            }

            var variationLine = (move | innerVariationLine).WithLocalValue(localScore);
            stopwatch.Stop();

            Trace.WriteLine(
                $@"[{CurrentMethodName} #{moveOrderNumber:D2}/{moveCount:D2}] {move.ToStandardAlgebraicNotation(board)
                    }: {variationLine.ValueString} : L({variationLine.LocalValueString}), line: {{ {
                    board.GetStandardAlgebraicNotation(variationLine.Moves)} }}, time: {
                    stopwatch.Elapsed:g}");

            return variationLine;
        }

        private VariationLine GetBestMoveInternal(GameBoard board)
        {
            _gameControlInfo.CheckInterruptions();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var orderedMoves = GetOrderedMoves(board, 0);
            var moveCount = orderedMoves.Length;
            if (moveCount == 0)
            {
                throw new InvalidOperationException(@"No moves to evaluate.");
            }

            var threadCount = _useMultipleProcessors ? Math.Max(Environment.ProcessorCount - 1, 1) : 1;

            var tasks = orderedMoves
                .Select(
                    (orderedMove, index) =>
                        new Func<VariationLine>(
                            () => ComputeAlphaBetaRoot(board, orderedMove.Move, index, moveCount)))
                .ToArray();

            var multiTaskController = new MultiTaskController<VariationLine>(_gameControlInfo, threadCount, tasks);
            var variationLines = multiTaskController.GetResults();

            foreach (var variationLine in variationLines)
            {
                VariationLineCache[variationLine.FirstMove.EnsureNotNull()] = variationLine;
            }

            var orderedMovesByScore = VariationLineCache.GetOrderedByScore().ToArray();
            var bestVariation = orderedMovesByScore.First().Value.EnsureNotNull();

            var orderedVariationsString = orderedMovesByScore
                .Select(
                    (pair, index) =>
                        $@"  #{index + 1:D2}/{moveCount:D2} {pair.Value.ToStandardAlgebraicNotationString(board)}")
                .Join(Environment.NewLine);

            var killersInfoString = _moveHistoryStatistics.GetAllKillersInfoString();
            var historyInfoString = _moveHistoryStatistics.GetAllHistoryInfoString();

            var scoreValue = bestVariation.Value.Value.ToString(CultureInfo.InvariantCulture);

            Trace.WriteLine(
                $@"{Environment.NewLine}[{currentMethodName}] Best move {
                    board.GetStandardAlgebraicNotation(bestVariation.FirstMove.EnsureNotNull())}: {scoreValue}.{
                    Environment.NewLine}{Environment.NewLine}Variation Lines ordered by score:{Environment.NewLine}{
                    orderedVariationsString}{Environment.NewLine}{Environment.NewLine}Killer move stats:{
                    Environment.NewLine}{killersInfoString}{Environment.NewLine}{Environment.NewLine}History stats:{
                    Environment.NewLine}{historyInfoString}{Environment.NewLine}");

            return bestVariation;
        }
    }
}