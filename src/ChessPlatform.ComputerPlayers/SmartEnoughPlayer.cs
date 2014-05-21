﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers
{
    public sealed class SmartEnoughPlayer : ChessPlayerBase
    {
        #region Constants and Fields

        private const int MinimumMaxPlyDepth = 2;
        private const int MateScoreAbs = int.MaxValue / 2;

        private static readonly Dictionary<PieceType, int> PieceTypeToMaterialWeightMap =
            CreatePieceTypeToMaterialWeightMap();

        // ReSharper disable once UnusedMember.Local
        private static readonly Dictionary<PieceType, int> PieceTypeToMobilityWeightMap =
            CreatePieceTypeToMobilityWeightMap();

        private static readonly Dictionary<Piece, Dictionary<Position, int>> PieceToPositionWeightMap =
            CreatePieceToPositionWeightMap();

        private readonly int _maxPlyDepth;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChessPlayerBase"/> class.
        /// </summary>
        public SmartEnoughPlayer(PieceColor color, int maxPlyDepth)
            : base(color)
        {
            #region Argument Check

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

            _maxPlyDepth = maxPlyDepth;
        }

        #endregion

        #region Protected Methods

        protected override PieceMove DoGetMove(IGameBoard board, CancellationToken cancellationToken)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            Trace.TraceInformation("[{0}] Analyzing \"{1}\"...", currentMethodName, board.GetFen());

            var transpositionTable = new SimpleTranspositionTable();

            var stopwatch = Stopwatch.StartNew();
            var result = DoGetMoveInternal(board, cancellationToken, transpositionTable);
            stopwatch.Stop();

            Trace.TraceInformation(
                "[{0}] Result: {1}, {2} spent, TT {{hits {3}, misses {4}, size {5}}}, for \"{6}\".",
                currentMethodName,
                result,
                stopwatch.Elapsed,
                transpositionTable.HitCount,
                transpositionTable.MissCount,
                transpositionTable.ItemCount,
                board.GetFen());

            return result.EnsureNotNull();
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
                { PieceType.King, 10 },
                { PieceType.Queen, 10 },
                { PieceType.Rook, 10 },
                { PieceType.Bishop, 10 },
                { PieceType.Knight, 10 },
                { PieceType.Pawn, 10 }
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
                throw new ArgumentException("Invalid array length.", "weights");
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

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static PieceMove[] OrderMoves([NotNull] IGameBoard board)
        {
            return board
                .ValidMoves
                .OrderByDescending(move => PieceTypeToMaterialWeightMap[board[move.To].GetPieceType()])
                .ThenBy(move => move.ToString())
                .ToArray();
        }

        private static int EvaluateMaterialAndItsPositionByColor([NotNull] IGameBoard board, PieceColor color)
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

                int materialScore = 0;
                if (pieceType != PieceType.King)
                {
                    var materialWeight = PieceTypeToMaterialWeightMap[pieceType];
                    materialScore = piecePositions.Length * materialWeight;
                }

                var positionWeightMap = PieceToPositionWeightMap[piece];
                var positionScore = piecePositions.Sum(position => positionWeightMap[position]);

                result += materialScore + positionScore;
            }

            return result;
        }

        private static int EvaluateMaterialAndItsPosition([NotNull] IGameBoard board)
        {
            var activeScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor);
            var inactiveScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor.Invert());

            return activeScore - inactiveScore;
        }

        // ReSharper disable once UnusedParameter.Local
        private static int EvaluateMobility([NotNull] IGameBoard board)
        {
            //// TODO [vmcl] Think on how determine mobility for inactive player
            return 0;

            ////var result = board
            ////    .ValidMoves
            ////    .GroupBy(
            ////        move => board[move.From].GetPieceType(),
            ////        move => 1,
            ////        (pieceType, moves) => moves.Sum() * PieceTypeToMobilityWeightMap[pieceType])
            ////    .Sum();

            ////return result;
        }

        private static int EvaluatePositionScore([NotNull] IGameBoard board)
        {
            switch (board.State)
            {
                case GameState.Checkmate:
                    return -MateScoreAbs;

                case GameState.Stalemate:
                    return 0;
            }

            var materialAndItsPosition = EvaluateMaterialAndItsPosition(board);
            var mobility = EvaluateMobility(board);

            var result = materialAndItsPosition + mobility;
            return result;
        }

        private static int ComputeAlphaBeta(
            [NotNull] IGameBoard board,
            int plyDepth,
            int alpha,
            int beta,
            CancellationToken cancellationToken,
            SimpleTranspositionTable transpositionTable)
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

            var cachedScore = transpositionTable.GetScore(board, plyDepth);
            if (cachedScore.HasValue)
            {
                return cachedScore.Value;
            }

            if (plyDepth == 0 || board.ValidMoves.Count == 0)
            {
                var result = EvaluatePositionScore(board);
                transpositionTable.SaveScore(board, plyDepth, result);
                return result;
            }

            var orderedMoves = OrderMoves(board);
            foreach (var move in orderedMoves)
            {
                var currentBoard = board.MakeMove(move);

                var score = -ComputeAlphaBeta(
                    currentBoard,
                    plyDepth - 1,
                    -beta,
                    -alpha,
                    cancellationToken,
                    transpositionTable);

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

            transpositionTable.SaveScore(board, plyDepth, alpha);
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

        private PieceMove DoGetMoveInternal(
            IGameBoard board,
            CancellationToken cancellationToken,
            SimpleTranspositionTable transpositionTable)
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

            var result = ComputeAlphaBetaRoot(board, cancellationToken, transpositionTable);
            return result.EnsureNotNull();
        }

        private PieceMove ComputeAlphaBetaRoot(
            IGameBoard board,
            CancellationToken cancellationToken,
            [NotNull] SimpleTranspositionTable transpositionTable)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();
            var plyDepth = _maxPlyDepth;

            var orderedMoves = OrderMoves(board);

            const int RootAlpha = checked(-MateScoreAbs - 1);
            const int RootBeta = checked(MateScoreAbs + 1);

            var bestMoves = new Dictionary<PieceMove, int>(orderedMoves.Length);
            var alpha = RootAlpha;

            foreach (var move in orderedMoves)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentBoard = board.MakeMove(move);
                var localScore = -EvaluatePositionScore(currentBoard);

                var score = -ComputeAlphaBeta(
                    currentBoard,
                    plyDepth - 1,
                    -RootBeta,
                    -alpha,
                    cancellationToken,
                    transpositionTable);

                Trace.TraceInformation("[{0}] Move {1}: {2} (local {3})", currentMethodName, move, score, localScore);

                if (score > alpha)
                {
                    alpha = score;

                    bestMoves.Clear();
                    bestMoves.Add(move, localScore);
                }
                else if (score == alpha)
                {
                    bestMoves.Add(move, localScore);
                }
            }

            var orderedBestMoves = bestMoves
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key.From.X88Value)
                .ThenBy(pair => pair.Key.To.X88Value)
                .ThenBy(pair => pair.Key.PromotionResult)
                .ToArray();

            var resultPair = orderedBestMoves.First();

            Trace.TraceInformation(
                "[{0}] Best move {1}: {2} (local {3})",
                currentMethodName,
                resultPair.Key,
                alpha,
                resultPair.Value);

            return resultPair.Key;
        }

        #endregion

        #region SimpleTranspositionTable Class

        private sealed class SimpleTranspositionTable
        {
            #region Constants and Fields

            private readonly Dictionary<string, int> _fenToScoreMap = new Dictionary<string, int>();

            #endregion

            #region Public Properties

            public ulong HitCount
            {
                get;
                private set;
            }

            public ulong MissCount
            {
                get;
                private set;
            }

            public int ItemCount
            {
                [DebuggerNonUserCode]
                get
                {
                    return _fenToScoreMap.Count;
                }
            }

            #endregion

            #region Public Methods

            public int? GetScore([NotNull] IGameBoard board, int plyDepth)
            {
                #region Argument Check

                if (board == null)
                {
                    throw new ArgumentNullException("board");
                }

                #endregion

                var key = GetKey(board, plyDepth);

                int result;
                if (!_fenToScoreMap.TryGetValue(key, out result))
                {
                    this.MissCount++;
                    return null;
                }

                this.HitCount++;
                return result;
            }

            public void SaveScore([NotNull] IGameBoard board, int plyDepth, int score)
            {
                #region Argument Check

                if (board == null)
                {
                    throw new ArgumentNullException("board");
                }

                #endregion

                var key = GetKey(board, plyDepth);
                _fenToScoreMap.Add(key, score);
            }

            #endregion

            #region Private Methods

            private static string GetKey([NotNull] IGameBoard board, int plyDepth)
            {
                var fen = board.GetFen();
                return fen + "|" + plyDepth.ToString(CultureInfo.InvariantCulture);
            }

            #endregion
        }

        #endregion
    }
}