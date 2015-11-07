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

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    public sealed class SmartEnoughPlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private static readonly string PlayerName = string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}",
            typeof(ChessPlayerBase).Assembly.GetName().Name,
            typeof(SmartEnoughPlayer).GetQualifiedName());

        private static readonly string TraceSeparator = new string('-', 120);

        private readonly Random _openingBookRandom;
        private readonly int _maxPlyDepth;
        private readonly IOpeningBook _openingBook;
        private readonly TimeSpan? _maxTimePerMove;
        private readonly bool _useMultipleProcessors;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        public SmartEnoughPlayer(PieceColor color, [NotNull] SmartEnoughPlayerParameters parameters)
            : base(color)
        {
            #region Argument Check

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.MaxPlyDepth < EngineConstants.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(parameters.MaxPlyDepth),
                    parameters.MaxPlyDepth,
                    $"The value must be at least {EngineConstants.MaxPlyDepthLowerLimit}.");
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

        protected override PrincipalVariationInfo DoGetMove(GetMoveRequest request)
        {
            request.CancellationToken.ThrowIfCancellationRequested();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();
            var board = request.Board;

            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(TraceSeparator);

            Trace.TraceInformation(
                $@"[{currentMethodName} :: {LocalHelper.GetTimestamp()}] Color: {Color}, max depth: {
                    _maxPlyDepth} plies, max time: {_maxTimePerMove?.ToString("g") ?? "unlimited"}, multi CPU: {
                    _useMultipleProcessors}, FEN: ""{board.GetFen()}"".");

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
                }

                stopwatch = new Stopwatch();

                var task = new Task(
                    () => DoGetMoveInternal(request.Board, internalCancellationToken, bestMoveContainer),
                    internalCancellationToken);

                if (maxMoveTime.HasValue)
                {
                    timeoutCancellationTokenSource.CancelAfter(maxMoveTime.Value);
                }

                stopwatch.Start();
                task.Start();
                try
                {
                    task.Wait(timeoutCancellationToken);
                }
                catch (AggregateException ex)
                {
                    var operationCanceledException = ex.InnerException as OperationCanceledException;
                    if (operationCanceledException != null)
                    {
                        if (operationCanceledException.CancellationToken == internalCancellationToken
                            || operationCanceledException.CancellationToken == request.CancellationToken)
                        {
                            request.CancellationToken.ThrowIfCancellationRequested();
                        }
                    }

                    throw;
                }
                catch (OperationCanceledException ex)
                {
                    if (ex.CancellationToken != timeoutCancellationToken)
                    {
                        throw;
                    }
                }

                stopwatch.Stop();
            }
            finally
            {
                timeoutCancellationTokenSource.DisposeSafely();
            }

            var bestMoveData = bestMoveContainer.Value;
            var principalVariationInfo = bestMoveData?.PrincipalVariation;

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var nodeCount = bestMoveData?.NodeCount ?? 0L;

            var nps = elapsedSeconds.IsZero()
                ? "?"
                : Convert.ToInt64(nodeCount / elapsedSeconds).ToString(CultureInfo.InvariantCulture);

            Trace.TraceInformation(
                $@"[{currentMethodName} :: {LocalHelper.GetTimestamp()}] Result: {
                    principalVariationInfo?.ToStandardAlgebraicNotationString(request.Board) ?? "<not found>"
                    }, depth {bestMoveData?.PlyDepth.ToString(CultureInfo.InvariantCulture) ?? "?"}, time: {
                    stopwatch.Elapsed:g}, {nodeCount} nodes ({nps} NPS), FEN ""{board.GetFen()}"".");

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
            CancellationToken cancellationToken,
            SyncValueContainer<BestMoveData> bestMoveContainer)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Trace.TraceInformation($@"[{currentMethodName}] Number of available moves: {board.ValidMoves.Count}.");

            if (board.ValidMoves.Count == 1)
            {
                var onlyMove = board.ValidMoves.Keys.Single();
                bestMoveContainer.Value = new BestMoveData(onlyMove | PrincipalVariationInfo.Zero, 0L, 1);
                return;
            }

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

                    Trace.TraceInformation(
                        $@"[{currentMethodName}] From the opening moves [ {openingMovesString} ] chosen {
                            openingMoveString}. Further opening move variants: {furtherOpeningMovesString}.");

                    bestMoveContainer.Value = new BestMoveData(openingMove | PrincipalVariationInfo.Zero, 1L, 1);
                    return;
                }
            }

            var boardHelper = new BoardHelper();

            PrincipalVariationInfo bestPrincipalVariationInfo = null;
            var totalNodeCount = 0L;
            PrincipalVariationCache principalVariationCache = null;

            var useIterativeDeepening = _maxTimePerMove.HasValue;

            var startingPlyDepth = useIterativeDeepening
                ? EngineConstants.MaxPlyDepthLowerLimit
                : _maxPlyDepth;

            for (var plyDepth = startingPlyDepth;
                plyDepth <= _maxPlyDepth;
                plyDepth++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Trace.WriteLine(string.Empty);

                if (useIterativeDeepening)
                {
                    Trace.TraceInformation(
                        $@"[{currentMethodName} :: {LocalHelper.GetTimestamp()}] Iterative deepening: {plyDepth} of {
                            _maxPlyDepth}.");
                }
                else
                {
                    Trace.TraceInformation(
                        $@"[{currentMethodName} :: {LocalHelper.GetTimestamp()}] Fixed depth: {plyDepth}.");
                }

                boardHelper.ResetLocalMoveCount();

                var moveChooser = new SmartEnoughPlayerMoveChooser(
                    board,
                    plyDepth,
                    boardHelper,
                    principalVariationCache,
                    bestPrincipalVariationInfo,
                    cancellationToken,
                    _useMultipleProcessors);

                bestPrincipalVariationInfo = moveChooser.GetBestMove();
                totalNodeCount += moveChooser.NodeCount;
                principalVariationCache = moveChooser.PrincipalVariationCache;

                bestMoveContainer.Value = new BestMoveData(bestPrincipalVariationInfo, totalNodeCount, plyDepth);

                if (bestPrincipalVariationInfo.IsCheckmating())
                {
                    Trace.TraceInformation(
                        $@"[{currentMethodName} :: {LocalHelper.GetTimestamp()}] Forced checkmate found: {
                            bestPrincipalVariationInfo}.");

                    break;
                }
            }
        }

        #endregion

        #region BestMoveData Class

        private sealed class BestMoveData
        {
            #region Constructors

            internal BestMoveData(PrincipalVariationInfo principalVariation, long nodeCount, int plyDepth)
            {
                PrincipalVariation = principalVariation.EnsureNotNull();
                NodeCount = nodeCount;
                PlyDepth = plyDepth;
            }

            #endregion

            #region Public Properties

            public int PlyDepth
            {
                get;
            }

            public PrincipalVariationInfo PrincipalVariation
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