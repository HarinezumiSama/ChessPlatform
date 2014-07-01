using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    public sealed class SmartEnoughPlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private readonly int _maxPlyDepth;

        private readonly OpeningBook _openingBook;
        private readonly Random _openingBookRandom = new Random();

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        public SmartEnoughPlayer(PieceColor color, int maxPlyDepth, bool useOpeningBook)
            : base(color)
        {
            #region Argument Check

            if (maxPlyDepth < SmartEnoughPlayerMoveChooser.MinimumMaxPlyDepth)
            {
                throw new ArgumentOutOfRangeException(
                    "maxPlyDepth",
                    maxPlyDepth,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The value must be at least {0}.",
                        SmartEnoughPlayerMoveChooser.MinimumMaxPlyDepth));
            }

            #endregion

            _maxPlyDepth = maxPlyDepth;
            _openingBook = useOpeningBook ? OpeningBook.Default : null;
        }

        #endregion

        #region Public Properties

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

        protected override PieceMove DoGetMove(GetMoveRequest request)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var board = request.Board;

            Trace.TraceInformation(
                "[{0}] Max ply depth: {1}. Analyzing \"{2}\"...",
                currentMethodName,
                _maxPlyDepth,
                board.GetFen());

            var stopwatch = Stopwatch.StartNew();
            var bestMove = DoGetMoveInternal(request).EnsureNotNull();
            stopwatch.Stop();

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var nodeCount = bestMove.Item2;

            var nps = elapsedSeconds.IsZero()
                ? "?"
                : Convert.ToInt64(nodeCount / elapsedSeconds).ToString(CultureInfo.InvariantCulture);

            Trace.TraceInformation(
                @"[{0}] Result: {1}, {2} spent, {3} nodes ({4} NPS), for ""{5}"".",
                currentMethodName,
                bestMove.Item1,
                stopwatch.Elapsed,
                nodeCount,
                nps,
                board.GetFen());

            return bestMove.Item1;
        }

        #endregion

        #region Private Methods

        private Tuple<PieceMove, long> DoGetMoveInternal([NotNull] GetMoveRequest request)
        {
            var board = request.Board;
            var cancellationToken = request.CancellationToken;

            cancellationToken.ThrowIfCancellationRequested();

            if (board.ValidMoves.Count == 1)
            {
                return Tuple.Create(board.ValidMoves.Keys.Single(), 0L);
            }

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

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

                    return Tuple.Create(openingMove, 0L);
                }
            }

            var boardCache = new BoardCache(100000);

            BestMoveInfo bestMoveInfo = null;
            var totalNodeCount = 0L;

            for (var plyDepth = SmartEnoughPlayerMoveChooser.MinimumMaxPlyDepth;
                plyDepth <= _maxPlyDepth;
                plyDepth++)
            {
                Trace.TraceInformation("[{0}] Iterative deepening: {1}.", currentMethodName, plyDepth);

                var moveChooser = new SmartEnoughPlayerMoveChooser(
                    board,
                    plyDepth,
                    boardCache,
                    bestMoveInfo,
                    cancellationToken);

                bestMoveInfo = moveChooser.GetBestMove();

                totalNodeCount += moveChooser.NodeCount;
            }

            var bestMove = bestMoveInfo.EnsureNotNull().BestMove;
            return Tuple.Create(bestMove, totalNodeCount);
        }

        #endregion
    }
}