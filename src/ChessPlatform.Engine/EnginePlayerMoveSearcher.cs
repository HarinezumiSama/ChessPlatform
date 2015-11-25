﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;
using ChessPlatform.Utilities;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    internal sealed class EnginePlayerMoveSearcher
    {
        #region Constants and Fields

        private const int KingTropismNormingFactor = 14;
        private const int KingTropismRelativeFactor = 5;

        ////private const int MidgameMaterialLimit = 5800;
        private const int EndgameMaterialLimit = 1470;

        private static readonly EvaluationScore NullWindowOffset = new EvaluationScore(1);

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

        ////private static readonly int PawnValueInMiddlegame = PieceTypeToMaterialWeightInMiddlegameMap[PieceType.Pawn];

        private readonly GameBoard _rootBoard;
        private readonly int _plyDepth;
        private readonly VariationLine _previousIterationBestLine;
        private readonly GameControlInfo _gameControlInfo;
        private readonly bool _useMultipleProcessors;
        private readonly KillerMoveStatistics _killerMoveStatistics;
        private readonly BoardHelper _boardHelper;
        private readonly VariationLineCache _previousIterationVariationLineCache;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="EnginePlayerMoveSearcher"/> class.
        /// </summary>
        internal EnginePlayerMoveSearcher(
            [NotNull] GameBoard rootBoard,
            int plyDepth,
            [NotNull] BoardHelper boardHelper,
            [CanBeNull] VariationLineCache previousIterationVariationLineCache,
            [CanBeNull] VariationLine previousIterationBestLine,
            [NotNull] GameControlInfo gameControlInfo,
            bool useMultipleProcessors,
            [NotNull] KillerMoveStatistics killerMoveStatistics)
        {
            #region Argument Check

            if (rootBoard == null)
            {
                throw new ArgumentNullException(nameof(rootBoard));
            }

            if (plyDepth < CommonEngineConstants.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDepth),
                    plyDepth,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The value must be at least {0}.",
                        CommonEngineConstants.MaxPlyDepthLowerLimit));
            }

            if (gameControlInfo == null)
            {
                throw new ArgumentNullException(nameof(gameControlInfo));
            }

            if (killerMoveStatistics == null)
            {
                throw new ArgumentNullException(nameof(killerMoveStatistics));
            }

            #endregion

            _rootBoard = rootBoard;
            _plyDepth = plyDepth;
            _boardHelper = boardHelper;
            _previousIterationVariationLineCache = previousIterationVariationLineCache;
            _previousIterationBestLine = previousIterationBestLine;
            _gameControlInfo = gameControlInfo;
            _useMultipleProcessors = useMultipleProcessors;
            _killerMoveStatistics = killerMoveStatistics;

            VariationLineCache = new VariationLineCache(rootBoard);
        }

        #endregion

        #region Public Properties

        public long NodeCount
        {
            [DebuggerStepThrough]
            get
            {
                return _boardHelper.LocalMoveCount;
            }
        }

        public VariationLineCache VariationLineCache
        {
            get;
        }

        #endregion

        #region Public Methods

        public VariationLine GetBestMove()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var stopwatch = Stopwatch.StartNew();
            var result = GetBestMoveInternal();
            stopwatch.Stop();

            Trace.WriteLine(
                $@"{Environment.NewLine
                    }[{currentMethodName}] {LocalHelper.GetTimestamp()}{Environment.NewLine
                    }  Depth: {_plyDepth}{Environment.NewLine
                    }  Result: {result.ToStandardAlgebraicNotationString(_rootBoard)}{Environment.NewLine
                    }  Time: {stopwatch.Elapsed}{Environment.NewLine
                    }  Nodes: {_boardHelper.LocalMoveCount}{Environment.NewLine
                    }  FEN: {_rootBoard.GetFen()}{Environment.NewLine}");

            return result;
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMaterialWeight(PieceType pieceType, GamePhase gamePhase = GamePhase.Middlegame)
        {
            var materialWeightMap = gamePhase == GamePhase.Endgame
                ? PieceTypeToMaterialWeightInEndgameMap
                : PieceTypeToMaterialWeightInMiddlegameMap;

            return materialWeightMap[pieceType];
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsQuietMove(GameMoveInfo moveInfo)
        {
            return !moveInfo.IsAnyCapture && !moveInfo.IsPawnPromotion;
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
            var cheapestAttackerMove = board
                .ValidMoves
                .Where(pair => pair.Key.To == position && pair.Value.IsAnyCapture)
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

        //// ReSharper disable SuggestBaseTypeForParameter
        private static void AddKillerMove(
            [CanBeNull] GameMove killerMove,
            [NotNull] Dictionary<GameMove, GameMoveInfo> remainingMoves,
            [NotNull] List<OrderedMove> resultList)
        {
            GameMoveInfo moveInfo;
            if (killerMove == null || !remainingMoves.TryGetValue(killerMove, out moveInfo))
            {
                return;
            }

            resultList.Add(new OrderedMove(killerMove, moveInfo, false));
            remainingMoves.Remove(killerMove);
        }

        //// ReSharper restore SuggestBaseTypeForParameter

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private OrderedMove[] OrderMoves([NotNull] GameBoard board, int plyDistance)
        {
            const string InternalLogicErrorInMoveOrdering = "Internal logic error in move ordering procedure.";

            var resultList = new List<OrderedMove>(board.ValidMoves.Count);

            if (plyDistance == 0 && _previousIterationVariationLineCache != null)
            {
                var movesOrderedByScore = _previousIterationVariationLineCache.GetOrderedByScore();

                resultList.AddRange(
                    movesOrderedByScore.Select(pair => new OrderedMove(pair.Key, board.ValidMoves[pair.Key], true)));

                if (resultList.Count != board.ValidMoves.Count)
                {
                    throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
                }

                return resultList.ToArray();
            }

            var remainingMoves = new Dictionary<GameMove, GameMoveInfo>(board.ValidMoves);

            if (_previousIterationBestLine != null
                && plyDistance < _previousIterationBestLine.Moves.Count)
            {
                var principalVariationMove = _previousIterationBestLine.Moves[plyDistance];

                GameMoveInfo moveInfo;
                if (remainingMoves.TryGetValue(principalVariationMove, out moveInfo))
                {
                    resultList.Add(new OrderedMove(principalVariationMove, moveInfo, true));
                    remainingMoves.Remove(principalVariationMove);
                }
            }

            var opponentKing = PieceType.King.ToPiece(board.ActiveColor.Invert());
            var opponentKingPosition = board.GetBitboard(opponentKing).GetFirstPosition();

            var capturingMoves = remainingMoves
                .Where(pair => pair.Value.IsAnyCapture)
                .Select(pair => new OrderedMove(pair.Key, pair.Value, false))
                .OrderByDescending(obj => GetMaterialWeight(board[obj.Move.To].GetPieceType()))
                .ThenBy(obj => GetMaterialWeight(board[obj.Move.From].GetPieceType()))
                .ThenByDescending(obj => GetMaterialWeight(obj.Move.PromotionResult))
                .ThenBy(obj => obj.Move.PromotionResult)
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
                .ToArray();

            resultList.AddRange(capturingMoves);
            capturingMoves.DoForEach(obj => remainingMoves.Remove(obj.Move));

            if (KillerMoveStatistics.DepthRange.Contains(plyDistance))
            {
                var killerMoveData = _killerMoveStatistics[plyDistance];

                AddKillerMove(killerMoveData.Primary, remainingMoves, resultList);
                AddKillerMove(killerMoveData.Secondary, remainingMoves, resultList);
            }

            var nonCapturingMoves = remainingMoves
                .Where(pair => !pair.Value.IsAnyCapture)
                .Select(pair => new OrderedMove(pair.Key, pair.Value, false))
                .OrderBy(obj => GetKingTropismDistance(obj.Move.To, opponentKingPosition))
                ////.OrderByDescending(obj => GetKingTropismScore(board, obj.Move.To, opponentKingPosition))
                .ThenByDescending(obj => GetMaterialWeight(board[obj.Move.From].GetPieceType()))
                .ThenByDescending(obj => GetMaterialWeight(obj.Move.PromotionResult))
                .ThenBy(obj => obj.Move.PromotionResult)
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
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

        private EvaluationScore EvaluatePositionScore([NotNull] GameBoard board, int plyDistance)
        {
            switch (board.State)
            {
                case GameState.Checkmate:
                    return EvaluationScore.CreateGettingCheckmatedScore(plyDistance);

                case GameState.Stalemate:
                    return EvaluationScore.Zero;

                case GameState.Default:
                    {
                        var autoDrawType = board.GetAutoDrawType();
                        if (autoDrawType != AutoDrawType.None)
                        {
                            return EvaluationScore.Zero;
                        }
                    }

                    break;

                case GameState.Check:
                case GameState.DoubleCheck:
                    break;

                default:
                    throw board.State.CreateEnumValueNotImplementedException();
            }

            var gamePhase = GetGamePhase(board);
            var materialAndItsPosition = EvaluateMaterialAndItsPosition(board, gamePhase);
            var mobility = EvaluateMobility(board);
            var kingTropism = EvaluateKingTropism(board, board.ActiveColor)
                - EvaluateKingTropism(board, board.ActiveColor.Invert());

            var result = new EvaluationScore(materialAndItsPosition + mobility + kingTropism);
            return result;
        }

        private int ComputeStaticExchangeEvaluationScore(
            [NotNull] GameBoard board,
            Position position,
            [CanBeNull] GameMove move)
        {
            _gameControlInfo.CheckInterruptions();

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

        private EvaluationScore Quiesce(
            [NotNull] GameBoard board,
            int plyDistance,
            EvaluationScore alpha,
            EvaluationScore beta,
            bool isPrincipalVariation)
        {
            _gameControlInfo.CheckInterruptions();

            var bestScore = EvaluatePositionScore(board, plyDistance);
            if (bestScore.Value >= beta.Value)
            {
                // Stand pat
                return bestScore;
            }

            var localAlpha = alpha;
            if (isPrincipalVariation && bestScore.Value > localAlpha.Value)
            {
                localAlpha = bestScore;
            }

            var nonQuietMoves = board
                .ValidMoves
                .Where(pair => !IsQuietMove(pair.Value))
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var nonQuietMove in nonQuietMoves)
            {
                _gameControlInfo.CheckInterruptions();

                var seeScore = ComputeStaticExchangeEvaluationScore(board, nonQuietMove.To, nonQuietMove);
                if (seeScore < 0)
                {
                    continue;
                }

                var currentBoard = _boardHelper.MakeMove(board, nonQuietMove);
                var score = -Quiesce(currentBoard, plyDistance + 1, -beta, -localAlpha, isPrincipalVariation);

                if (score.Value >= beta.Value)
                {
                    // Fail-soft beta-cutoff
                    return score;
                }

                if (score.Value > bestScore.Value)
                {
                    bestScore = score;
                }

                if (score.Value > localAlpha.Value)
                {
                    localAlpha = score;
                }
            }

            return bestScore;
        }

        [NotNull]
        private VariationLine ComputeAlphaBeta(
            [NotNull] GameBoard board,
            int plyDistance,
            int maxDepth,
            EvaluationScore alpha,
            EvaluationScore beta,
            bool isPrincipalVariation,
            //// ReSharper disable once UnusedParameter.Local
            bool skipHeuristicPruning)
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

            _gameControlInfo.CheckInterruptions();

            var autoDrawType = board.GetAutoDrawType();
            if (autoDrawType != AutoDrawType.None)
            {
                return VariationLine.Zero;
            }

            // Mate distance pruning
            var localAlpha = EvaluationScore.Max(alpha, EvaluationScore.CreateGettingCheckmatedScore(plyDistance));
            var localBeta = EvaluationScore.Min(beta, EvaluationScore.CreateCheckmatingScore(plyDistance + 1));

            if (localAlpha.Value >= localBeta.Value)
            {
                return new VariationLine(localAlpha);
            }

            ////if (!skipHeuristicPruning)
            ////{
            ////    var remainingDepth = maxDepth - plyDistance;

            ////    if (!isPrincipalVariation
            ////        && remainingDepth >= 2
            ////        && board.CanMakeNullMove
            ////        && board.HasNonPawnMaterial(board.ActiveColor))
            ////    {
            ////        var staticEvaluation = EvaluatePositionScore(board, plyDistance);
            ////        if (staticEvaluation.Value >= localBeta.Value)
            ////        {
            ////            var depthReduction = (400 + 32 * remainingDepth) / 125
            ////                + Math.Min((staticEvaluation.Value - localBeta.Value) / PawnValueInMiddlegame, 3);

            ////            var nullMoveBoard = board.MakeNullMove();

            ////            var nullMoveLine = ComputeAlphaBeta(
            ////                nullMoveBoard,
            ////                plyDistance,
            ////                maxDepth - depthReduction,
            ////                -localBeta,
            ////                -localBeta + NullWindowOffset,
            ////                false,
            ////                true);

            ////            var nullMoveScore = nullMoveLine.Value;

            ////            if (nullMoveScore.Value >= localBeta.Value)
            ////            {
            ////                if (nullMoveScore.IsCheckmating())
            ////                {
            ////                    nullMoveScore = localBeta;
            ////                }

            ////                var verificationLine = ComputeAlphaBeta(
            ////                    board,
            ////                    plyDistance,
            ////                    maxDepth - depthReduction,
            ////                    localBeta - NullWindowOffset,
            ////                    localBeta,
            ////                    false,
            ////                    true);

            ////                if (verificationLine.Value.Value >= localBeta.Value)
            ////                {
            ////                    return new VariationLine(nullMoveScore);
            ////                }
            ////            }
            ////        }
            ////    }
            ////}

            if (plyDistance >= maxDepth || board.ValidMoves.Count == 0)
            {
                var quiesceScore = Quiesce(board, plyDistance, localAlpha, localBeta, isPrincipalVariation);
                var result = new VariationLine(quiesceScore);
                return result;
            }

            VariationLine best = null;

            var orderedMoves = OrderMoves(board, plyDistance);
            var moveCount = orderedMoves.Length;
            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                _gameControlInfo.CheckInterruptions();

                var orderedMove = orderedMoves[moveIndex];
                var move = orderedMove.Move;

                var currentBoard = _boardHelper.MakeMove(board, move);

                var useNullWindow = !isPrincipalVariation || moveIndex > 0;

                VariationLine variationLine = null;
                if (useNullWindow)
                {
                    variationLine =
                        -ComputeAlphaBeta(
                            currentBoard,
                            plyDistance + 1,
                            maxDepth,
                            -localAlpha - NullWindowOffset,
                            -localAlpha,
                            false,
                            skipHeuristicPruning);
                }

                if (isPrincipalVariation
                    && (variationLine == null || moveIndex == 0
                        || (variationLine.Value.Value > localAlpha.Value
                            && variationLine.Value.Value < localBeta.Value)))
                {
                    variationLine =
                        -ComputeAlphaBeta(
                            currentBoard,
                            plyDistance + 1,
                            maxDepth,
                            -localBeta,
                            -localAlpha,
                            true,
                            skipHeuristicPruning);
                }

                if (variationLine.Value.Value >= localBeta.Value)
                {
                    // Fail-soft beta-cutoff
                    best = move | variationLine;

                    if (IsQuietMove(orderedMove.MoveInfo) && !orderedMove.IsPvMove)
                    {
                        _killerMoveStatistics.RecordKiller(plyDistance, move);
                    }

                    return best;
                }

                if (best == null || variationLine.Value.Value > best.Value.Value)
                {
                    best = move | variationLine;
                    if (variationLine.Value.Value > localAlpha.Value)
                    {
                        localAlpha = variationLine.Value;
                    }
                }
            }

            return best.EnsureNotNull();
        }

        private VariationLine AnalyzeRootMoveInternal(
            GameBoard board,
            GameMove move,
            int rootMoveIndex,
            int moveCount)
        {
            _gameControlInfo.CheckInterruptions();

            const string CurrentMethodName = nameof(AnalyzeRootMoveInternal);
            const int StartingDelta = 25;

            var moveOrderNumber = rootMoveIndex + 1;

            var stopwatch = Stopwatch.StartNew();
            var currentBoard = _boardHelper.MakeMove(board, move);
            var localScore = -EvaluatePositionScore(currentBoard, 1);

            var alpha = EvaluationScore.NegativeInfinity;
            var beta = EvaluationScore.PositiveInfinity;
            var delta = EvaluationScore.PositiveInfinityValue;

            if (_plyDepth >= 5)
            {
                var previousPvi = _previousIterationVariationLineCache?[move];
                if (previousPvi != null)
                {
                    delta = StartingDelta;

                    var previousValue = previousPvi.Value.Value;

                    alpha = new EvaluationScore(
                        Math.Max(previousValue - delta, EvaluationScore.NegativeInfinityValue));

                    beta = new EvaluationScore(
                        Math.Min(previousValue + delta, EvaluationScore.PositiveInfinityValue));
                }
            }

            VariationLine innerVariationLine;
            while (true)
            {
                innerVariationLine = -ComputeAlphaBeta(currentBoard, 1, _plyDepth, -beta, -alpha, true, false);
                if (innerVariationLine.Value.Value <= alpha.Value)
                {
                    beta = new EvaluationScore((alpha.Value + beta.Value) / 2);

                    alpha = new EvaluationScore(
                        Math.Max(
                            innerVariationLine.Value.Value - delta,
                            EvaluationScore.NegativeInfinityValue));
                }
                else if (innerVariationLine.Value.Value >= beta.Value)
                {
                    alpha = new EvaluationScore((alpha.Value + beta.Value) / 2);

                    beta = new EvaluationScore(
                        Math.Min(
                            innerVariationLine.Value.Value + delta,
                            EvaluationScore.PositiveInfinityValue));
                }
                else
                {
                    break;
                }

                delta += delta / 2;
            }

            var variationLine = (move | innerVariationLine).WithLocalValue(localScore);
            stopwatch.Stop();

            Trace.WriteLine(
                $@"[{CurrentMethodName} #{moveOrderNumber:D2}/{moveCount:D2}] {move.ToStandardAlgebraicNotation(board)
                    }: {variationLine.ValueString} : L({variationLine.LocalValueString}), line: {{ {
                    board.GetStandardAlgebraicNotation(variationLine.Moves)} }}, time: {
                    stopwatch.Elapsed:g}");

            return variationLine;
        }

        private VariationLine ComputeAlphaBetaRoot(GameBoard board)
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var orderedMoves = OrderMoves(board, 0);
            var moveCount = orderedMoves.Length;
            if (moveCount == 0)
            {
                throw new InvalidOperationException(@"No moves to evaluate.");
            }

            var threadCount = _useMultipleProcessors ? Math.Max(Environment.ProcessorCount, 1) : 1;

            var tasks = orderedMoves
                .Select(
                    (orderedMove, index) =>
                        new Func<VariationLine>(
                            () => AnalyzeRootMoveInternal(board, orderedMove.Move, index, moveCount)))
                .ToArray();

            var multiTaskController = new MultiTaskController<VariationLine>(_gameControlInfo, threadCount, tasks);
            var variationLines = multiTaskController.GetResults();

            foreach (var variationLine in variationLines)
            {
                VariationLineCache[variationLine.FirstMove.EnsureNotNull()] = variationLine;
            }

            var orderedMovesByScore = VariationLineCache.GetOrderedByScore().ToArray();
            var bestVariation = orderedMovesByScore.First().Value.EnsureNotNull();

            var orderedVariationsString = orderedMovesByScore
                .Select(
                    (pair, index) =>
                        $@"  #{index + 1:D2}/{moveCount:D2} {pair.Value.ToStandardAlgebraicNotationString(board)}")
                .Join(Environment.NewLine);

            var killers = _killerMoveStatistics.GetKillersData();
            var killersString = killers
                .Select(
                    (data, i) =>
                        data.Primary == null
                            ? null
                            : $@"  #{i + 1:D2} {{ {data.Primary}, {data.Secondary.ToStringSafely("<none>")} }}")
                .Where(s => !s.IsNullOrEmpty())
                .Join(Environment.NewLine);

            if (killersString.IsNullOrWhiteSpace())
            {
                killersString = "  (none)";
            }

            var scoreValue = bestVariation.Value.Value.ToString(CultureInfo.InvariantCulture);

            Trace.WriteLine(
                $@"{Environment.NewLine}[{currentMethodName}] Best move {
                    board.GetStandardAlgebraicNotation(bestVariation.FirstMove.EnsureNotNull())}: {scoreValue}.{
                    Environment.NewLine}{Environment.NewLine}Variation Lines ordered by score:{Environment.NewLine}{
                    orderedVariationsString}{Environment.NewLine}{Environment.NewLine}Killer move stats:{
                    Environment.NewLine}{killersString}{Environment.NewLine}");

            return bestVariation;
        }

        private VariationLine GetBestMoveInternal()
        {
            _gameControlInfo.CheckInterruptions();

            var result = ComputeAlphaBetaRoot(_rootBoard);
            return result.EnsureNotNull();
        }

        #endregion
    }
}