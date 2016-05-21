using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.Internal;
using Omnifactotum;
using Omnifactotum.Annotations;

//// ReSharper disable LoopCanBeConvertedToQuery - Using simpler loops for speed optimization
//// ReSharper disable ForCanBeConvertedToForeach - Using simpler loops for speed optimization
//// ReSharper disable ReturnTypeCanBeEnumerable.Local - Using simpler types (such as arrays) for speed optimization
//// ReSharper disable SuggestBaseTypeForParameter - Using specific types (such as arrays) for speed optimization

namespace ChessPlatform
{
    internal abstract class GamePosition
    {
        #region Constants and Fields

        protected static readonly ShiftDirection[] AllDirections = EnumFactotum.GetAllValues<ShiftDirection>();
        protected static readonly ShiftDirection[] QueenDirections = AllDirections;

        protected static readonly ShiftDirection[] RookDirections =
        {
            ShiftDirection.North,
            ShiftDirection.South,
            ShiftDirection.East,
            ShiftDirection.West
        };

        protected static readonly ShiftDirection[] BishopDirections =
        {
            ShiftDirection.NorthEast,
            ShiftDirection.NorthWest,
            ShiftDirection.SouthEast,
            ShiftDirection.SouthWest
        };

        protected static readonly Bitboard[] StraightSlidingAttacks = InitializeStraightSlidingAttacks();
        protected static readonly Bitboard[] DiagonallySlidingAttacks = InitializeDiagonallySlidingAttacks();
        protected static readonly Bitboard[] KnightAttacksOrMoves = InitializeKnightAttacksOrMoves();
        protected static readonly Bitboard[] KingAttacksOrMoves = InitializeKingAttacksOrMoves();

        protected static readonly Bitboard[] Connections = InitializeConnections();

        protected static readonly Bitboard[] DefaultPinLimitations =
            Enumerable.Repeat(Bitboard.Everything, ChessConstants.SquareCount).ToArray();

        #endregion

        #region Protected Properties

        protected abstract PiecePosition PiecePosition
        {
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return PiecePosition.GetFenSnippet();
        }

        public abstract GamePosition Copy();

        public abstract bool IsSamePosition([NotNull] GamePosition other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard GetAttackers(Square targetSquare, GameSide attackingSide)
        {
            return GetAttackersInternal(targetSquare, attackingSide, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUnderAttack(Square targetSquare, GameSide attackingSide)
        {
            var attackers = GetAttackersInternal(targetSquare, attackingSide, true);
            return attackers.IsAny;
        }

        public abstract GamePosition MakeMove([NotNull] GameMove move);

        #endregion

        #region Protected Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Bitboard GetConnection(int squareIndex1, int squareIndex2)
        {
            #region Argument Check

            if (squareIndex1 < 0 || squareIndex1 > ChessConstants.MaxSquareIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(squareIndex1),
                    squareIndex1,
                    $@"The value is out of the valid range ({0} .. {ChessConstants.MaxSquareIndex}).");
            }

            if (squareIndex2 < 0 || squareIndex2 > ChessConstants.MaxSquareIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(squareIndex2),
                    squareIndex2,
                    $@"The value is out of the valid range ({0} .. {ChessConstants.MaxSquareIndex}).");
            }

            #endregion

            return GetConnectionInternal(squareIndex1, squareIndex2);
        }

        protected void GenerateKnightMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes,
            Bitboard target)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            var emptySquares = PiecePosition[Piece.None];
            var enemies = PiecePosition[side.Invert()];

            var internalTarget = Bitboard.None;
            if (moveTypes.IsAnySet(GeneratedMoveTypes.Quiet))
            {
                internalTarget |= emptySquares;
            }

            if (moveTypes.IsAnySet(GeneratedMoveTypes.Capture))
            {
                internalTarget |= enemies;
            }

            var actualTarget = target & internalTarget;
            if (actualTarget.IsNone)
            {
                return;
            }

            var knightPiece = side.ToPiece(PieceType.Knight);
            var knights = PiecePosition[knightPiece];

