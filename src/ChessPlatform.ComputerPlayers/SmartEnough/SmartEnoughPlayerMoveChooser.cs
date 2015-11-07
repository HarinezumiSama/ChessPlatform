using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ChessPlatform.GamePlay;
using ChessPlatform.Utilities;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class SmartEnoughPlayerMoveChooser
    {
        #region Constants and Fields

        private const int KingTropismNormingFactor = 14;
        private const int KingTropismRelativeFactor = 5;

        ////private const int MidgameMaterialLimit = 5800;
        private const int EndgameMaterialLimit = 1470;

        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMaterialWeightInMiddlegameMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMaterialWeightInMiddlegameMap());

        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMaterialWeightInEndgameMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMaterialWeightInEndgameMap());

        // ReSharper disable once UnusedMember.Local
        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMobilityWeightMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMobilityWeightMap());

        private static readonly EnumFixedSizeDictionary<Piece, PositionDictionary<int>>
            PieceToPositionWeightInMiddlegameMap =
                new EnumFixedSizeDictionary<Piece, PositionDictionary<int>>(
                    CreatePieceToPositionWeightInMiddlegameMap());

        private static readonly EnumFixedSizeDictionary<Piece, PositionDictionary<int>>
            PieceToPositionWeightInEndgameMap =
                new EnumFixedSizeDictionary<Piece, PositionDictionary<int>>(
                    CreatePieceToPositionWeightInEndgameMap());

        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToKingTropismWeightMap =
            CreatePieceTypeToKingTropismWeightMap();

        private static readonly PieceType[] PhaseDeterminationPieceTypes =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };

        private readonly GameBoard _rootBoard;
        private readonly int _plyDepth;
        private readonly PrincipalVariationInfo _previousIterationBestVariation;
        private readonly CancellationToken _cancellationToken;
        private readonly bool _useMultipleProcessors;
        private readonly SimpleTranspositionTable _transpositionTable;
        private readonly BoardHelper _boardHelper;
        private readonly PrincipalVariationCache _previousIterationPrincipalVariationCache;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmartEnoughPlayerMoveChooser"/> class.
        /// </summary>
        internal SmartEnoughPlayerMoveChooser(
            [NotNull] GameBoard rootBoard,
            int plyDepth,
            [NotNull] BoardHelper boardHelper,
            [CanBeNull] PrincipalVariationCache previousIterationPrincipalVariationCache,
            [CanBeNull] PrincipalVariationInfo previousIterationBestVariation,
            CancellationToken cancellationToken,
            bool useMultipleProcessors)
        {
            #region Argument Check

            if (rootBoard == null)
            {
                throw new ArgumentNullException(nameof(rootBoard));
            }

            if (plyDepth < SmartEnoughPlayerConstants.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDepth),
                    plyDepth,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The value must be at least {0}.",
                        SmartEnoughPlayerConstants.MaxPlyDepthLowerLimit));
            }

            #endregion

            _rootBoard = rootBoard;
            _plyDepth = plyDepth;
            _boardHelper = boardHelper;
            _previousIterationPrincipalVariationCache = previousIterationPrincipalVariationCache;
            _previousIterationBestVariation = previousIterationBestVariation;
            _cancellationToken = cancellationToken;
            _useMultipleProcessors = useMultipleProcessors;

            _transpositionTable = new SimpleTranspositionTable(0); // Disabled for now due to bug
            PrincipalVariationCache = new PrincipalVariationCache(rootBoard);
        }

        #endregion

        #region Public Properties

        public long NodeCount
        {
            [DebuggerStepThrough]
            get
            {
                return _boardHelper.MoveCount;
            }
        }

        public PrincipalVariationCache PrincipalVariationCache
        {
            get;
        }

        #endregion

        #region Public Methods

        public PrincipalVariationInfo GetBestMove()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var stopwatch = Stopwatch.StartNew();
            var result = GetBestMoveInternal();
            stopwatch.Stop();

            Trace.TraceInformation(
                $@"[{currentMethodName} :: {LocalHelper.GetTimestamp()}] Result: {
                    result.ToStandardAlgebraicNotationString(_rootBoard)}, depth {_plyDepth}, time: {stopwatch.Elapsed
                    }, FEN ""{_rootBoard.GetFen()}"".");

            return result;
        }

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetMaterialWeight(PieceType pieceType, GamePhase gamePhase = GamePhase.Middlegame)
        {
            var materialWeightMap = gamePhase == GamePhase.Endgame
                ? PieceTypeToMaterialWeightInEndgameMap
                : PieceTypeToMaterialWeightInMiddlegameMap;

            return materialWeightMap[pieceType];
        }

        #endregion

        #region Private Methods

        private static Dictionary<PieceType, int> CreatePieceTypeToMaterialWeightInMiddlegameMap()
        {
            return new Dictionary<PieceType, int>
            {
                { PieceType.King, 20000 },
                { PieceType.Queen, 900 },
                { PieceType.Rook, 500 },
                { PieceType.Bishop, 325 },
                { PieceType.Knight, 320 },
                { PieceType.Pawn, 100 },
                { PieceType.None, 0 }
            };
        }

        private static Dictionary<PieceType, int> CreatePieceTypeToMaterialWeightInEndgameMap()
        {
            return new Dictionary<PieceType, int>
            {
                { PieceType.King, 20000 },
                { PieceType.Queen, 915 },
                { PieceType.Rook, 505 },
                { PieceType.Bishop, 335 },
                { PieceType.Knight, 330 },
                { PieceType.Pawn, 130 },
                { PieceType.None, 0 }
            };
        }

        private static Dictionary<PieceType, int> CreatePieceTypeToMobilityWeightMap()
        {
            return new Dictionary<PieceType, int>
            {
                { PieceType.King, 20 },
                { PieceType.Queen, 10 },
                { PieceType.Rook, 6 },
                { PieceType.Bishop, 5 },
                { PieceType.Knight, 5 },
                { PieceType.Pawn, 4 }
            };
        }

        private static EnumFixedSizeDictionary<PieceType, int> CreatePieceTypeToKingTropismWeightMap()
        {
            var result = new EnumFixedSizeDictionary<PieceType, int>(PieceTypeToMaterialWeightInMiddlegameMap)
            {
                [PieceType.King] = 0
            };

            return result;
        }

        private static PositionDictionary<int> ToPositionWeightMap(PieceColor color, int[,] weights)
        {
            #region Argument Check

            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            if (weights.Length != ChessConstants.SquareCount)
            {
                throw new ArgumentException(@"Invalid array length.", nameof(weights));
            }

            #endregion

            var result = new PositionDictionary<int>();

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

        private static Dictionary<Piece, PositionDictionary<int>> CreatePieceToPositionWeightMap(
            [NotNull] Dictionary<PieceType, Func<int[,]>> weightGetters)
        {
            var result = weightGetters
                .EnsureNotNull()
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

        private static Dictionary<Piece, PositionDictionary<int>> CreatePieceToPositionWeightInMiddlegameMap()
        {
            var weightGetters =
                new Dictionary<PieceType, Func<int[,]>>
                {
                    { PieceType.Pawn, CreatePawnPositionWeightInMiddlegameMap },
                    { PieceType.Knight, CreateKnightPositionWeightMap },
                    { PieceType.Bishop, CreateBishopPositionWeightMap },
                    { PieceType.Rook, CreateRookPositionWeightMap },
                    { PieceType.Queen, CreateQueenPositionWeightMap },
                    { PieceType.King, CreateKingPositionWeightInMiddlegameMap }
                };

            return CreatePieceToPositionWeightMap(weightGetters);
        }

        private static Dictionary<Piece, PositionDictionary<int>> CreatePieceToPositionWeightInEndgameMap()
        {
            var weightGetters =
                new Dictionary<PieceType, Func<int[,]>>
                {
                    { PieceType.Pawn, CreatePawnPositionWeightInEndgameMap },
                    { PieceType.Knight, CreateKnightPositionWeightMap },
                    { PieceType.Bishop, CreateBishopPositionWeightMap },
                    { PieceType.Rook, CreateRookPositionWeightMap },
                    { PieceType.Queen, CreateQueenPositionWeightMap },
                    { PieceType.King, CreateKingPositionWeightInEndgameMap }
                };

            return CreatePieceToPositionWeightMap(weightGetters);
        }

        private static int[,] CreatePawnPositionWeightInMiddlegameMap()
        {
            var weights = new[,]
            {
                { 000, 000, 000, 000, 000, 000, 000, 000 },
                { +40, +50, +50, +50, +50, +50, +50, +40 },
                { +10, +20, +20, +30, +30, +20, +20, +10 },
                { +05, +07, +10, +25, +25, +10, +07, +05 },
                { 000, 000, 000, +20, +20, 000, 000, 000 },
                { +05, -05, -10, 000, 000, -10, -05, +05 },
                { +05, +10, +10, -20, -20, +10, +10, +05 },
                { 000, 000, 000, 000, 000, 000, 000, 000 }
            };

            return weights;
        }

        private static int[,] CreatePawnPositionWeightInEndgameMap()
        {
            var weights = new[,]
            {
                { 000, 000, 000, 000, 000, 000, 000, 000 },
                { +72, +90, +90, +90, +90, +90, +90, +72 },
                { +43, +54, +54, +54, +54, +54, +54, +43 },
                { +36, +45, +45, +45, +45, +45, +45, +36 },
                { +28, +36, +36, +36, +36, +36, +36, +28 },
                { 000, 000, 000, 000, 000, 000, 000, 000 },
                { -28, -36, -36, -36, -36, -36, -36, -28 },
                { 000, 000, 000, 000, 000, 000, 000, 000 }
            };

            return weights;
        }

        private static int[,] CreateKnightPositionWeightMap()
        {
            //// Formula used here for knight (normalized to range from -50 to +30):
            ////   Weight = ((Sq - 2) * 10) - 50
            //// where:
            ////   Sq - number of controlled/attacked squares on the empty board plus:
            ////        if positioned on a central square (e4, d4, e5, or d5), then bonus +2;
            ////        otherwise, no bonus.

            var weights = new[,]
            {
                { -50, -40, -30, -30, -30, -30, -40, -50 },
                { -40, -30, -10, -10, -10, -10, -30, -40 },
                { -30, -10, +10, +10, +10, +10, -10, -30 },
                { -30, -10, +10, +30, +30, +10, -10, -30 },
                { -30, -10, +10, +30, +30, +10, -10, -30 },
                { -30, -10, +10, +10, +10, +10, -10, -30 },
                { -40, -30, -10, -10, -10, -10, -30, -40 },
                { -50, -40, -30, -30, -30, -30, -40, -50 }
            };

            return weights;
        }

        private static int[,] CreateBishopPositionWeightMap()
        {
            //// Formula used here for bishop (normalized to range from -20 to +20):
            ////   Weight = (((Sq * N) - 7) / 45 * 40) - 20
            //// where:
            ////   Sq - number of controlled/attacked squares on the empty board
            ////   N  - number of possible move directions on the empty board (1, 2 or 4)

            var weights = new[,]
            {
                { -20, -13, -13, -13, -13, -13, -13, -20 },
                { -13, +05, +05, +05, +05, +05, +05, -13 },
                { -13, +05, +12, +12, +12, +12, +05, -13 },
                { -13, +05, +12, +20, +20, +12, +05, -13 },
                { -13, +05, +12, +20, +20, +12, +05, -13 },
                { -13, +05, +12, +12, +12, +12, +05, -13 },
                { -13, +05, +05, +05, +05, +05, +05, -13 },
                { -20, -13, -13, -13, -13, -13, -13, -20 }
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

        private static int[,] CreateKingPositionWeightInMiddlegameMap()
        {
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

            return weights;
        }

        private static int[,] CreateKingPositionWeightInEndgameMap()
        {
            var weights = new[,]
            {
                { -80, -50, -30, -20, -20, -30, -50, -80 },
                { -50, -30, -10, -10, -10, -10, -30, -50 },
                { -30, -10, +20, +30, +30, +20, -10, -30 },
                { -20, -10, +30, +40, +40, +30, -10, -20 },
                { -20, -10, +30, +40, +40, +30, -10, -20 },
                { -30, -10, +20, +30, +30, +20, -10, -30 },
                { -50, -30, -10, -10, -10, -10, -30, -50 },
                { -80, -50, -30, -20, -20, -30, -50, -80 }
            };

            return weights;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNonPawnMaterialValue([NotNull] GameBoard board, PieceColor color)
        {
            var result = 0;

            //// ReSharper disable once ForCanBeConvertedToForeach - For optimization (hopefully)
            //// ReSharper disable once LoopCanBeConvertedToQuery - For optimization (hopefully)
            for (var index = 0; index < PhaseDeterminationPieceTypes.Length; index++)
            {
                var pieceType = PhaseDeterminationPieceTypes[index];
                var piece = pieceType.ToPiece(color);
                var count = board.GetBitboard(piece).GetBitSetCount();
                result += PieceTypeToMaterialWeightInMiddlegameMap[pieceType] * count;
            }

            return result;
        }

        private static GamePhase GetGamePhase([NotNull] GameBoard board)
        {
            var nonPawnMaterialValue = GetNonPawnMaterialValue(board, PieceColor.White)
                + GetNonPawnMaterialValue(board, PieceColor.Black);

            return nonPawnMaterialValue <= EndgameMaterialLimit ? GamePhase.Endgame : GamePhase.Middlegame;
        }

        private static int EvaluateMaterialAndItsPositionByColor(
            [NotNull] GameBoard board,
            PieceColor color,
            GamePhase gamePhase)
        {
            var result = 0;

            var pieceToPositionWeightMap = gamePhase == GamePhase.Endgame
                ? PieceToPositionWeightInEndgameMap
                : PieceToPositionWeightInMiddlegameMap;

            foreach (var pieceType in ChessConstants.PieceTypesExceptNone)
            {
                var piece = pieceType.ToPiece(color);
                var pieceBitboard = board.GetBitboard(piece);
                if (pieceBitboard.IsNone)
                {
                    continue;
                }

                var materialWeight = GetMaterialWeight(pieceType, gamePhase);
                var positionWeightMap = pieceToPositionWeightMap[piece];

                var remainingBitboard = pieceBitboard;
                int currentSquareIndex;
                while ((currentSquareIndex = Bitboard.PopFirstBitSetIndex(ref remainingBitboard)) >= 0)
                {
                    result += materialWeight;

                    var position = Position.FromSquareIndex(currentSquareIndex);
                    var positionScore = positionWeightMap[position];
                    result += positionScore;
                }
            }

            return result;
        }

        private static int EvaluateMaterialAndItsPosition([NotNull] GameBoard board, GamePhase gamePhase)
        {
            var activeScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor, gamePhase);
            var inactiveScore = EvaluateMaterialAndItsPositionByColor(board, board.ActiveColor.Invert(), gamePhase);

            return activeScore - inactiveScore;
        }

        // ReSharper disable once UnusedMember.Local
        private static int EvaluateBoardMobility([NotNull] GameBoard board)
        {
            var result = board
                .ValidMoves
                .Keys
                .Sum(move => PieceTypeToMobilityWeightMap[board[move.From].GetPieceType()]);

            return result;
        }

        private static GameMove GetCheapestAttackerMove([NotNull] GameBoard board, Position position)
        {
            //// TODO [vmcl] Consider en passant capture

            var cheapestAttackerMove = board
                .ValidMoves
                .Where(pair => pair.Key.To == position && pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderBy(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .FirstOrDefault();

            return cheapestAttackerMove;
        }

        private static int GetKingTropismDistance(Position attackerPosition, Position kingPosition)
        {
            //// Using Manhattan-Distance

            var result = Math.Abs(attackerPosition.Rank - kingPosition.Rank)
                + Math.Abs(attackerPosition.File - kingPosition.File);

            return result;
        }

        private static int GetKingTropismScore(
            [NotNull] GameBoard board,
            Position attackerPosition,
            Position kingPosition)
        {
            var proximity = KingTropismNormingFactor - GetKingTropismDistance(attackerPosition, kingPosition);
            var attackerPieceType = board[attackerPosition].GetPieceType();
            var score = proximity * PieceTypeToKingTropismWeightMap[attackerPieceType] / KingTropismNormingFactor;

            return score;
        }

        private static int EvaluateKingTropism([NotNull] GameBoard board, PieceColor kingColor)
        {
            var king = PieceType.King.ToPiece(kingColor);
            var kingPosition = board.GetBitboard(king).GetFirstPosition();
            var allAttackersBitboard = board.GetBitboard(kingColor.Invert());

            var result = 0;

            var remainingAttackers = allAttackersBitboard;
            int attackerSquareIndex;
            while ((attackerSquareIndex = Bitboard.PopFirstBitSetIndex(ref remainingAttackers)) >= 0)
            {
                var attackerPosition = Position.FromSquareIndex(attackerSquareIndex);
                var score = GetKingTropismScore(board, attackerPosition, kingPosition);
                result -= score;
            }

            return result / KingTropismRelativeFactor;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private GameMove[] OrderMoves([NotNull] GameBoard board, int plyDistance)
        {
            const string InternalLogicErrorInMoveOrdering = "Internal logic error in move ordering procedure.";

            var resultList = new List<GameMove>(board.ValidMoves.Count);

            var validMoves = board.ValidMoves.ToArray();

            if (_previousIterationPrincipalVariationCache != null && plyDistance == 0)
            {
                var movesOrderedByScore = _previousIterationPrincipalVariationCache.GetOrderedByScore();
                resultList.AddRange(movesOrderedByScore.Select(pair => pair.Key));

                if (resultList.Count != board.ValidMoves.Count)
                {
                    throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
                }

                return resultList.ToArray();
            }

            if (_previousIterationBestVariation != null
                && plyDistance < _previousIterationBestVariation.Moves.Count)
            {
                var principalVariationMove = _previousIterationBestVariation.Moves[plyDistance];
                if (board.ValidMoves.ContainsKey(principalVariationMove))
                {
                    resultList.Add(principalVariationMove);
                    validMoves = validMoves.Where(pair => pair.Key != principalVariationMove).ToArray();
                }
            }

            var opponentKing = PieceType.King.ToPiece(board.ActiveColor.Invert());
            var opponentKingPosition = board.GetBitboard(opponentKing).GetFirstPosition();

            var capturingMoves = validMoves
                .Where(pair => pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderByDescending(move => GetMaterialWeight(board[move.To].GetPieceType()))
                .ThenBy(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            resultList.AddRange(capturingMoves);

            var nonCapturingMoves = validMoves
                .Where(pair => !pair.Value.IsCapture)
                .Select(pair => pair.Key)
                .OrderBy(move => GetKingTropismDistance(move.To, opponentKingPosition))
                ////.OrderByDescending(move => GetKingTropismScore(board, move.To, opponentKingPosition))
                .ThenByDescending(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .ThenBy(move => move.PromotionResult)
                .ThenBy(move => move.From.SquareIndex)
                .ThenBy(move => move.To.SquareIndex)
                .ToArray();

            resultList.AddRange(nonCapturingMoves);

            if (resultList.Count != board.ValidMoves.Count)
            {
                throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
            }

            return resultList.ToArray();
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        // ReSharper disable once UnusedParameter.Local
        private int EvaluateMobility([NotNull] GameBoard board)
        {
            return 0;

            ////if (!board.CanMakeNullMove)
            ////{
            ////    return 0;
            ////}

            ////var nullMoveBoard = _boardHelper.MakeNullMove(board);

            ////var mobility = EvaluateBoardMobility(board);
            ////var opponentMobility = EvaluateBoardMobility(nullMoveBoard);

            ////var result = mobility - opponentMobility;
            ////return result;
        }

        private int EvaluatePositionScore([NotNull] GameBoard board, int plyDistance)
        {
            switch (board.State)
            {
                case GameState.Checkmate:
                    return -LocalConstants.MateScoreAbs + plyDistance;

                case GameState.Stalemate:
                    return 0;

                default:
                    {
                        var autoDrawType = board.GetAutoDrawType();
                        if (autoDrawType != AutoDrawType.None)
                        {
                            return 0;
                        }
                    }

                    break;
            }

            var gamePhase = GetGamePhase(board);
            var materialAndItsPosition = EvaluateMaterialAndItsPosition(board, gamePhase);
            var mobility = EvaluateMobility(board);
            var kingTropism = EvaluateKingTropism(board, board.ActiveColor)
                - EvaluateKingTropism(board, board.ActiveColor.Invert());

            var result = materialAndItsPosition + mobility + kingTropism;
            return result;
        }

        private int ComputeStaticExchangeEvaluationScore(
            [NotNull] GameBoard board,
            Position position,
            [CanBeNull] GameMove move)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var actualMove = move ?? GetCheapestAttackerMove(board, position);
            if (actualMove == null)
            {
                return 0;
            }

            var currentBoard = _boardHelper.MakeMove(board, actualMove);
            var gamePhase = GetGamePhase(board);
            var weight = GetMaterialWeight(currentBoard.LastCapturedPiece.GetPieceType(), gamePhase);

            var result = weight - ComputeStaticExchangeEvaluationScore(currentBoard, position, null);

            if (move == null && result < 0)
            {
                // If it's not the root move, then the side to move has an option to stand pat
                result = 0;
            }

            return result;
        }

        private int Quiesce([NotNull] GameBoard board, int alpha, int beta, int plyDistance)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var standPatScore = EvaluatePositionScore(board, plyDistance);
            if (beta <= standPatScore)
            {
                return beta;
            }

            if (alpha < standPatScore)
            {
                alpha = standPatScore;
            }

            var captureMoves = board.ValidMoves.Where(pair => pair.Value.IsCapture).Select(pair => pair.Key).ToArray();
            foreach (var captureMove in captureMoves)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var seeScore = ComputeStaticExchangeEvaluationScore(board, captureMove.To, captureMove);
                if (seeScore < 0)
                {
                    continue;
                }

                var currentBoard = _boardHelper.MakeMove(board, captureMove);
                var score = -Quiesce(currentBoard, -beta, -alpha, plyDistance);

                if (beta <= score)
                {
                    // Fail-hard beta-cutoff
                    return beta;
                }

                if (alpha < score)
                {
                    alpha = score;
                }
            }

            return alpha;
        }

        private PrincipalVariationInfo ComputeAlphaBeta(
            [NotNull] GameBoard board,
            int plyDistance,
            PrincipalVariationInfo alpha,
            PrincipalVariationInfo beta)
        {
            #region Argument Check

            if (plyDistance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDistance),
                    plyDistance,
                    @"The value must be positive.");
            }

            #endregion

            _cancellationToken.ThrowIfCancellationRequested();

            var cachedScore = _transpositionTable.GetScore(board, plyDistance);
            if (cachedScore != null)
            {
                return cachedScore;
            }

            var autoDrawType = board.GetAutoDrawType();
            if (autoDrawType != AutoDrawType.None)
            {
                return PrincipalVariationInfo.Zero;
            }

            if (plyDistance == _plyDepth || board.ValidMoves.Count == 0)
            {
                var quiesceScore = Quiesce(board, alpha.Value, beta.Value, plyDistance);
                var score = new PrincipalVariationInfo(quiesceScore);
                _transpositionTable.SaveScore(board, plyDistance, score);
                return score;
            }

            var bestScore = alpha;

            var orderedMoves = OrderMoves(board, plyDistance);
            foreach (var move in orderedMoves)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var currentBoard = _boardHelper.MakeMove(board, move);
                var score = -ComputeAlphaBeta(currentBoard, plyDistance + 1, -beta, -alpha);

                if (score.Value >= beta.Value)
                {
                    // Fail-hard beta-cutoff
                    _transpositionTable.SaveScore(board, plyDistance, beta);
                    return beta;
                }

                if (score.Value > alpha.Value)
                {
                    alpha = score;
                    bestScore = move | score;
                }
            }

            _transpositionTable.SaveScore(board, plyDistance, bestScore);

            return bestScore;
        }

        private KeyValuePair<GameMove, PrincipalVariationInfo> AnalyzeRootMoveInternal(
            GameBoard board,
            GameMove move,
            int rootMoveIndex,
            int moveCount)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            const string CurrentMethodName = nameof(AnalyzeRootMoveInternal);

            var moveOrderNumber = rootMoveIndex + 1;

            var stopwatch = Stopwatch.StartNew();
            var currentBoard = _boardHelper.MakeMove(board, move);
            var innerPrincipalVariationInfo =
                -ComputeAlphaBeta(currentBoard, 1, LocalConstants.RootAlphaInfo, -LocalConstants.RootAlphaInfo);
            stopwatch.Stop();

            var principalVariationInfo = move | innerPrincipalVariationInfo;

            Trace.TraceInformation(
                $@"[{CurrentMethodName} #{moveOrderNumber:D2}/{moveCount:D2}] {move.ToStandardAlgebraicNotation(board)
                    }: {principalVariationInfo.Value}, PV: {{ {
                    board.GetStandardAlgebraicNotation(principalVariationInfo.Moves)} }}, time: {
                    stopwatch.Elapsed:g}");

            return KeyValuePair.Create(move, principalVariationInfo);
        }

        private PrincipalVariationInfo ComputeAlphaBetaRoot(GameBoard board)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var orderedMoves = OrderMoves(board, 0);
            var moveCount = orderedMoves.Length;
            if (moveCount == 0)
            {
                throw new InvalidOperationException(@"No moves to evaluate.");
            }

            var variationPairs = _useMultipleProcessors
                ? orderedMoves
                    .AsParallel()
                    .WithCancellation(_cancellationToken)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select((move, index) => AnalyzeRootMoveInternal(board, move, index, moveCount))
                    .ToArray()
                : orderedMoves
                    .Select((move, index) => AnalyzeRootMoveInternal(board, move, index, moveCount))
                    .ToArray();

            foreach (var variationPair in variationPairs)
            {
                PrincipalVariationCache[variationPair.Key] = variationPair.Value;
            }

            var orderedMovesByScore = PrincipalVariationCache.GetOrderedByScore().ToArray();
            var bestVariation = orderedMovesByScore.First().Value.EnsureNotNull();

            var orderedVariationsString = orderedMovesByScore
                .Select(pair => $@"  {pair.Value.ToStandardAlgebraicNotationString(board)}")
                .Join(Environment.NewLine);

            var scoreValue = bestVariation.Value.ToString(CultureInfo.InvariantCulture);
            Trace.TraceInformation(
                $@"[{currentMethodName}] Best move {
                    board.GetStandardAlgebraicNotation(bestVariation.FirstMove.EnsureNotNull())}: {scoreValue}.{
                    Environment.NewLine}{Environment.NewLine}PVs ordered by score:{Environment.NewLine}{
                    orderedVariationsString}{Environment.NewLine}");

            return bestVariation;
        }

        private PrincipalVariationInfo GetBestMoveInternal()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var result = ComputeAlphaBetaRoot(_rootBoard);
            return result.EnsureNotNull();
        }

        #endregion
    }
}