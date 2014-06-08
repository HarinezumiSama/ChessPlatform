using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ChessPlatform.ComputerPlayers.Properties;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers
{
    public sealed class SmartEnoughPlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private static readonly Lazy<OpeningBook> GlobalOpeningBook = Lazy.Create(
            InitializeGlobalOpeningBook,
            LazyThreadSafetyMode.ExecutionAndPublication);

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
            _openingBook = useOpeningBook ? GlobalOpeningBook.Value : null;
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

        protected override PieceMove DoGetMove(IGameBoard board, CancellationToken cancellationToken)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Trace.TraceInformation(
                "[{0}] Max ply depth: {1}. Analyzing \"{2}\"...",
                currentMethodName,
                _maxPlyDepth,
                board.GetFen());

            var stopwatch = Stopwatch.StartNew();
            var result = DoGetMoveInternal(board, cancellationToken);
            stopwatch.Stop();

            Trace.TraceInformation(
                @"[{0}] Result: {1}, {2} spent, for ""{3}"".",
                currentMethodName,
                result,
                stopwatch.Elapsed,
                board.GetFen());

            return result.EnsureNotNull();
        }

        #endregion

        #region Private Methods

        private static OpeningBook InitializeGlobalOpeningBook()
        {
            OpeningBook openingBook;

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Trace.TraceInformation("[{0}] Initializing the opening book...", currentMethodName);

            var stopwatch = Stopwatch.StartNew();
            using (var reader = new StringReader(Resources.OpeningBook))
            {
                openingBook = new OpeningBook(reader);
            }

            stopwatch.Stop();

            Trace.TraceInformation(
                "[{0}] The opening book has been initialized in {1}.",
                currentMethodName,
                stopwatch.Elapsed);

            return openingBook;
        }

        private PieceMove DoGetMoveInternal([NotNull] IGameBoard board, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (board.ValidMoves.Count == 1)
            {
                return board.ValidMoves.Keys.Single();
            }

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            if (_openingBook != null)
            {
                var openingMoves = _openingBook.FindPossibleMoves(board);
                if (openingMoves.Length != 0)
                {
                    var index = _openingBookRandom.Next(openingMoves.Length);
                    var openingMove = openingMoves[index];

                    Trace.TraceInformation(
                        "[{0}] From the opening move(s): {1}, chosen {2}.",
                        currentMethodName,
                        openingMoves.Select(move => move.ToString()).Join(", "),
                        openingMove);

                    return openingMove;
                }
            }

            PieceMove bestMove = null;

            for (var plyDepth = SmartEnoughPlayerMoveChooser.MinimumMaxPlyDepth;
                plyDepth <= _maxPlyDepth;
                plyDepth++)
            {
                Trace.TraceInformation("[{0}] Iterative deepening: {1}.", currentMethodName, plyDepth);

                var moveChooser = new SmartEnoughPlayerMoveChooser(
                    board,
                    plyDepth,
                    bestMove,
                    cancellationToken);

                bestMove = moveChooser.GetBestMove();
            }

            return bestMove.EnsureNotNull();
        }

        #endregion
    }
}