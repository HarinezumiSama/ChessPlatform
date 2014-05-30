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

        private const int MinimumMaxPlyDepth = 2;
        private const int MateScoreAbs = int.MaxValue / 2;

        private static readonly Dictionary<PieceType, int> PieceTypeToMaterialWeightMap =
            CreatePieceTypeToMaterialWeightMap();

        // ReSharper disable once UnusedMember.Local
        private static readonly Dictionary<PieceType, int> PieceTypeToMobilityWeightMap =
            CreatePieceTypeToMobilityWeightMap();

        private static readonly Dictionary<Piece, Dictionary<Position, int>> PieceToPositionWeightMap =
            CreatePieceToPositionWeightMap();

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

            var transpositionTable = new SimpleTranspositionTable(10000000);

            var stopwatch = Stopwatch.StartNew();
            var result = DoGetMoveInternal(board, cancellationToken, transpositionTable);
            stopwatch.Stop();

            Trace.TraceInformation(
                @"[{0}] Result: {1}, {2} spent, TT {{hits {3}, misses {4}, size {5}/{6}}}, for ""{7}"".",
                currentMethodName,
                result,
                stopwatch.Elapsed,
                transpositionTable.HitCount,
                transpositionTable.MissCount,
                transpositionTable.ItemCount,
                transpositionTable.MaximumItemCount,
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
                throw new ArgumentException(@"Invalid array length.", "weights");
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
            var capturingMoves = board
                .ValidMoves
                .Where(pair => pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderByDescending(move => PieceTypeToMaterialWeightMap[board[move.To].GetPieceType()])
                .ThenBy(move => PieceTypeToMaterialWeightMap[board[move.From].GetPieceType()])
                .ThenByDescending(move => PieceTypeToMaterialWeightMap[move.PromotionResult])
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            var nonCapturingMoves = board
                .ValidMoves
                .Where(pair => !pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderByDescending(move => PieceTypeToMaterialWeightMap[board[move.From].GetPieceType()])
                .ThenByDescending(move => PieceTypeToMaterialWeightMap[move.PromotionResult])
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            var result = capturingMoves.Concat(nonCapturingMoves).ToArray();
            return result;
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

                var materialScore = 0;
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

            //// TODO [vmcl] Ideally this method has to search for a guaranteed mate in a number of moves (rather than in mate-in-one only)

            var mateMoves = board
                .ValidMoves
                .Keys
                .Where(
                    move =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var currentBoard = board.MakeMove(move);
                        return currentBoard.State == GameState.Checkmate;
                    })
                .ToArray();

            if (mateMoves.Length == 0)
            {
                return null;
            }

            return mateMoves
                .OrderBy(move => PieceTypeToMaterialWeightMap[board[move.From].GetPieceType()])
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ThenBy(move => move.PromotionResult)
                .First();
        }

        private PieceMove DoGetMoveInternal(
            IGameBoard board,
            CancellationToken cancellationToken,
            SimpleTranspositionTable transpositionTable)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (board.ValidMoves.Count == 1)
            {
                return board.ValidMoves.Keys.Single();
            }

            var mateMove = FindMateMove(board, cancellationToken);
            if (mateMove != null)
            {
                return mateMove;
            }

            if (_openingBook != null)
            {
                var openingMoves = _openingBook.FindPossibleMoves(board);
                if (openingMoves.Length != 0)
                {
                    var index = _openingBookRandom.Next(openingMoves.Length);
                    var openingMove = openingMoves[index];
                    var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

                    Trace.TraceInformation(
                        "[{0}] From the opening move(s): {1}, chosen {2}.",
                        currentMethodName,
                        openingMoves.Select(move => move.ToString()).Join(", "),
                        openingMove);

                    return openingMove;
                }
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

            PieceMove bestMove = null;
            var bestMoveLocalScore = int.MinValue;
            var alpha = RootAlpha;
            var stopwatch = new Stopwatch();

            foreach (var move in orderedMoves)
            {
                cancellationToken.ThrowIfCancellationRequested();

                stopwatch.Restart();

                var currentBoard = board.MakeMove(move);
                var localScore = -EvaluatePositionScore(currentBoard);

                var score = -ComputeAlphaBeta(
                    currentBoard,
                    plyDepth - 1,
                    -RootBeta,
                    -alpha,
                    cancellationToken,
                    transpositionTable);

                stopwatch.Stop();

                Trace.TraceInformation(
                    "[{0}] Move {1}: {2} (local {3}). Time spent: {4}",
                    currentMethodName,
                    move,
                    score,
                    localScore,
                    stopwatch.Elapsed);

                if (score <= alpha)
                {
                    continue;
                }

                alpha = score;
                bestMove = move;
                bestMoveLocalScore = localScore;
            }

            Trace.TraceInformation(
                "[{0}] Best move {1}: {2} (local {3})",
                currentMethodName,
                bestMove,
                alpha,
                bestMoveLocalScore);

            return bestMove;
        }

        #endregion

        #region SimpleTranspositionTable Class

        private sealed class SimpleTranspositionTable
        {
            #region Constants and Fields

            private readonly Dictionary<Tuple<PackedGameBoard, int>, int> _scoreMap =
                new Dictionary<Tuple<PackedGameBoard, int>, int>();

            #endregion

            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="SimpleTranspositionTable"/> class.
            /// </summary>
            internal SimpleTranspositionTable(int maximumItemCount)
            {
                #region Argument Check

                if (maximumItemCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "maximumItemCount",
                        maximumItemCount,
                        @"The value must be positive.");
                }

                #endregion

                this.MaximumItemCount = maximumItemCount;
            }

            #endregion

            #region Public Properties

            public int MaximumItemCount
            {
                get;
                private set;
            }

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
                    return _scoreMap.Count;
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
                if (!_scoreMap.TryGetValue(key, out result))
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

                if (_scoreMap.Count >= this.MaximumItemCount)
                {
                    return;
                }

                var key = GetKey(board, plyDepth);
                _scoreMap.Add(key, score);

                if (_scoreMap.Count >= this.MaximumItemCount)
                {
                    Trace.TraceWarning(
                        "[{0}] Maximum entry count has been reached ({1}).",
                        MethodBase.GetCurrentMethod().GetQualifiedName(),
                        this.MaximumItemCount);
                }
            }

            #endregion

            #region Private Methods

            private static Tuple<PackedGameBoard, int> GetKey([NotNull] IGameBoard board, int plyDepth)
            {
                var packedGameBoard = board.Pack();
                return Tuple.Create(packedGameBoard, plyDepth);
            }

            #endregion
        }

        #endregion
    }
}