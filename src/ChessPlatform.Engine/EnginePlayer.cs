using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChessPlatform.GamePlay;
using ChessPlatform.Logging;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public sealed class EnginePlayer : ChessPlayerBase
    {
        private static readonly string PlayerName =
            $@"{typeof(ChessPlayerBase).Assembly.GetName().Name}:{typeof(EnginePlayer).GetQualifiedName()}";

        private static readonly string LogSeparator = new string('-', 120);

        private readonly ILogger _logger;
        private readonly Random _openingBookRandom;
        private readonly IOpeningBook _openingBook;
        private readonly TimeSpan? _maxTimePerMove;
        private readonly bool _useMultipleProcessors;

        [CanBeNull]
        private readonly TranspositionTable _transpositionTable;

        public EnginePlayer(
            [NotNull] ILogger logger,
            [NotNull] IOpeningBookProvider openingBookProvider,
            GameSide side,
            [NotNull] EnginePlayerParameters parameters)
            : base(side)
        {
            if (openingBookProvider is null)
            {
                throw new ArgumentNullException(nameof(openingBookProvider));
            }

            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.MaxPlyDepth < CommonEngineConstants.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parameters.MaxPlyDepth),
                    parameters.MaxPlyDepth,
                    $@"The value must be at least {CommonEngineConstants.MaxPlyDepthLowerLimit}.");
            }

            if (parameters.MaxTimePerMove.HasValue && parameters.MaxTimePerMove.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parameters.MaxTimePerMove),
                    parameters.MaxTimePerMove,
                    @"The time per move, if specified, must be positive.");
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _openingBookRandom = new Random();
            MaxPlyDepth = parameters.MaxPlyDepth;
            _openingBook = parameters.UseOpeningBook ? openingBookProvider.VariedOpeningBook : null;
            _maxTimePerMove = parameters.MaxTimePerMove;
            _useMultipleProcessors = parameters.UseMultipleProcessors;

            _transpositionTable = parameters.UseTranspositionTable
                ? new TranspositionTable(_logger, parameters.TranspositionTableSizeInMegaBytes)
                : null;
        }

        public override string Name
        {
            [DebuggerStepThrough]
            get => PlayerName;
        }

        public int MaxPlyDepth
        {
            [DebuggerStepThrough]
            get;
        }

        protected override VariationLine DoGetMove(GetMoveRequest request)
        {
            request.CancellationToken.ThrowIfCancellationRequested();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();
            var board = request.Board;

            var useOpeningBook = _openingBook != null;

            _logger.Info(LogSeparator);

            _logger.Info(
                $@"[{currentMethodName}] BEGIN:{Environment.NewLine
                    }  Side: {Side}{Environment.NewLine
                    }  Max depth: {MaxPlyDepth} plies{Environment.NewLine
                    }  Max time: {_maxTimePerMove?.ToString("g") ?? "unlimited"}{Environment.NewLine
                    }  Multi CPU: {_useMultipleProcessors}{Environment.NewLine
                    }  Opening book: {useOpeningBook}{Environment.NewLine
                    }  FEN: {board.GetFen()}{Environment.NewLine
                    }  Key: {board.ZobristKey:X16}{Environment.NewLine}");

            var bestMoveContainer = new SyncValueContainer<BestMoveData>();
            Stopwatch stopwatch;

            CancellationTokenSource timeoutCancellationTokenSource = null;
            try
            {
                var internalCancellationToken = request.CancellationToken;
                var timeoutCancellationToken = CancellationToken.None;
                TimeSpan? maxMoveTime = null;
                if (_maxTimePerMove.HasValue)
                {
                    timeoutCancellationTokenSource = new CancellationTokenSource();
                    timeoutCancellationToken = timeoutCancellationTokenSource.Token;

                    var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        request.CancellationToken,
                        timeoutCancellationTokenSource.Token);

                    internalCancellationToken = linkedTokenSource.Token;

                    maxMoveTime = TimeSpan.FromTicks(_maxTimePerMove.Value.Ticks * 99L / 100L);
                    _logger.Verbose($@"[{currentMethodName}] Adjusted max time for move: {maxMoveTime.Value}");
                }

                var gameControlInfo = new GameControlInfo(request.GameControl, internalCancellationToken);

                var task = new Task(
                    () => DoGetMoveInternal(request.Board, gameControlInfo, bestMoveContainer),
                    internalCancellationToken);

                if (maxMoveTime.HasValue)
                {
                    timeoutCancellationTokenSource.CancelAfter(maxMoveTime.Value);
                }

                stopwatch = Stopwatch.StartNew();
                task.Start();
                try
                {
                    task.Wait(timeoutCancellationToken);
                }
                catch (AggregateException ex)
                    when (ex.Flatten().InnerException is MoveNowRequestedException)
                {
                    _logger.Info($@"[{currentMethodName}] Interrupting the search since the Move Now request was received.");
                }
                catch (AggregateException ex)
                    when (ex.Flatten().InnerException is OperationCanceledException)
                {
                    var operationCanceledException = (OperationCanceledException)ex.Flatten().InnerException;

                    //// ReSharper disable once InvertIf
                    if (operationCanceledException?.CancellationToken == internalCancellationToken
                        || operationCanceledException?.CancellationToken == request.CancellationToken)
                    {
                        _logger.Info($@"[{currentMethodName}] The search has been canceled by the caller.");
                        request.CancellationToken.ThrowIfCancellationRequested();
                    }

                    throw;
                }
                catch (OperationCanceledException ex)
                    when (ex.CancellationToken == timeoutCancellationToken)
                {
                    _logger.Info($@"[{currentMethodName}] Interrupting the search since maximum time per move has been reached.");
                }
                catch (OperationCanceledException)
                {
                    _logger.Info($@"[{currentMethodName}] The search has been canceled by the caller.");
                    throw;
                }

                stopwatch.Stop();
            }
            finally
            {
                timeoutCancellationTokenSource.DisposeSafely();
            }

            var bestMoveData = bestMoveContainer.Value;
            var principalVariationInfo = bestMoveData?.Variation;

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var nodeCount = bestMoveData?.NodeCount ?? 0L;

            var nps = elapsedSeconds.IsZero()
                ? "?"
                : Convert.ToInt64(nodeCount / elapsedSeconds).ToString("#,##0", CultureInfo.InvariantCulture);

            var principalVariationString = principalVariationInfo?.ToStandardAlgebraicNotationString(request.Board)
                ?? "<not found>";

            var depthString = bestMoveData?.PlyDepth.ToString(CultureInfo.InvariantCulture) ?? "?";

            _logger.Info(
                $@"[{currentMethodName}] END:{Environment.NewLine
                    }  Result: {principalVariationString}{Environment.NewLine
                    }  Depth: {depthString}{Environment.NewLine
                    }  Time: {stopwatch.Elapsed:g}{Environment.NewLine
                    }  Nodes: {nodeCount:#,##0}{Environment.NewLine
                    }  NPS: {nps}{Environment.NewLine
                    }  FEN: {board.GetFen()}{Environment.NewLine
                    }  Key: {board.ZobristKey:X16}{Environment.NewLine}");

            if (_transpositionTable != null)
            {
                var bucketCount = _transpositionTable.BucketCount;
                var probeCount = _transpositionTable.ProbeCount;
                var hitCount = _transpositionTable.HitCount;
                var saveCount = _transpositionTable.SaveCount;

                var hitRatio = probeCount == 0 ? 0 : (decimal)hitCount / probeCount * 100;

                _logger.Info(
                    $@"TT statistics:{Environment.NewLine
                        }  {nameof(TranspositionTable.BucketCount)}: {bucketCount:#,##0}{Environment.NewLine
                        }  {nameof(TranspositionTable.SaveCount)}: {saveCount:#,##0}{Environment.NewLine
                        }  {nameof(TranspositionTable.ProbeCount)}: {probeCount:#,##0}{Environment.NewLine
                        }  {nameof(TranspositionTable.HitCount)}: {hitCount:#,##0}{Environment.NewLine
                        }  Hit Ratio: {hitRatio:0.0}%{Environment.NewLine}");
            }

            _logger.Info(LogSeparator);

            if (principalVariationInfo is null)
            {
                throw new InvalidOperationException("Could not determine the best move. (Has timeout expired?)");
            }

            return principalVariationInfo;
        }

        protected override void Dispose(bool explicitDisposing)
        {
            base.Dispose(explicitDisposing);

            _transpositionTable?.Dispose();
        }

        private void DoGetMoveInternal(
            [NotNull] GameBoard board,
            [NotNull] GameControlInfo gameControlInfo,
            //// ReSharper disable once SuggestBaseTypeForParameter :: Performance critical code (trying to avoid virtual calls)
            SyncValueContainer<BestMoveData> bestMoveContainer)
        {
            gameControlInfo.CheckInterruptions();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            _logger.Info($@"[{currentMethodName}] Number of available moves: {board.ValidMoves.Count}.");

            if (_openingBook != null)
            {
                var openingMoves = _openingBook.FindPossibleMoves(board);
                if (openingMoves.Length != 0)
                {
                    var index = _openingBookRandom.Next(openingMoves.Length);
                    var selectedOpeningMove = openingMoves[index];

                    var boardAfterOpeningMove = board.MakeMove(selectedOpeningMove.Move);
                    var furtherOpeningMoves = _openingBook.FindPossibleMoves(boardAfterOpeningMove);

                    var openingMovesString = openingMoves
                        .Select(move => move.ToStandardAlgebraicNotation(board))
                        .Join(", ");

                    var selectedOpeningMoveString = selectedOpeningMove.ToStandardAlgebraicNotation(board);

                    var furtherOpeningMovesString = furtherOpeningMoves.Length == 0
                        ? "n/a"
                        : furtherOpeningMoves
                            .Select(move => move.ToStandardAlgebraicNotation(boardAfterOpeningMove))
                            .Join(", ");

                    _logger.Info(
                        $@"[{currentMethodName}] {selectedOpeningMoveString
                            } was randomly chosen from the known opening moves [{openingMovesString
                            }]. Further known opening move variants: {furtherOpeningMovesString}.");

                    bestMoveContainer.Value = new BestMoveData(selectedOpeningMove.Move | VariationLine.Zero, 1L, 1);
                    return;
                }
            }

            var boardHelper = new BoardHelper();
            var moveHistoryStatistics = new MoveHistoryStatistics();

            _transpositionTable?.NotifyNewSearch();

            var totalNodeCount = 0L;
            VariationLineCache variationLineCache = null;

            for (var plyDepth = CommonEngineConstants.MaxPlyDepthLowerLimit;
                plyDepth <= MaxPlyDepth;
                plyDepth++)
            {
                gameControlInfo.CheckInterruptions();

                _logger.Info($@"[{currentMethodName}] Iterative deepening: {plyDepth} of {MaxPlyDepth}.");

                boardHelper.ResetLocalMoveCount();

                var moveChooser = new EnginePlayerMoveSearcher(
                    _logger,
                    board,
                    plyDepth,
                    boardHelper,
                    _transpositionTable,
                    variationLineCache,
                    gameControlInfo,
                    _useMultipleProcessors,
                    moveHistoryStatistics);

                var bestVariationLine = moveChooser.GetBestMove();
                totalNodeCount += moveChooser.NodeCount;
                variationLineCache = moveChooser.VariationLineCache;

                bestMoveContainer.Value = new BestMoveData(bestVariationLine, totalNodeCount, plyDepth);
                gameControlInfo.AllowMoveNow();

                var feedbackEventArgs = new ChessPlayerFeedbackEventArgs(
                    Side,
                    board,
                    plyDepth,
                    MaxPlyDepth,
                    bestVariationLine);

                OnFeedbackProvided(feedbackEventArgs);

                //// ReSharper disable once InvertIf
                if (bestVariationLine.Value.IsCheckmating())
                {
                    _logger.Info($@"[{currentMethodName}] Forced checkmate found: {bestVariationLine}.");
                    break;
                }
            }
        }

        private sealed class BestMoveData
        {
            internal BestMoveData(VariationLine variation, long nodeCount, int plyDepth)
            {
                Variation = variation.EnsureNotNull();
                NodeCount = nodeCount;
                PlyDepth = plyDepth;
            }

            public int PlyDepth { get; }

            public VariationLine Variation { get; }

            public long NodeCount { get; }

            public override string ToString()
            {
                return this.ToPropertyString();
            }
        }
    }
}