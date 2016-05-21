using System;
using System.Collections.Generic;
using System.Linq;
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
    internal sealed class Evaluator
    {
        #region Constants and Static Fields

        private const int KingTropismNormingFactor = 14;
        private const int KingTropismRelativeFactor = 5;

        ////private const int MidgameMaterialLimit = 5800;
        private const int EndgameMaterialLimit = 1470;

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

        private static readonly PieceType[] PhaseDeterminationPieceTypes =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };

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

        #endregion

        #region Fields

        private readonly GameControlInfo _gameControlInfo;

        //// ReSharper disable once NotAccessedField.Local - To be used in EvaluateMobility
        private readonly BoardHelper _boardHelper;

        #endregion

        #region Constructors

        internal Evaluator([NotNull] GameControlInfo gameControlInfo, [NotNull] BoardHelper boardHelper)
        {
            #region Argument Check

            if (gameControlInfo == null)
            {
                throw new ArgumentNullException(nameof(gameControlInfo));
            }

            if (boardHelper == null)
            {
                throw new ArgumentNullException(nameof(boardHelper));
            }

            #endregion

            _gameControlInfo = gameControlInfo;
            _boardHelper = boardHelper;
        }

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaterialWeight(PieceType pieceType, GamePhase gamePhase = GamePhase.Middlegame)
        {
            var materialWeightMap = gamePhase == GamePhase.Endgame
                ? PieceTypeToMaterialWeightInEndgameMap
                : PieceTypeToMaterialWeightInMiddlegameMap;

            return materialWeightMap[pieceType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetKingTropismDistance(Square attackerSquare, Square kingSquare)
        {
            //// Using Manhattan-Distance

            var result = Math.Abs(attackerSquare.Rank - kingSquare.Rank)
                + Math.Abs(attackerSquare.File - kingSquare.File);

            return result;
        }

        public EvaluationScore EvaluatePositionScore([NotNull] GameBoard board)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            _gameControlInfo.CheckInterruptions();

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

        public int ComputeStaticExchangeEvaluationScore(
            [NotNull] GameBoard board,
            Square square,
            [CanBeNull] GameMove move)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

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
                var count = board.GetBitboard(piece).GetSquareCount();
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

                var count = pawnsOnFile.GetSquareCount();

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
                while ((currentSquareIndex = Bitboard.PopFirstSquareIndex(ref remainingBitboard)) >= 0)
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
            while ((attackerSquareIndex = Bitboard.PopFirstSquareIndex(ref remainingAttackers)) >= 0)
            {
                var attackerSquare = new Square(attackerSquareIndex);
                var score = GetKingTropismScore(board, attackerSquare, kingSquare);
                result -= score;
            }

            return result / KingTropismRelativeFactor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static GameMove GetCheapestAttackerMove([NotNull] GameBoard board, Square square)
        {
            var result = board
                .ValidMoves
                .Where(pair => pair.Key.To == square && pair.Value.IsAnyCapture())
                .Select(pair => pair.Key)
                .OrderBy(move => GetMaterialWeight(board[move.From].GetPieceType()))
                .ThenByDescending(move => GetMaterialWeight(move.PromotionResult))
                .FirstOrDefault();

            return result;
        }

        #endregion
    }
}