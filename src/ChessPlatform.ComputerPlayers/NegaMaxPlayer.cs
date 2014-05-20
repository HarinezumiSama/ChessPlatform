using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers
{
    public sealed class NegaMaxPlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private const int MateScoreAbs = int.MaxValue / 2;

        private static readonly Dictionary<PieceType, int> PieceTypeToWeightMap = new Dictionary<PieceType, int>
        {
            { PieceType.King, 20000 },
            { PieceType.Queen, 900 },
            { PieceType.Rook, 500 },
            { PieceType.Bishop, 300 },
            { PieceType.Knight, 250 },
            { PieceType.Pawn, 100 },
        };

        private readonly int _moveDepth;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        public NegaMaxPlayer(PieceColor color, int moveDepth)
            : base(color)
        {
            #region Argument Check

            if (moveDepth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "moveDepth",
                    moveDepth,
                    @"The value must be positive.");
            }

            #endregion

            _moveDepth = moveDepth;
        }

        #endregion

        #region Protected Methods

        protected override PieceMove DoGetMove(IGameBoard board, CancellationToken cancellationToken)
        {
            if (board.ValidMoves.Count == 0)
            {
                throw new ArgumentException("There are no valid moves.", "board");
            }

            if (board.ValidMoves.Count == 1)
            {
                return board.ValidMoves.ToArray().Single();
            }

            var stopwatch = Stopwatch.StartNew();
            var result = NegaMaxRoot(board, cancellationToken);
            stopwatch.Stop();

            Trace.TraceInformation(
                "[{0}] Result: {1}, {2} spent, for \"{3}\".",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                result,
                stopwatch.Elapsed,
                board.GetFen());

            return result.EnsureNotNull();
        }

        #endregion

        #region Private Methods

        private static int EvaluateMaterial([NotNull] IGameBoard board)
        {
            var result = 0;

            var activeColor = board.ActiveColor;
            var inactiveColor = activeColor.Invert();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var pieceType in ChessConstants.PieceTypesExceptNone)
            {
                var activePiece = pieceType.ToPiece(activeColor);
                var inactivePiece = pieceType.ToPiece(inactiveColor);
                var advantage = board.GetPieceCount(activePiece) - board.GetPieceCount(inactivePiece);
                var weight = PieceTypeToWeightMap[pieceType];
                result += advantage * weight;
            }

            return result;
        }

        private static int EvaluateMobility([NotNull] IGameBoard board)
        {
            return board.ValidMoves.Count * 10;
        }

        private static int EvaluatePosition([NotNull] IGameBoard board)
        {
            switch (board.State)
            {
                case GameState.Checkmate:
                    return -MateScoreAbs;

                case GameState.Stalemate:
                    return 0;
            }

            return EvaluateMaterial(board) + EvaluateMobility(board);
        }

        private int NegaMax(IGameBoard board, int plyDepth)
        {
            #region Argument Check

            if (plyDepth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "plyDepth",
                    plyDepth,
                    @"The value cannot be negative.");
            }

            #endregion

            if (plyDepth == 0 || board.ValidMoves.Count == 0)
            {
                return EvaluatePosition(board);
            }

            var result = int.MinValue;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var move in board.ValidMoves)
            {
                var currentBoard = board.MakeMove(move);
                var score = -NegaMax(currentBoard, plyDepth - 1);
                if (score > result)
                {
                    result = score;
                }
            }

            return result;
        }

        private PieceMove NegaMaxRoot(IGameBoard board, CancellationToken cancellationToken)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();
            var plyDepth = _moveDepth * 2;

            ////var evaluatedMoves = board
            ////    .ValidMoves
            ////    .Select(
            ////        move =>
            ////        {
            ////            cancellationToken.ThrowIfCancellationRequested();

            ////            var currentBoard = board.MakeMove(move);
            ////            var score = -NegaMax(currentBoard, plyDepth - 1);
            ////            var pair = KeyValuePair.Create(move, score);

            ////            Trace.TraceInformation("[{0}] Move {1}: {2}", currentMethodName, pair.Key, pair.Value);

            ////            return pair;
            ////        })
            ////    .ToArray();

            var evaluatedMoves = board
                .ValidMoves
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Select(
                    move =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var currentBoard = board.MakeMove(move);
                        var score = -NegaMax(currentBoard, plyDepth - 1);
                        var pair = KeyValuePair.Create(move, score);

                        Trace.TraceInformation("[{0}] Move {1}: {2}", currentMethodName, pair.Key, pair.Value);

                        return pair;
                    })
                .ToArray();

            var result = evaluatedMoves
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key.From.X88Value)
                .ThenBy(pair => pair.Key.To.X88Value)
                .ThenBy(pair => pair.Key.PromotionResult)
                .First()
                .Key;

            return result;
        }

        #endregion
    }
}