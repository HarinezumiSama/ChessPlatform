using System;
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

//// ReSharper disable LoopCanBeConvertedToQuery - Using simpler loops for speed optimization
//// ReSharper disable ForCanBeConvertedToForeach - Using simpler loops for speed optimization
//// ReSharper disable once ReturnTypeCanBeEnumerable.Local - Using simpler types (such as arrays) for speed optimization

namespace ChessPlatform.Engine
{
    internal sealed class EnginePlayerMoveSearcher
    {
        #region Constants and Fields

        private const int KingTropismNormingFactor = 14;
        private const int KingTropismRelativeFactor = 5;

        ////private const int MidgameMaterialLimit = 5800;
        private const int EndgameMaterialLimit = 1470;

        private const int QuiesceDepth = -1;

        private const int MaxDepthExtension = 6;

        //// ReSharper disable once RedundantExplicitArraySize - Used to ensure consistency
        private static readonly int[] MiddlegameExtraPawnPenalties = new int[ChessConstants.FileCount]
        {
            45,
            25,
            25,
            25,
            25,
            25,
            25,
            45
        };

        //// ReSharper disable once RedundantExplicitArraySize - Used to ensure consistency
        private static readonly int[] EndgameExtraPawnPenalties = new int[ChessConstants.FileCount]
        {
            65,
            35,
            35,
            35,
            35,
            35,
            35,
            65
        };

        //// ReSharper disable once RedundantExplicitArraySize - Used to ensure consistency
        private static readonly int[] MiddlegameIsolatedPawnPenalties = new int[ChessConstants.FileCount]
        {
            10,
            15,
            15,
            25,
            25,
            15,
            15,
            10
        };

        //// ReSharper disable once RedundantExplicitArraySize - Used to ensure consistency
        private static readonly int[] EndgameIsolatedPawnPenalties = new int[ChessConstants.FileCount]
        {
            20,
            30,
            30,
            50,
            50,
            30,
            30,
            20
        };

        private static readonly Bitboard[] AdjacentFiles = Enumerable
            .Range(0, ChessConstants.FileCount)
            .Select(GetAdjacentFilesBitboard)
            .ToArray();