            while (knights.IsAny)
            {
                var sourceSquareIndex = Bitboard.PopFirstSquareIndex(ref knights);
                var moves = KnightAttacksOrMoves[sourceSquareIndex];
                var movesOnTarget = moves & actualTarget;
                if (movesOnTarget.IsNone)
                {
                    continue;
                }

                var sourceSquare = new Square(sourceSquareIndex);
                if (moveTypes.IsAnySet(GeneratedMoveTypes.Capture))
                {
                    var captures = movesOnTarget & enemies;
                    PopulateSimpleMoves(resultMoves, sourceSquare, captures, GameMoveFlags.IsRegularCapture);
                }

                if (moveTypes.IsAnySet(GeneratedMoveTypes.Quiet))
                {
                    var nonCaptures = movesOnTarget & emptySquares;
                    PopulateSimpleMoves(resultMoves, sourceSquare, nonCaptures, GameMoveFlags.None);
                }
            }
        }

        #endregion

        #region Private Methods

        private static Bitboard[] InitializeStraightSlidingAttacks()
        {
            var result = new Bitboard[ChessConstants.SquareCount];

            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var square = new Square(squareIndex);
                var attackBitboard = new Bitboard(
                    Square.GenerateRank(square.Rank).Concat(Square.GenerateFile(square.File)));
                result[squareIndex] = attackBitboard;
            }

