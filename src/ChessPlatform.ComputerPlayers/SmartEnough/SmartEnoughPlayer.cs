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
            typeof(SmartEnoughPlayer).Assembly.GetName().Name,
            typeof(SmartEnoughPlayer).GetQualifiedName());

        private static readonly string TraceSeparator = new string('-', 120);

        private readonly int _maxPlyDepth;
        private readonly OpeningBook _openingBook;
        private readonly Random _openingBookRandom;
        private readonly TimeSpan? _maxTimePerMove;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        public SmartEnoughPlayer(PieceColor color, int maxPlyDepth, bool useOpeningBook, TimeSpan? maxTimePerMove)
            : base(color)
        {
            #region Argument Check

            if (maxPlyDepth < SmartEnoughPlayerMoveChooser.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    "maxPlyDepth",
                    maxPlyDepth,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The value must be at least {0}.",
                        SmartEnoughPlayerMoveChooser.MaxPlyDepthLowerLimit));
            }

            if (maxTimePerMove.HasValue && maxTimePerMove.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    "maxTimePerMove",
                    maxTimePerMove,
                    @"The time per move must be positive.");
            }

            #endregion

            _maxPlyDepth = maxPlyDepth;
            _openingBook = useOpeningBook ? OpeningBook.Default : null;
            _openingBookRandom = new Random();
            _maxTimePerMove = maxTimePerMove;
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

        protected override GameMove DoGetMove(GetMoveRequest request)
        {
            request.CancellationToken.ThrowIfCancellationRequested();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();
            var board = request.Board;

            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(TraceSeparator);

            Trace.TraceInformation(
                "[{0}] Max ply depth: {1}. Analyzing \"{2}\"...",
                currentMethodName,
                _maxPlyDepth,
                board.GetFen());

            var bestMoveContainer = new SyncValueContainer<BestMoveData>();
            Stopwatch stopwatch;

            Timer timer = null;
            try
            {
                var internalCancellationToken = request.CancellationToken;
                var timeoutCancellationToken = CancellationToken.None;
                if (_maxTimePerMove.HasValue)
                {
                    var timeoutCancellationTokenSource = new CancellationTokenSource();
                    timeoutCancellationToken = timeoutCancellationTokenSource.Token;

                    var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        request.CancellationToken,
                        timeoutCancellationTokenSource.Token);

                    internalCancellationToken = linkedTokenSource.Token;

                    timer = new Timer(
                        state => timeoutCancellationTokenSource.Cancel(),
                        null,
                        _maxTimePerMove.Value,
                        TimeSpan.FromMilliseconds(Timeout.Infinite));
                }

                stopwatch = new Stopwatch();

                var task = new Task(
                    () => DoGetMoveInternal(request.Board, internalCancellationToken, bestMoveContainer),
                    internalCancellationToken);

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
                timer.DisposeSafely();
            }

            var bestMoveData = bestMoveContainer.Value;
            var bestMove = bestMoveData == null ? null : bestMoveData.Move;

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var nodeCount = bestMoveData == null ? 0L : bestMoveData.NodeCount;

            var nps = elapsedSeconds.IsZero()
                ? "?"
                : Convert.ToInt64(nodeCount / elapsedSeconds).ToString(CultureInfo.InvariantCulture);

            Trace.TraceInformation(
                @"[{0}] Result: {1}, {2} spent, depth {3}, {4} nodes ({5} NPS), position ""{6}"".",
                currentMethodName,
                bestMove == null ? "<not found>" : bestMove.ToString(),
                stopwatch.Elapsed,
                bestMoveData == null ? "?" : bestMoveData.PlyDepth.ToString(CultureInfo.InvariantCulture),
                nodeCount,
                nps,
                board.GetFen());

            Trace.WriteLine(TraceSeparator);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);

            if (bestMove == null)
            {
                throw new InvalidOperationException("Could not determine the best move. (Has timeout expired?)");
            }

            return bestMove;
        }

        #endregion

        #region Private Methods

        private void DoGetMoveInternal(
            [NotNull] IGameBoard board,
            CancellationToken cancellationToken,
            SyncValueContainer<BestMoveData> bestMoveContainer)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Trace.TraceInformation(
                "[{0}] Number of available moves: {1}.",
                currentMethodName,
                board.ValidMoves.Count);

            if (board.ValidMoves.Count == 1)
            {
                bestMoveContainer.Value = new BestMoveData(board.ValidMoves.Keys.Single(), 0L, 1);
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

                    Trace.TraceInformation(
                        "[{0}] From the opening moves [ {1} ]: chosen {2}. Further opening moves [ {3} ].",
                        currentMethodName,
                        openingMoves.Select(move => move.ToString()).Join(", "),
                        openingMove,
                        furtherOpeningMoves.Select(move => move.ToString()).Join(", "));

                    bestMoveContainer.Value = new BestMoveData(openingMove, 1L, 1);
                    return;
                }
            }

            var boardCache = new BoardCache(100000);

            BestMoveInfo bestMoveInfo = null;
            var totalNodeCount = 0L;

            for (var plyDepth = SmartEnoughPlayerMoveChooser.MaxPlyDepthLowerLimit;
                plyDepth <= _maxPlyDepth;
                plyDepth++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Trace.WriteLine(string.Empty);
                Trace.TraceInformation("[{0}] Iterative deepening: {1}.", currentMethodName, plyDepth);

                var moveChooser = new SmartEnoughPlayerMoveChooser(
                    board,
                    plyDepth,
                    boardCache,
                    bestMoveInfo,
                    cancellationToken);

                bestMoveInfo = moveChooser.GetBestMove();
                totalNodeCount += moveChooser.NodeCount;

                bestMoveContainer.Value = new BestMoveData(bestMoveInfo.BestMove, totalNodeCount, plyDepth);
            }
        }

        #endregion

        #region BestMoveData Class

        private sealed class BestMoveData
        {
            #region Constructors

            internal BestMoveData(GameMove move, long nodeCount, int plyDepth)
            {
                this.Move = move.EnsureNotNull();
                this.NodeCount = nodeCount;
                this.PlyDepth = plyDepth;
            }

            #endregion

            #region Public Properties

            public int PlyDepth
            {
                get;
                private set;
            }

            public GameMove Move
            {
                get;
                private set;
            }

            public long NodeCount
            {
                get;
                private set;
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