        private static readonly EvaluationScore NullWindowOffset = new EvaluationScore(1);

        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMaterialWeightInMiddlegameMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMaterialWeightInMiddlegameMap());

        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMaterialWeightInEndgameMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMaterialWeightInEndgameMap());

        // ReSharper disable once UnusedMember.Local
        private static readonly EnumFixedSizeDictionary<PieceType, int> PieceTypeToMobilityWeightMap =
            new EnumFixedSizeDictionary<PieceType, int>(CreatePieceTypeToMobilityWeightMap());

        private static readonly EnumFixedSizeDictionary<Piece, SquareDictionary<int>>
            PieceToSquareWeightInMiddlegameMap =
                new EnumFixedSizeDictionary<Piece, SquareDictionary<int>>(
                    CreatePieceToSquareWeightInMiddlegameMap());

        private static readonly EnumFixedSizeDictionary<Piece, SquareDictionary<int>>
            PieceToSquareWeightInEndgameMap =
                new EnumFixedSizeDictionary<Piece, SquareDictionary<int>>(
                    CreatePieceToSquareWeightInEndgameMap());

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
        private readonly BoardHelper _boardHelper;

        [CanBeNull]
        private readonly TranspositionTable _transpositionTable;

        private readonly VariationLineCache _previousIterationVariationLineCache;
        private readonly GameControlInfo _gameControlInfo;
        private readonly bool _useMultipleProcessors;
        private readonly MoveHistoryStatistics _moveHistoryStatistics;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="EnginePlayerMoveSearcher"/> class.
        /// </summary>
        internal EnginePlayerMoveSearcher(
            [NotNull] GameBoard rootBoard,
            int plyDepth,
            [NotNull] BoardHelper boardHelper,
            [CanBeNull] TranspositionTable transpositionTable,
            [CanBeNull] VariationLineCache previousIterationVariationLineCache,
            [NotNull] GameControlInfo gameControlInfo,
            bool useMultipleProcessors,
            [NotNull] MoveHistoryStatistics moveHistoryStatistics)
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
                    $@"The value must be at least {CommonEngineConstants.MaxPlyDepthLowerLimit}.");
            }

            if (gameControlInfo == null)
            {
                throw new ArgumentNullException(nameof(gameControlInfo));
            }

            if (moveHistoryStatistics == null)
            {
                throw new ArgumentNullException(nameof(moveHistoryStatistics));
            }

            #endregion

            _rootBoard = rootBoard;
            _plyDepth = plyDepth;
            _boardHelper = boardHelper;
            _transpositionTable = transpositionTable;
            _previousIterationVariationLineCache = previousIterationVariationLineCache;
            _gameControlInfo = gameControlInfo;
            _useMultipleProcessors = useMultipleProcessors;
            _moveHistoryStatistics = moveHistoryStatistics;

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
            var result = GetBestMoveInternal(_rootBoard);
            stopwatch.Stop();

            Trace.WriteLine(
                $@"{Environment.NewLine
                    }[{currentMethodName}] {LocalHelper.GetTimestamp()}{Environment.NewLine
                    }  Depth: {_plyDepth}{Environment.NewLine
                    }  Result: {result.ToStandardAlgebraicNotationString(_rootBoard)}{Environment.NewLine
                    }  Time: {stopwatch.Elapsed}{Environment.NewLine
                    }  Nodes: {_boardHelper.LocalMoveCount:#,##0}{Environment.NewLine
                    }  FEN: {_rootBoard.GetFen()}{Environment.NewLine}");

            return result;
        }

        #endregion

        #region Private Methods

        private static Bitboard GetAdjacentFilesBitboard(int index)
        {
            var leftIndex = index - 1;
            var left = ChessConstants.FileRange.Contains(leftIndex) ? Bitboards.Files[leftIndex] : Bitboard.None;

            var rightIndex = index + 1;
            var right = ChessConstants.FileRange.Contains(rightIndex) ? Bitboards.Files[rightIndex] : Bitboard.None;

            var result = left | right;
            return result;
        }

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
                { PieceType.King, 1 },
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
                { PieceType.King, 1 },
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

        private static SquareDictionary<int> ToSquareWeightMap(GameSide side, int[,] weights)
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

            var result = new SquareDictionary<int>();

            var startRank = side == GameSide.White
                ? ChessConstants.RankRange.Upper
                : ChessConstants.RankRange.Lower;
            var rankIncrement = side == GameSide.White ? -1 : 1;

            for (int rank = startRank, rankIndex = ChessConstants.RankRange.Lower;
                rankIndex <= ChessConstants.RankRange.Upper;
                rankIndex++, rank += rankIncrement)
            {
                for (var file = ChessConstants.FileRange.Lower; file <= ChessConstants.FileRange.Upper; file++)
                {
                    var weight = weights[rankIndex, file];
                    result.Add(new Square(file, rank), weight);
                }
            }

            return result;
        }

        private static Dictionary<Piece, SquareDictionary<int>> CreatePieceToSquareWeightMap(
            [NotNull] Dictionary<PieceType, Func<int[,]>> weightGetters)
        {
            var result = weightGetters
                .EnsureNotNull()
                .SelectMany(
                    pair =>
                        ChessConstants.GameSides.Select(
                            side =>
                                new
                                {
                                    Piece = pair.Key.ToPiece(side),
                                    Weights = ToSquareWeightMap(side, pair.Value())
                                }))
                .ToDictionary(obj => obj.Piece, obj => obj.Weights);

            return result;
        }

        private static Dictionary<Piece, SquareDictionary<int>> CreatePieceToSquareWeightInMiddlegameMap()
        {
            var weightGetters =
                new Dictionary<PieceType, Func<int[,]>>
                {
                    { PieceType.Pawn, CreatePawnSquareWeightInMiddlegameMap },
                    { PieceType.Knight, CreateKnightSquareWeightMap },
                    { PieceType.Bishop, CreateBishopSquareWeightMap },
                    { PieceType.Rook, CreateRookSquareWeightMap },
                    { PieceType.Queen, CreateQueenSquareWeightMap },
                    { PieceType.King, CreateKingSquareWeightInMiddlegameMap }
                };

            return CreatePieceToSquareWeightMap(weightGetters);
        }

        private static Dictionary<Piece, SquareDictionary<int>> CreatePieceToSquareWeightInEndgameMap()
        {
            var weightGetters =
                new Dictionary<PieceType, Func<int[,]>>
                {
                    { PieceType.Pawn, CreatePawnSquareWeightInEndgameMap },
                    { PieceType.Knight, CreateKnightSquareWeightMap },
                    { PieceType.Bishop, CreateBishopSquareWeightMap },
                    { PieceType.Rook, CreateRookSquareWeightMap },
                    { PieceType.Queen, CreateQueenSquareWeightMap },
                    { PieceType.King, CreateKingSquareWeightInEndgameMap }
                };

            return CreatePieceToSquareWeightMap(weightGetters);
        }

        private static int[,] CreatePawnSquareWeightInMiddlegameMap()
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

        private static int[,] CreatePawnSquareWeightInEndgameMap()
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

        private static int[,] CreateKnightSquareWeightMap()
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

        private static int[,] CreateBishopSquareWeightMap()
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

        private static int[,] CreateRookSquareWeightMap()
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

        private static int[,] CreateQueenSquareWeightMap()
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

        private static int[,] CreateKingSquareWeightInMiddlegameMap()
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

        private static int[,] CreateKingSquareWeightInEndgameMap()
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
        private static int GetNonPawnMaterialValue([NotNull] GameBoard board, GameSide side)
        {
            var result = 0;

            for (var index = 0; index < PhaseDeterminationPieceTypes.Length; index++)
            {
                var pieceType = PhaseDeterminationPieceTypes[index];
                var piece = pieceType.ToPiece(side);
                var count = board.GetBitboard(piece).GetBitSetCount();
                result += PieceTypeToMaterialWeightInMiddlegameMap[pieceType] * count;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GamePhase GetGamePhase([NotNull] GameBoard board)
        {
            var nonPawnMaterialValue = GetNonPawnMaterialValue(board, GameSide.White)
                + GetNonPawnMaterialValue(board, GameSide.Black);

            return nonPawnMaterialValue <= EndgameMaterialLimit ? GamePhase.Endgame : GamePhase.Middlegame;
        }

        private static int EvaluatePawnStructureBySide(GameBoard board, GameSide side, GamePhase gamePhase)
        {
            var pawnPiece = side.ToPiece(PieceType.Pawn);
            var pawns = board.GetBitboard(pawnPiece);
            if (pawns.IsNone)
            {
                return 0;
            }

            int[] extraPawnPenalties;
            int[] isolatedPawnPenalties;
            if (gamePhase == GamePhase.Endgame)
            {
                extraPawnPenalties = EndgameExtraPawnPenalties;
                isolatedPawnPenalties = EndgameIsolatedPawnPenalties;
            }
            else
            {
                extraPawnPenalties = MiddlegameExtraPawnPenalties;
                isolatedPawnPenalties = MiddlegameIsolatedPawnPenalties;
            }

            var result = 0;
            for (var fileIndex = 0; fileIndex < ChessConstants.FileCount; fileIndex++)
            {
                var file = Bitboards.Files[fileIndex];
                var pawnsOnFile = pawns & file;
                if (pawnsOnFile.IsNone)
                {
                    continue;
                }

                var count = pawnsOnFile.GetBitSetCount();

                //// Extra pawns on files (double/triple/etc)
                var extraCount = Math.Max(0, count - 1);
                result -= extraCount * extraPawnPenalties[fileIndex];

                var adjacentFiles = AdjacentFiles[fileIndex];
                var adjacentFilesPawns = pawns & adjacentFiles;
                if (adjacentFilesPawns.IsNone)
                {
                    //// Isolated pawns on file
                    result -= count * isolatedPawnPenalties[fileIndex];
                }
            }

            return result;
        }

        private static int EvaluateMaterialAndItsPositionBySide(
            [NotNull] GameBoard board,
            GameSide side,
            GamePhase gamePhase)
        {
            var result = 0;

            var pieceToSquareWeightMap = gamePhase == GamePhase.Endgame
                ? PieceToSquareWeightInEndgameMap
                : PieceToSquareWeightInMiddlegameMap;

            foreach (var pieceType in ChessConstants.PieceTypesExceptNone)
            {
                var piece = pieceType.ToPiece(side);
                var pieceBitboard = board.GetBitboard(piece);
                if (pieceBitboard.IsNone)
                {
                    continue;
                }

                var materialWeight = GetMaterialWeight(pieceType, gamePhase);
                var squareWeightMap = pieceToSquareWeightMap[piece];

                var remainingBitboard = pieceBitboard;
                int currentSquareIndex;
                while ((currentSquareIndex = Bitboard.PopFirstBitSetIndex(ref remainingBitboard)) >= 0)
                {
                    result += materialWeight;

                    var square = new Square(currentSquareIndex);
                    var squareWeight = squareWeightMap[square];
                    result += squareWeight;
                }
            }

            var pawnStructureScore = EvaluatePawnStructureBySide(board, side, gamePhase);
            result += pawnStructureScore;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EvaluateMaterialAndItsPosition([NotNull] GameBoard board, GamePhase gamePhase)
        {
            var activeScore = EvaluateMaterialAndItsPositionBySide(board, board.ActiveSide, gamePhase);
            var inactiveScore = EvaluateMaterialAndItsPositionBySide(board, board.ActiveSide.Invert(), gamePhase);

            return activeScore - inactiveScore;
        }

        // ReSharper disable once UnusedMember.Local
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int EvaluateBoardMobility([NotNull] GameBoard board)
        {
            var result = board
                .ValidMoves
                .Keys
                .Sum(move => PieceTypeToMobilityWeightMap[board[move.From].GetPieceType()]);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GameMove GetCheapestAttackerMove([NotNull] GameBoard board, Square square)
        {
            var result = board
                .ValidMoves
                .Where(pair => pair.Key.To == square && pair.Value.IsAnyCapture)
                .Select(pair => pair.Key)
                .OrderBy(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .FirstOrDefault();

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetKingTropismDistance(Square attackerSquare, Square kingSquare)
        {
            //// Using Manhattan-Distance

            var result = Math.Abs(attackerSquare.Rank - kingSquare.Rank)
                + Math.Abs(attackerSquare.File - kingSquare.File);

            return result;
        }

        private static int GetKingTropismScore(
            [NotNull] GameBoard board,
            Square attackerSquare,
            Square kingSquare)
        {
            var proximity = KingTropismNormingFactor - GetKingTropismDistance(attackerSquare, kingSquare);
            var attackerPieceType = board[attackerSquare].GetPieceType();
            var score = proximity * PieceTypeToKingTropismWeightMap[attackerPieceType] / KingTropismNormingFactor;

            return score;
        }

        private static int EvaluateKingTropism([NotNull] GameBoard board, GameSide kingSide)
        {
            var king = kingSide.ToPiece(PieceType.King);
            var kingSquare = board.GetBitboard(king).GetFirstSquare();
            var allAttackersBitboard = board.GetBitboard(kingSide.Invert());

            var result = 0;

            var remainingAttackers = allAttackersBitboard;
            int attackerSquareIndex;
            while ((attackerSquareIndex = Bitboard.PopFirstBitSetIndex(ref remainingAttackers)) >= 0)
            {
                var attackerSquare = new Square(attackerSquareIndex);
                var score = GetKingTropismScore(board, attackerSquare, kingSquare);
                result -= score;
            }

            return result / KingTropismRelativeFactor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetCaptureOrPromotionValue(
            [NotNull] GameBoard board,
            [NotNull] GameMove move,
            GameMoveInfo moveInfo)
        {
            var result = moveInfo.IsAnyCapture ? ComputeStaticExchangeEvaluationScore(board, move.To, move) : 0;

            if (moveInfo.IsPawnPromotion)
            {
                result += GetMaterialWeight(move.PromotionResult) - GetMaterialWeight(PieceType.Pawn);
            }

            return result;
        }

        private OrderedMove[] GetOrderedMoves([NotNull] GameBoard board, int plyDistance)
        {
            const string InternalLogicErrorInMoveOrdering = "Internal logic error in move ordering procedure.";

            var resultList = new List<OrderedMove>(board.ValidMoves.Count);

            if (plyDistance == 0 && _previousIterationVariationLineCache != null)
            {
                var movesOrderedByScore = _previousIterationVariationLineCache.GetOrderedByScore();

                var orderedMoves = movesOrderedByScore
                    .Select(pair => new OrderedMove(pair.Key, board.ValidMoves[pair.Key]))
                    .ToArray();

                resultList.AddRange(orderedMoves);

                if (resultList.Count != board.ValidMoves.Count)
                {
                    throw new InvalidOperationException(InternalLogicErrorInMoveOrdering);
                }

                return resultList.ToArray();
            }

            var remainingMoves = new Dictionary<GameMove, GameMoveInfo>(board.ValidMoves);

            var entryProbe = _transpositionTable?.Probe(board.ZobristKey);
            var ttBestMove = entryProbe?.BestMove;
            if (ttBestMove != null)
            {
                GameMoveInfo moveInfo;
                if (remainingMoves.TryGetValue(ttBestMove, out moveInfo))
                {
                    resultList.Add(new OrderedMove(ttBestMove, moveInfo));
                    remainingMoves.Remove(ttBestMove);
                }
            }

            var opponentKing = board.ActiveSide.Invert().ToPiece(PieceType.King);
            var opponentKingSquare = board.GetBitboard(opponentKing).GetFirstSquare();

            var allCaptureOrPromotionDatas = remainingMoves
                .Where(pair => !LocalHelper.IsQuietMove(pair.Value))
                .Select(
                    pair =>
                        new
                        {
                            Move = pair.Key,
                            MoveInfo = pair.Value,
                            Value = GetCaptureOrPromotionValue(board, pair.Key, pair.Value)
                        })
                .ToArray();

            var goodCaptureOrPromotionMoves = allCaptureOrPromotionDatas
                .Where(obj => obj.Value >= 0)
                .OrderByDescending(obj => obj.Value)
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
                .Select(obj => new OrderedMove(obj.Move, obj.MoveInfo))
                .ToArray();

            resultList.AddRange(goodCaptureOrPromotionMoves);
            goodCaptureOrPromotionMoves.DoForEach(obj => remainingMoves.Remove(obj.Move));

            _moveHistoryStatistics.AddKillerMoves(plyDistance, remainingMoves, resultList);

            var nonCapturingMoves = remainingMoves
                .Where(pair => LocalHelper.IsQuietMove(pair.Value))
                .Select(
                    pair =>
                        new
                        {
                            Move = pair.Key,
                            MoveInfo = pair.Value,
                            Value = _moveHistoryStatistics.GetHistoryValue(board, pair.Key)
                        })
                .OrderByDescending(obj => obj.Value)
                .ThenBy(obj => GetKingTropismDistance(obj.Move.To, opponentKingSquare))
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
                .Select(obj => new OrderedMove(obj.Move, obj.MoveInfo))
                .ToArray();

            resultList.AddRange(nonCapturingMoves);
            nonCapturingMoves.DoForEach(obj => remainingMoves.Remove(obj.Move));

            var badCapturingMoves = allCaptureOrPromotionDatas
                .Where(obj => obj.Value < 0 && remainingMoves.ContainsKey(obj.Move))
                .OrderByDescending(obj => obj.Value)
                .ThenBy(obj => obj.Move.From.SquareIndex)
                .ThenBy(obj => obj.Move.To.SquareIndex)
                .Select(obj => new OrderedMove(obj.Move, obj.MoveInfo))
                .ToArray();

            resultList.AddRange(badCapturingMoves);
            badCapturingMoves.DoForEach(obj => remainingMoves.Remove(obj.Move));

            if (resultList.Count != board.ValidMoves.Count || remainingMoves.Count != 0)
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

        private EvaluationScore EvaluatePositionScore([NotNull] GameBoard board)
        {
            switch (board.State)
            {
                case GameState.Checkmate:
                    return -EvaluationScore.Mate;

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
            var kingTropism = EvaluateKingTropism(board, board.ActiveSide)
                - EvaluateKingTropism(board, board.ActiveSide.Invert());

            var result = new EvaluationScore(materialAndItsPosition + mobility + kingTropism);
            return result;
        }

        private int ComputeStaticExchangeEvaluationScore(
            [NotNull] GameBoard board,
            Square square,
            [CanBeNull] GameMove move)
        {
            _gameControlInfo.CheckInterruptions();

            var actualMove = move ?? GetCheapestAttackerMove(board, square);
            if (actualMove == null)
            {
                return 0;
            }

            var currentBoard = _boardHelper.MakeMove(board, actualMove);
            var lastCapturedPieceType = currentBoard.LastCapturedPiece.GetPieceType();
            var weight = GetMaterialWeight(lastCapturedPieceType);

            var result = weight - ComputeStaticExchangeEvaluationScore(currentBoard, square, null);

            if (move == null && result < 0)
            {
                // If it's not the analyzed move, then the side to move has an option to stand pat
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

            EvaluationScore bestScore;
            EvaluationScore localScore;

            var entryProbe = _transpositionTable?.Probe(board.ZobristKey);
            if (entryProbe.HasValue)
            {
                var ttScore = entryProbe.Value.Score.ConvertValueFromTT(plyDistance);
                var bound = entryProbe.Value.Bound;

                if (!isPrincipalVariation && entryProbe.Value.Depth >= 0
                    && (bound & (ttScore.Value >= beta.Value ? ScoreBound.Lower : ScoreBound.Upper)) != 0)
                {
                    return ttScore;
                }

                localScore = entryProbe.Value.LocalScore;
                bestScore = localScore.ToRelative(plyDistance);

                if ((bound & (ttScore.Value > bestScore.Value ? ScoreBound.Lower : ScoreBound.Upper)) != 0)
                {
                    bestScore = ttScore;
                }
            }
            else
            {
                localScore = EvaluatePositionScore(board);
                bestScore = localScore.ToRelative(plyDistance);
            }

            // Stand pat if local evaluation is at least beta
            if (bestScore.Value >= beta.Value)
            {
                if (!entryProbe.HasValue && _transpositionTable != null)
                {
                    var entry = new TranspositionTableEntry(
                        board.ZobristKey,
                        null,
                        bestScore.ConvertValueForTT(plyDistance),
                        localScore,
                        ScoreBound.Lower,
                        QuiesceDepth);

                    _transpositionTable.Save(ref entry);
                }

                return bestScore;
            }

            var localAlpha = alpha;
            if (isPrincipalVariation && bestScore.Value > localAlpha.Value)
            {
                localAlpha = bestScore;
            }

            var nonQuietMovePairs = board
                .ValidMoves
                .Where(pair => !LocalHelper.IsQuietMove(pair.Value))
                .ToArray();

            GameMove bestMove = null;
            foreach (var movePair in nonQuietMovePairs)
            {
                _gameControlInfo.CheckInterruptions();

                if (movePair.Value.IsAnyCapture)
                {
                    var seeScore = ComputeStaticExchangeEvaluationScore(board, movePair.Key.To, movePair.Key);
                    if (seeScore < 0)
                    {
                        continue;
                    }
                }

                var currentBoard = _boardHelper.MakeMove(board, movePair.Key);
                var score = -Quiesce(currentBoard, plyDistance + 1, -beta, -localAlpha, isPrincipalVariation);

                if (score.Value >= beta.Value)
                {
                    // Fail-soft beta-cutoff

                    if (_transpositionTable != null)
                    {
                        var entry = new TranspositionTableEntry(
                            board.ZobristKey,
                            movePair.Key,
                            score.ConvertValueForTT(plyDistance),
                            localScore,
                            ScoreBound.Lower,
                            QuiesceDepth);

                        _transpositionTable.Save(ref entry);
                    }

                    return score;
                }

                if (score.Value > bestScore.Value)
                {
                    bestScore = score;
                }

                if (score.Value > localAlpha.Value)
                {
                    localAlpha = score;
                    bestMove = movePair.Key;
                }
            }

            if (_transpositionTable != null)
            {
                var entry = new TranspositionTableEntry(
                    board.ZobristKey,
                    bestMove,
                    bestScore.ConvertValueForTT(plyDistance),
                    localScore,
                    isPrincipalVariation && bestScore.Value > alpha.Value ? ScoreBound.Exact : ScoreBound.Upper,
                    QuiesceDepth);

                _transpositionTable.Save(ref entry);
            }

            return bestScore;
        }

        [NotNull]
        private VariationLine ComputeAlphaBeta(
            [NotNull] GameBoard board,
            int plyDistance,
            int requestedMaxDepth,
            EvaluationScore alpha,
            EvaluationScore beta,
            bool isPrincipalVariation,
            bool skipHeuristicPruning,
            int totalDepthExtension)
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

            var defaultRemainingDepth = Math.Max(0, requestedMaxDepth - plyDistance);

            var depthExtension = 0;
            if (totalDepthExtension < MaxDepthExtension)
            {
                if (defaultRemainingDepth <= 0 && board.State.IsCheck())
                {
                    depthExtension++;
                }
            }

            var innerTotalDepthExtension = totalDepthExtension + depthExtension;
            var maxDepth = requestedMaxDepth + depthExtension;

            var correctedRemainingDepth = Math.Max(0, maxDepth - plyDistance);

            EvaluationScore localScore;
            if (isPrincipalVariation)
            {
                localScore = EvaluatePositionScore(board);
            }
            else
            {
                var entryProbe = _transpositionTable?.Probe(board.ZobristKey);
                if (entryProbe.HasValue && entryProbe.Value.Depth >= correctedRemainingDepth)
                {
                    var ttScore = entryProbe.Value.Score.ConvertValueFromTT(plyDistance);
                    var bound = entryProbe.Value.Bound;
                    localScore = entryProbe.Value.LocalScore;

                    if ((bound & (ttScore.Value >= beta.Value ? ScoreBound.Lower : ScoreBound.Upper)) != 0)
                    {
                        var move = entryProbe.Value.BestMove;

                        if (move != null && ttScore.Value >= beta.Value)
                        {
                            _moveHistoryStatistics.RecordCutOffMove(
                                board,
                                move,
                                plyDistance,
                                correctedRemainingDepth,
                                null);
                        }

                        return move == null ? new VariationLine(ttScore) : move | new VariationLine(ttScore);
                    }
                }
                else
                {
                    localScore = EvaluatePositionScore(board);
                }
            }

            if (plyDistance >= maxDepth || board.ValidMoves.Count == 0)
            {
                var quiesceScore = Quiesce(board, plyDistance, localAlpha, localBeta, isPrincipalVariation);
                var result = new VariationLine(quiesceScore);
                return result;
            }

            if (!skipHeuristicPruning && !board.State.IsAnyCheck())
            {
                if (!isPrincipalVariation
                    && correctedRemainingDepth >= 2
                    && localScore.Value >= localBeta.Value
                    && board.CanMakeNullMove
                    && board.HasNonPawnMaterial(board.ActiveSide))
                {
                    //// TODO [vmcl] IDEA (board.HasNonPawnMaterial): Check also that non-pawn pieces have at least one legal move (to avoid zugzwang more thoroughly)

                    var staticEvaluation = EvaluatePositionScore(board);
                    if (staticEvaluation.Value >= localBeta.Value)
                    {
                        var depthReduction = correctedRemainingDepth > 6 ? 4 : 3;

                        var nullMoveBoard = _boardHelper.MakeNullMove(board);

                        var nullMoveLine = -ComputeAlphaBeta(
                            nullMoveBoard,
                            plyDistance + 1,
                            maxDepth - depthReduction,
                            -localBeta,
                            -localBeta + NullWindowOffset,
                            false,
                            true,
                            innerTotalDepthExtension);

                        var nullMoveScore = nullMoveLine.Value;

                        if (nullMoveScore.Value >= localBeta.Value)
                        {
                            if (nullMoveScore.IsCheckmating())
                            {
                                nullMoveScore = localBeta;
                            }

                            var verificationLine = ComputeAlphaBeta(
                                board,
                                plyDistance,
                                maxDepth - depthReduction,
                                localBeta - NullWindowOffset,
                                localBeta,
                                false,
                                true,
                                innerTotalDepthExtension);

                            if (verificationLine.Value.Value >= localBeta.Value)
                            {
                                return new VariationLine(nullMoveScore);
                            }
                        }
                    }
                }
            }

            VariationLine best = null;

            var orderedMoves = GetOrderedMoves(board, plyDistance);
            var moveCount = orderedMoves.Length;
            GameMove bestMove = null;
            var processedQuietMoves = new List<GameMove>(moveCount);
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
                            false,
                            innerTotalDepthExtension);
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
                            false,
                            innerTotalDepthExtension);
                }

                if (variationLine.Value.Value >= localBeta.Value)
                {
                    // Fail-soft beta-cutoff
                    best = move | variationLine;

                    _moveHistoryStatistics.RecordCutOffMove(
                        board,
                        move,
                        plyDistance,
                        correctedRemainingDepth,
                        processedQuietMoves);

                    break;
                }

                if (best == null || variationLine.Value.Value > best.Value.Value)
                {
                    best = move | variationLine;

                    if (variationLine.Value.Value > localAlpha.Value)
                    {
                        localAlpha = variationLine.Value;
                        bestMove = move;
                    }
                }

                if (LocalHelper.IsQuietMove(orderedMove.MoveInfo))
                {
                    processedQuietMoves.Add(move);
                }
            }

            best = best.EnsureNotNull();

            if (_transpositionTable != null)
            {
                var ttEntry = new TranspositionTableEntry(
                    board.ZobristKey,
                    best.FirstMove,
                    best.Value.ConvertValueForTT(plyDistance),
                    localScore,
                    best.Value.Value >= localBeta.Value
                        ? ScoreBound.Lower
                        : (isPrincipalVariation && bestMove != null ? ScoreBound.Exact : ScoreBound.Upper),
                    correctedRemainingDepth);

                _transpositionTable.Save(ref ttEntry);
            }

            return best;
        }

        private VariationLine ComputeAlphaBetaRoot(
            GameBoard board,
            GameMove move,
            int rootMoveIndex,
            int moveCount)
        {
            _gameControlInfo.CheckInterruptions();

            const string CurrentMethodName = nameof(ComputeAlphaBetaRoot);
            const int StartingDelta = 25;

            var moveOrderNumber = rootMoveIndex + 1;

            var stopwatch = Stopwatch.StartNew();
            var currentBoard = _boardHelper.MakeMove(board, move);
            var localScore = -EvaluatePositionScore(currentBoard);

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
                innerVariationLine = -ComputeAlphaBeta(currentBoard, 1, _plyDepth, -beta, -alpha, true, false, 0);
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

        private VariationLine GetBestMoveInternal(GameBoard board)
        {
            _gameControlInfo.CheckInterruptions();

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var orderedMoves = GetOrderedMoves(board, 0);
            var moveCount = orderedMoves.Length;
            if (moveCount == 0)
            {
                throw new InvalidOperationException(@"No moves to evaluate.");
            }

            var threadCount = _useMultipleProcessors ? Math.Max(Environment.ProcessorCount - 1, 1) : 1;

            var tasks = orderedMoves
                .Select(
                    (orderedMove, index) =>
                        new Func<VariationLine>(
                            () => ComputeAlphaBetaRoot(board, orderedMove.Move, index, moveCount)))
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

            var killersInfoString = _moveHistoryStatistics.GetAllKillersInfoString();
            var historyInfoString = _moveHistoryStatistics.GetAllHistoryInfoString();

            var scoreValue = bestVariation.Value.Value.ToString(CultureInfo.InvariantCulture);

            Trace.WriteLine(
                $@"{Environment.NewLine}[{currentMethodName}] Best move {
                    board.GetStandardAlgebraicNotation(bestVariation.FirstMove.EnsureNotNull())}: {scoreValue}.{
                    Environment.NewLine}{Environment.NewLine}Variation Lines ordered by score:{Environment.NewLine}{
                    orderedVariationsString}{Environment.NewLine}{Environment.NewLine}Killer move stats:{
                    Environment.NewLine}{killersInfoString}{Environment.NewLine}{Environment.NewLine}History stats:{
                    Environment.NewLine}{historyInfoString}{Environment.NewLine}");

            return bestVariation;
        }

        #endregion
    }
}