            return result;
        }

        private static Bitboard[] InitializeDiagonallySlidingAttacks()
        {
            var result = new Bitboard[ChessConstants.SquareCount];

            var directions = new[]
            {
                ShiftDirection.NorthEast,
                ShiftDirection.SouthEast,
                ShiftDirection.SouthWest,
                ShiftDirection.NorthWest
            };

            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var bitboard = Bitboard.FromSquareIndex(squareIndex);

                var attackBitboard = Bitboard.None;
                foreach (var direction in directions)
                {
                    var current = bitboard;
                    while ((current = current.Shift(direction)).IsAny)
                    {
                        attackBitboard |= current;
                    }
                }

                result[squareIndex] = attackBitboard;
            }

            return result;
        }

        private static Bitboard[] InitializeKnightAttacksOrMoves()
        {
            var result = new Bitboard[ChessConstants.SquareCount];

            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var square = new Square(squareIndex);
                var moveSquares = ChessHelper.GetKnightMoveSquares(square);
                var bitboard = new Bitboard(moveSquares);
                result[squareIndex] = bitboard;
            }

            return result;
        }

        private static Bitboard[] InitializeKingAttacksOrMoves()
        {
            var result = new Bitboard[ChessConstants.SquareCount * ChessConstants.SquareCount];

            var directions = EnumFactotum.GetAllValues<ShiftDirection>();

            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var sourceBitboard = Bitboard.FromSquareIndex(squareIndex);

                var movesBitboard = directions
                    .Select(sourceBitboard.Shift)
                    .Aggregate(Bitboard.None, (current, bitboard) => current | bitboard);

                result[squareIndex] = movesBitboard;
            }

            return result;
        }

        private static Bitboard[] InitializeConnections()
        {
            var result = new Bitboard[ChessConstants.SquareCount * ChessConstants.SquareCount];
            result.Initialize(i => Bitboard.Everything);

            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var source = Bitboard.FromSquareIndex(squareIndex);

                foreach (var direction in AllDirections)
                {
                    var current = source;
                    var connection = Bitboard.None;
                    while ((current = current.Shift(direction)).IsAny)
                    {
                        var anotherSquareIndex = current.FindFirstSquareIndex();
                        var index = GetConnectionIndex(squareIndex, anotherSquareIndex);
                        var reverseIndex = GetConnectionIndex(anotherSquareIndex, squareIndex);
                        result[index] = result[reverseIndex] = connection;

                        connection |= current;
                    }
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetConnectionIndex(int squareIndex1, int squareIndex2)
        {
            return squareIndex1 + squareIndex2 * ChessConstants.SquareCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Bitboard GetConnectionInternal(int squareIndex1, int squareIndex2)
        {
            var index = GetConnectionIndex(squareIndex1, squareIndex2);
            return Connections[index];
        }

        private static Bitboard GetSlidingAttackers(
            int targetSquareIndex,
            Bitboard opponentSlidingPieces,
            Bitboard[] slidingAttacks,
            Bitboard emptySquareBitboard,
            bool findFirstAttackOnly)
        {
            if (!opponentSlidingPieces.IsAny)
            {
                return Bitboard.None;
            }

            var result = Bitboard.None;

            var slidingAttack = slidingAttacks[targetSquareIndex];
            var attackingSlidingPieces = slidingAttack & opponentSlidingPieces;

            var currentValue = attackingSlidingPieces.InternalValue;
            while (currentValue != Bitboard.NoneValue)
            {
                var attackerBitboard = Bitboard.PopFirstSquareBitboardInternal(ref currentValue);
                var attackerSquareIndex = Bitboard.FindFirstSquareIndexInternal(attackerBitboard);
                var connection = GetConnectionInternal(targetSquareIndex, attackerSquareIndex);
                if ((emptySquareBitboard & connection) != connection)
                {
                    continue;
                }

                result |= new Bitboard(attackerBitboard);
                if (findFirstAttackOnly)
                {
                    return result;
                }
            }

            return result;
        }

        private static void PopulateSimpleMoves(
            ICollection<GameMoveData> resultMoves,
            Square sourceSquare,
            Bitboard destinationsBitboard,
            GameMoveFlags moveFlags)
        {
            while (destinationsBitboard.IsAny)
            {
                var targetSquareIndex = Bitboard.PopFirstSquareIndex(ref destinationsBitboard);

                var move = new GameMove(sourceSquare, new Square(targetSquareIndex));
                resultMoves.Add(new GameMoveData(move, moveFlags));
            }
        }

        private Bitboard GetAttackersInternal(
            Square targetSquare,
            GameSide attackingSide,
            bool findFirstAttackOnly)
        {
            var result = new Bitboard();

            var targetBitboard = targetSquare.Bitboard;
            var targetSquareIndex = targetSquare.SquareIndex;

            var pawns = PiecePosition[PieceType.Pawn.ToPiece(attackingSide)];
            if (pawns.IsAny)
            {
                ShiftDirection left;
                ShiftDirection right;
                if (attackingSide == GameSide.White)
                {
                    left = ShiftDirection.SouthEast;
                    right = ShiftDirection.SouthWest;
                }
                else
                {
                    left = ShiftDirection.NorthWest;
                    right = ShiftDirection.NorthEast;
                }

                var attackingPawns = (targetBitboard.Shift(left) | targetBitboard.Shift(right)) & pawns;
                result |= attackingPawns;
                if (findFirstAttackOnly && result.IsAny)
                {
                    return result;
                }
            }

            var knights = PiecePosition[PieceType.Knight.ToPiece(attackingSide)];
            if (knights.IsAny)
            {
                var knightAttacks = KnightAttacksOrMoves[targetSquareIndex];
                var attackingKnights = knightAttacks & knights;
                result |= attackingKnights;
                if (findFirstAttackOnly && result.IsAny)
                {
                    return result;
                }
            }

            var kings = PiecePosition[PieceType.King.ToPiece(attackingSide)];
            if (kings.IsAny)
            {
                var kingAttacks = KingAttacksOrMoves[targetSquareIndex];
                var attackingKings = kingAttacks & kings;
                result |= attackingKings;
                if (findFirstAttackOnly && result.IsAny)
                {
                    return result;
                }
            }

            var emptySquareBitboard = PiecePosition[Piece.None];

            var queens = PiecePosition[PieceType.Queen.ToPiece(attackingSide)];
            var rooks = PiecePosition[PieceType.Rook.ToPiece(attackingSide)];

            var opponentSlidingStraightPieces = queens | rooks;

            var slidingStraightAttackers =
                GetSlidingAttackers(
                    targetSquareIndex,
                    opponentSlidingStraightPieces,
                    StraightSlidingAttacks,
                    emptySquareBitboard,
                    findFirstAttackOnly);

            result |= slidingStraightAttackers;
            if (findFirstAttackOnly && result.IsAny)
            {
                return result;
            }

            var bishops = PiecePosition[PieceType.Bishop.ToPiece(attackingSide)];

            var opponentSlidingDiagonallyPieces = queens | bishops;

            var slidingDiagonallyAttackers =
                GetSlidingAttackers(
                    targetSquareIndex,
                    opponentSlidingDiagonallyPieces,
                    DiagonallySlidingAttacks,
                    emptySquareBitboard,
                    findFirstAttackOnly);

            result |= slidingDiagonallyAttackers;

            return result;
        }

        #endregion
    }
}