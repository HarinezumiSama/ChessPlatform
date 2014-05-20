using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers
{
    public sealed class SmartEnoughPlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private const int MateScoreAbs = int.MaxValue / 2;

        private static readonly Dictionary<PieceType, int> PieceTypeToMaterialWeightMap =
            new Dictionary<PieceType, int>
            {
                { PieceType.King, 20000 },
                { PieceType.Queen, 900 },
                { PieceType.Rook, 500 },
                { PieceType.Bishop, 300 },
                { PieceType.Knight, 250 },
                { PieceType.Pawn, 100 },
                { PieceType.None, 0 }
            };

        private static readonly Dictionary<PieceType, int> PieceTypeToMobilityWeightMap =
            new Dictionary<PieceType, int>
            {
                { PieceType.King, 5 },
                { PieceType.Queen, 20 },
                { PieceType.Rook, 15 },
                { PieceType.Bishop, 10 },
                { PieceType.Knight, 10 },
                { PieceType.Pawn, 5 }
            };

        private readonly int _moveDepth;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        public SmartEnoughPlayer(PieceColor color, int moveDepth)
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
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Trace.TraceInformation("[{0}] Analyzing \"{1}\"...", currentMethodName, board.GetFen());

            var stopwatch = Stopwatch.StartNew();
            var result = DoGetMoveInternal(board, cancellationToken);
            stopwatch.Stop();

            Trace.TraceInformation(
                "[{0}] Result: {1}, {2} spent, for \"{3}\".",
                currentMethodName,
                result,
                stopwatch.Elapsed,
                board.GetFen());

            return result.EnsureNotNull();
        }

        #endregion

        #region Private Methods

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static PieceMove[] OrderMoves([NotNull] IGameBoard board)
        {
            return board
                .ValidMoves
                .OrderByDescending(move => PieceTypeToMaterialWeightMap[board[move.To].GetPieceType()])
                .ThenBy(move => move.ToString())
                .ToArray();
        }

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
                var weight = PieceTypeToMaterialWeightMap[pieceType];
                result += advantage * weight;
            }

            return result;
        }

        private static int EvaluateMobility([NotNull] IGameBoard board)
        {
            var result = board
                .ValidMoves
                .GroupBy(
                    move => board[move.From].GetPieceType(),
                    move => 1,
                    (pieceType, moves) => moves.Sum() * PieceTypeToMobilityWeightMap[pieceType])
                .Sum();

            return result;
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

        private static int ComputeAlphaBeta(
            [NotNull] IGameBoard board,
            int plyDepth,
            int alpha,
            int beta,
            CancellationToken cancellationToken)
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

            cancellationToken.ThrowIfCancellationRequested();

            if (plyDepth == 0 || board.ValidMoves.Count == 0)
            {
                return EvaluatePosition(board);
            }

            var orderedMoves = OrderMoves(board);
            foreach (var move in orderedMoves)
            {
                var currentBoard = board.MakeMove(move);
                var score = -ComputeAlphaBeta(currentBoard, plyDepth - 1, -beta, -alpha, cancellationToken);

                if (score >= beta)
                {
                    // Fail-hard beta-cutoff
                    return beta;
                }

                if (score > alpha)
                {
                    alpha = score;
                }
            }

            return alpha;
        }

        private static PieceMove FindMateMove([NotNull] IGameBoard board, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mateMoves = board
                .ValidMoves
                .AsParallel()
                .WithCancellation(cancellationToken)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Where(
                    move =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var currentBoard = board.MakeMove(move);
                        return currentBoard.State == GameState.Checkmate;
                    })
                .ToArray();

            return mateMoves.FirstOrDefault();
        }

        private PieceMove DoGetMoveInternal(IGameBoard board, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (board.ValidMoves.Count == 1)
            {
                return board.ValidMoves.Single();
            }

            var mateMove = FindMateMove(board, cancellationToken);
            if (mateMove != null)
            {
                return mateMove;
            }

            var result = ComputeAlphaBetaRoot(board, cancellationToken);
            return result.EnsureNotNull();
        }

        private PieceMove ComputeAlphaBetaRoot(IGameBoard board, CancellationToken cancellationToken)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();
            var plyDepth = _moveDepth * 2;

            var orderedMoves = OrderMoves(board);

            var alpha = int.MinValue / 2;
            PieceMove result = null;
            foreach (var move in orderedMoves)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentBoard = board.MakeMove(move);

                var score = -ComputeAlphaBeta(
                    currentBoard,
                    plyDepth - 1,
                    int.MinValue / 2,
                    -alpha,
                    cancellationToken);

                Trace.TraceInformation("[{0}] Move {1}: {2}", currentMethodName, move, score);

                if (score > alpha)
                {
                    alpha = score;
                    result = move;
                }
            }

            return result;
        }

        #endregion
    }
}