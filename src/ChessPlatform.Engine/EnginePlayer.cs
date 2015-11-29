using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChessPlatform.GamePlay;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public sealed class EnginePlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private static readonly string PlayerName = string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}",
            typeof(ChessPlayerBase).Assembly.GetName().Name,
            typeof(EnginePlayer).GetQualifiedName());

        private static readonly string TraceSeparator = new string('-', 120);

        private readonly Random _openingBookRandom;
        private readonly int _maxPlyDepth;
        private readonly IOpeningBook _openingBook;
        private readonly TimeSpan? _maxTimePerMove;
        private readonly bool _useMultipleProcessors;

        [CanBeNull]
        private readonly TranspositionTable _transpositionTable;

        #endregion

        #region Constructors

        public EnginePlayer(PieceColor color, [NotNull] EnginePlayerParameters parameters)
            : base(color)
        {
            #region Argument Check

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.MaxPlyDepth < CommonEngineConstants.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parameters.MaxPlyDepth),
                    parameters.MaxPlyDepth,
                    $"The value must be at least {CommonEngineConstants.MaxPlyDepthLowerLimit}.");
            }

            if (parameters.MaxTimePerMove.HasValue && parameters.MaxTimePerMove.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parameters.MaxTimePerMove),
                    parameters.MaxTimePerMove,
                    @"The time per move, if specified, must be positive.");
            }

            #endregion

            _openingBookRandom = new Random();
            _maxPlyDepth = parameters.MaxPlyDepth;
            _openingBook = parameters.UseOpeningBook ? PolyglotOpeningBook.Varied : null;
            _maxTimePerMove = parameters.MaxTimePerMove;
            _useMultipleProcessors = parameters.UseMultipleProcessors;

            _transpositionTable = parameters.UseTranspositionTable
                ? new TranspositionTable(parameters.TranspositionTableSizeInMegaBytes)
                : null;
        }

        #endregion

        #region Public Properties

        public override string Name
        {
            [DebuggerStepThrough]
            get
            {
                return PlayerName;
            }
        }

        public int MaxPlyDepth
        {
            [DebuggerStepThrough]
            get
            {
                return _maxPlyDepth;
            }
        }

        #endregion

        #region Protected Methods

        protected override VariationLine DoGetMove(GetMoveRequest request)
        {
            request.CancellationToken.ThrowIfCancellationRequested();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();
            var board = request.Board;

            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(TraceSeparator);

            var useOpeningBook = _openingBook != null;

            Trace.WriteLine(
                $@"{Environment.NewLine
                    }*** [{currentMethodName}] BEGIN: {LocalHelper.GetTimestamp()}{Environment.NewLine
                    }  Color: {Color}{Environment.NewLine
                    }  Max depth: {_maxPlyDepth} plies{Environment.NewLine
                    }  Max time: {_maxTimePerMove?.ToString("g") ?? "unlimited"}{Environment.NewLine
                    }  Multi CPU: {_useMultipleProcessors}{Environment.NewLine
                    }  Opening book: {useOpeningBook}{Environment.NewLine
                    }  FEN: {board.GetFen()}{Environment.NewLine}");

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
                    Trace.WriteLine($@"[{currentMethodName}] Adjusted max time for move: {maxMoveTime.Value}");
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
                    Trace.WriteLine(
                        $@"[{currentMethodName}] Interrupting the search since the Move Now request was received.");
                }
                catch (AggregateException ex)
                    when (ex.Flatten().InnerException is OperationCanceledException)
                {
                    var operationCanceledException = (OperationCanceledException)ex.Flatten().InnerException;
                    if (operationCanceledException.CancellationToken == internalCancellationToken
                        || operationCanceledException.CancellationToken == request.CancellationToken)
                    {
                        Trace.WriteLine($@"[{currentMethodName}] The search has been canceled by the caller.");
                        request.CancellationToken.ThrowIfCancellationRequested();
                    }

                    throw;
                }
                catch (OperationCanceledException ex)
                    when (ex.CancellationToken == timeoutCancellationToken)
                {
                    Trace.WriteLine(
                        $@"[{currentMethodName
                            }] Interrupting the search since maximum time per move has been reached.");
                }
                catch (OperationCanceledException)
                {
                    Trace.WriteLine($@"[{currentMethodName}] The search has been canceled by the caller.");
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
                : Convert.ToInt64(nodeCount / elapsedSeconds).ToString(CultureInfo.InvariantCulture);

            var principalVariationString = principalVariationInfo?.ToStandardAlgebraicNotationString(request.Board)
                ?? "<not found>";

            var depthString = bestMoveData?.PlyDepth.ToString(CultureInfo.InvariantCulture) ?? "?";

            Trace.WriteLine(
                $@"{Environment.NewLine
                    }*** [{currentMethodName}] END: {LocalHelper.GetTimestamp()}{Environment.NewLine
                    }  Result: {principalVariationString}{Environment.NewLine
                    }  Depth: {depthString}{Environment.NewLine
                    }  Time: {stopwatch.Elapsed:g}{Environment.NewLine
                    }  Nodes: {nodeCount}{Environment.NewLine
                    }  NPS: {nps}{Environment.NewLine
                    }  FEN: {board.GetFen()}{Environment.NewLine}");

            if (_transpositionTable != null)
            {
                var bucketCount = _transpositionTable.BucketCount;
                var probeCount = _transpositionTable.ProbeCount;
                var hitCount = _transpositionTable.HitCount;

                var hitRatio = probeCount == 0
                    ? "n/a"
                    : ((decimal)hitCount / probeCount * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";

                Trace.WriteLine(
                    $@"{Environment.NewLine}TT statistics:{Environment.NewLine
                        }  {nameof(TranspositionTable.BucketCount)}: {bucketCount}{Environment.NewLine
                        }  {nameof(TranspositionTable.ProbeCount)}: {probeCount}{Environment.NewLine
                        }  {nameof(TranspositionTable.HitCount)}: {hitCount}{Environment.NewLine
                        }  Hit Ratio: {hitRatio}{Environment.NewLine}");
            }

            Trace.WriteLine(TraceSeparator);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);

            if (principalVariationInfo == null)
            {
                throw new InvalidOperationException("Could not determine the best move. (Has timeout expired?)");
            }

            return principalVariationInfo;
        }

        #endregion

        #region Private Methods

        private void DoGetMoveInternal(
            [NotNull] GameBoard board,
            [NotNull] GameControlInfo gameControlInfo,
            SyncValueContainer<BestMoveData> bestMoveContainer)
        {
            gameControlInfo.CheckInterruptions();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Trace.WriteLine($@"[{currentMethodName}] Number of available moves: {board.ValidMoves.Count}.");

            if (_openingBook != null)
            {
                var openingMoves = _openingBook.FindPossibleMoves(board);
                if (openingMoves.Length != 0)
                {
                    var index = _openingBookRandom.Next(openingMoves.Length);
                    var openingMove = openingMoves[index];

                    var boardAfterOpeningMove = board.MakeMove(openingMove);
                    var furtherOpeningMoves = _openingBook.FindPossibleMoves(boardAfterOpeningMove);

                    var openingMovesString = openingMoves
                        .Select(move => move.ToStandardAlgebraicNotation(board))
                        .Join(", ");

                    var openingMoveString = openingMove.ToStandardAlgebraicNotation(board);

                    var furtherOpeningMovesString = furtherOpeningMoves.Length == 0
                        ? "n/a"
                        : furtherOpeningMoves
                            .Select(move => move.ToStandardAlgebraicNotation(boardAfterOpeningMove))
                            .Join(", ");

                    Trace.WriteLine(
                        $@"[{currentMethodName}] From the opening moves [ {openingMovesString} ] chosen {
                            openingMoveString}. Further opening move variants: {furtherOpeningMovesString}.");

                    bestMoveContainer.Value = new BestMoveData(openingMove | VariationLine.Zero, 1L, 1);
                    return;
                }
            }

            var boardHelper = new BoardHelper();
            var killerMoveStatistics = new KillerMoveStatistics();

            VariationLine bestVariationLine = null;
            var totalNodeCount = 0L;
            VariationLineCache variationLineCache = null;

            for (var plyDepth = CommonEngineConstants.MaxPlyDepthLowerLimit;
                plyDepth <= _maxPlyDepth;
                plyDepth++)
            {
                gameControlInfo.CheckInterruptions();

                Trace.WriteLine(
                    $@"{Environment.NewLine}[{currentMethodName} :: {LocalHelper.GetTimestamp()
                        }] Iterative deepening: {plyDepth} of {_maxPlyDepth}.");

                boardHelper.ResetLocalMoveCount();

                var moveChooser = new EnginePlayerMoveSearcher(
                    board,
                    plyDepth,
                    boardHelper,
                    _transpositionTable,
                    variationLineCache,
                    bestVariationLine,
                    gameControlInfo,
                    _useMultipleProcessors,
                    killerMoveStatistics);

                bestVariationLine = moveChooser.GetBestMove();
                totalNodeCount += moveChooser.NodeCount;
                variationLineCache = moveChooser.VariationLineCache;

                bestMoveContainer.Value = new BestMoveData(bestVariationLine, totalNodeCount, plyDepth);
                gameControlInfo.AllowMoveNow();

                var feedbackEventArgs = new ChessPlayerFeedbackEventArgs(
                    Color,
                    board,
                    plyDepth,
                    _maxPlyDepth,
                    bestVariationLine);

                OnFeedbackProvided(feedbackEventArgs);

                if (bestVariationLine.Value.IsCheckmating())
                {
                    Trace.WriteLine(
                        $@"[{currentMethodName} :: {LocalHelper.GetTimestamp()}] Forced checkmate found: {
                            bestVariationLine}.");

                    break;
                }
            }
        }

        #endregion

        #region BestMoveData Class

        private sealed class BestMoveData
        {
            #region Constructors

            internal BestMoveData(VariationLine variation, long nodeCount, int plyDepth)
            {
                Variation = variation.EnsureNotNull();
                NodeCount = nodeCount;
                PlyDepth = plyDepth;
            }

            #endregion

            #region Public Properties

            public int PlyDepth
            {
                get;
            }

            public VariationLine Variation
            {
                get;
            }

            public long NodeCount
            {
                get;
            }

            #endregion

            #region Public Methods

            public override string ToString()
            {
                return this.ToPropertyString();
            }

            #endregion
        }

        #endregion
    }
}