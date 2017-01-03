using System;
using System.Collections.Generic;
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
    public abstract class GamePosition
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
        private static readonly InternalCastlingInfo[] KingCastlingInfos = InitializeKingCastlingInfos();

        protected static readonly Bitboard[] Connections = InitializeConnections();

        protected static readonly Bitboard[] DefaultPinLimitations =
            Enumerable.Repeat(Bitboard.Everything, ChessConstants.SquareCount).ToArray();

        #endregion

        #region Constructors

        protected GamePosition([NotNull] PiecePosition piecePosition, GameSide activeSide, int fullMoveIndex)
        {
            if (piecePosition == null)
            {
                throw new ArgumentNullException(nameof(piecePosition));
            }

            if (fullMoveIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fullMoveIndex),
                    fullMoveIndex,
                    @"The value must be positive.");
            }

            PiecePosition = piecePosition;
            ActiveSide = activeSide;
            FullMoveIndex = fullMoveIndex;
        }

        protected GamePosition([NotNull] GamePosition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var otherType = other.GetType();
            if (otherType != GetType())
            {
                throw new ArgumentException($@"Invalid object type '{otherType.GetFullName()}'.", nameof(other));
            }

            PiecePosition = other.PiecePosition.Copy();
            ActiveSide = other.ActiveSide;
            FullMoveIndex = other.FullMoveIndex;
        }

        #endregion

        #region Public Properties

        public abstract long ZobristKey
        {
            get;
        }

        public GameSide ActiveSide
        {
            get;
        }

        public int FullMoveIndex
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

        public abstract GamePosition MakeMove(GameMove2 move);

        #endregion

        #region Protected Properties

        protected PiecePosition PiecePosition
        {
            get;
        }

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

        protected void GeneratePotentialKingMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes,
            Bitboard target,
            CastlingOptions allowedCastlingOptions)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            var kingPiece = side.ToPiece(PieceType.King);
            var kings = PiecePosition[kingPiece];

            while (kings.IsAny)
            {
                var kingSquareIndex = Bitboard.PopFirstSquareIndex(ref kings);

                var sourceSquare = new Square(kingSquareIndex);
                var moves = KingAttacksOrMoves[kingSquareIndex];
                var movesOnTarget = moves & target;

                if (moveTypes.IsAnySet(GeneratedMoveTypes.Capture))
                {
                    var enemies = PiecePosition[side.Invert()];
                    var captures = movesOnTarget & enemies;
                    PopulateSimpleMoves(resultMoves, sourceSquare, captures, GameMoveFlags.IsRegularCapture);
                }

                if (moveTypes.IsAnySet(GeneratedMoveTypes.Quiet))
                {
                    var emptySquares = PiecePosition[Piece.None];
                    var nonCaptures = movesOnTarget & emptySquares;
                    PopulateSimpleMoves(resultMoves, sourceSquare, nonCaptures, GameMoveFlags.None);

                    var nonEmptySquares = ~emptySquares;

                    PopulateKingCastlingMoves(
                        resultMoves,
                        sourceSquare,
                        target,
                        allowedCastlingOptions,
                        nonEmptySquares,
                        CastlingSide.KingSide.ToCastlingType(side));

                    PopulateKingCastlingMoves(
                        resultMoves,
                        sourceSquare,
                        target,
                        allowedCastlingOptions,
                        nonEmptySquares,
                        CastlingSide.QueenSide.ToCastlingType(side));
                }
            }
        }

        protected void GeneratePotentialKnightMoves(
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

        protected void GeneratePotentialPawnMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes,
            Bitboard target,
            Bitboard enPassantCaptureTarget)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            var pawnPiece = side.ToPiece(PieceType.Pawn);
            var pawns = PiecePosition[pawnPiece];
            if (pawns.IsNone)
            {
                return;
            }

            var rank8 = side == GameSide.White ? Bitboards.Rank8 : Bitboards.Rank1;

            if (moveTypes.IsAnySet(GeneratedMoveTypes.Quiet))
            {
                var forwardDirection = side == GameSide.White ? ShiftDirection.North : ShiftDirection.South;
                var emptySquares = PiecePosition[Piece.None];
                var pushes = pawns.Shift(forwardDirection) & emptySquares;

                var targetPushes = pushes & target;
                if (targetPushes.IsAny)
                {
                    var nonPromotionPushes = targetPushes & ~rank8;
                    PopulatePawnMoves(resultMoves, nonPromotionPushes, (int)forwardDirection, GameMoveFlags.None);

                    var promotionPushes = targetPushes & rank8;
                    PopulatePawnMoves(
                        resultMoves,
                        promotionPushes,
                        (int)forwardDirection,
                        GameMoveFlags.IsPawnPromotion);
                }

                if (pushes.IsAny)
                {
                    var rank3 = side == GameSide.White ? Bitboards.Rank3 : Bitboards.Rank6;
                    var doublePushes = (pushes & rank3).Shift(forwardDirection) & emptySquares & target;
                    PopulatePawnMoves(
                        resultMoves,
                        doublePushes,
                        (int)forwardDirection << 1,
                        GameMoveFlags.None);
                }
            }

            if (moveTypes.IsAnySet(GeneratedMoveTypes.Capture))
            {
                var enemies = PiecePosition[side.Invert()];
                var enemyTargets = enemies & target;

                var leftCaptureOffset = side == GameSide.White
                    ? ShiftDirection.NorthWest
                    : ShiftDirection.SouthEast;
                PopulatePawnCaptures(
                    resultMoves,
                    pawns,
                    enemyTargets,
                    leftCaptureOffset,
                    rank8,
                    enPassantCaptureTarget);

                var rightCaptureOffset = side == GameSide.White
                    ? ShiftDirection.NorthEast
                    : ShiftDirection.SouthWest;
                PopulatePawnCaptures(
                    resultMoves,
                    pawns,
                    enemyTargets,
                    rightCaptureOffset,
                    rank8,
                    enPassantCaptureTarget);
            }
        }

        protected void GeneratePotentialQueenMoves(
            [NotNull] List<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            GenerateSlidingPieceMoves(resultMoves, side, moveTypes, PieceType.Queen, QueenDirections);
        }

        protected void GeneratePotentialRookMoves(
            [NotNull] List<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            GenerateSlidingPieceMoves(resultMoves, side, moveTypes, PieceType.Rook, RookDirections);
        }

        protected void GeneratePotentialBishopMoves(
            [NotNull] List<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            GenerateSlidingPieceMoves(resultMoves, side, moveTypes, PieceType.Bishop, BishopDirections);
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCastlingTypeArrayIndexInternal(CastlingType castlingType)
        {
            return (int)castlingType;
        }

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

        private static InternalCastlingInfo[] InitializeKingCastlingInfos()
        {
            var castlingTypes = EnumFactotum.GetAllValues<CastlingType>();

            var result = new InternalCastlingInfo[(int)castlingTypes.Max() + 1];
            foreach (var castlingType in castlingTypes)
            {
                var info = ChessHelper.CastlingTypeToInfoMap[castlingType];

                var index = GetCastlingTypeArrayIndexInternal(castlingType);
                result[index] = new InternalCastlingInfo(
                    info.KingMove,
                    new Bitboard(info.EmptySquares.Concat(info.PassedSquare.AsArray())));
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

        private static void PopulatePawnMoves(
            ICollection<GameMoveData> resultMoves,
            Bitboard destinationsBitboard,
            int moveOffset,
            GameMoveFlags moveFlags)
        {
            var isPawnPromotion = (moveFlags & GameMoveFlags.IsPawnPromotion) != 0;

            while (destinationsBitboard.IsAny)
            {
                var targetSquareIndex = Bitboard.PopFirstSquareIndex(ref destinationsBitboard);

                var move = new GameMove(
                    new Square(targetSquareIndex - moveOffset),
                    new Square(targetSquareIndex));

                if (isPawnPromotion)
                {
                    var promotionMoves = move.MakeAllPromotions();
                    foreach (var promotionMove in promotionMoves)
                    {
                        resultMoves.Add(new GameMoveData(promotionMove, moveFlags));
                    }
                }
                else
                {
                    resultMoves.Add(new GameMoveData(move, moveFlags));
                }
            }
        }

        private static void PopulatePawnCaptures(
            ICollection<GameMoveData> resultMoves,
            Bitboard pawns,
            Bitboard enemies,
            ShiftDirection captureDirection,
            Bitboard rank8,
            Bitboard enPassantCaptureTarget)
        {
            var captureTargets = pawns.Shift(captureDirection);

            var enPassantCapture = captureTargets & enPassantCaptureTarget;
            PopulatePawnMoves(
                resultMoves,
                enPassantCapture,
                (int)captureDirection,
                GameMoveFlags.IsEnPassantCapture);

            var captures = captureTargets & enemies;
            if (captures.IsNone)
            {
                return;
            }

            var nonPromotionCaptures = captures & ~rank8;
            PopulatePawnMoves(
                resultMoves,
                nonPromotionCaptures,
                (int)captureDirection,
                GameMoveFlags.IsRegularCapture);

            var promotionCaptures = captures & rank8;
            PopulatePawnMoves(
                resultMoves,
                promotionCaptures,
                (int)captureDirection,
                GameMoveFlags.IsRegularCapture | GameMoveFlags.IsPawnPromotion);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PopulateKingCastlingMoves(
            ICollection<GameMoveData> resultMoves,
            Square sourceSquare,
            Bitboard target,
            CastlingOptions allowedCastlingOptions,
            Bitboard nonEmptySquares,
            CastlingType castlingType)
        {
            var option = castlingType.ToOption();
            if ((allowedCastlingOptions & option) == 0)
            {
                return;
            }

            var info = KingCastlingInfos[GetCastlingTypeArrayIndexInternal(castlingType)];
            if (info.KingMove.From != sourceSquare || (info.ExpectedEmptySquares & nonEmptySquares).IsAny
                || (info.KingMove.To.Bitboard & target).IsNone)
            {
                return;
            }

            var moveData = new GameMoveData(info.KingMove, GameMoveFlags.IsKingCastling);
            resultMoves.Add(moveData);
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

        private void GenerateSlidingPieceMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes,
            PieceType pieceType,
            [NotNull] ShiftDirection[] directions)
        {
            var piece = pieceType.ToPiece(side);
            var pieces = PiecePosition[piece];

            var emptySquares = PiecePosition[Piece.None];
            var enemies = PiecePosition[side.Invert()];

            var shouldGenerateQuiets = moveTypes.IsAnySet(GeneratedMoveTypes.Quiet);
            var shouldGenerateCaptures = moveTypes.IsAnySet(GeneratedMoveTypes.Capture);

            //// TODO [vmcl] NEW-DESIGN: IDEA: Generate for all pieces rather for one by one
            while (pieces.IsAny)
            {
                var sourceSquareIndex = Bitboard.PopFirstSquareIndex(ref pieces);
                var sourceBitboard = Bitboard.FromSquareIndex(sourceSquareIndex);
                var sourceSquare = new Square(sourceSquareIndex);

                foreach (var direction in directions)
                {
                    var current = sourceBitboard;

                    while ((current = current.Shift(direction)).IsAny)
                    {
                        if ((current & emptySquares).IsAny)
                        {
                            if (shouldGenerateQuiets)
                            {
                                var move = new GameMove(sourceSquare, current.GetFirstSquare());
                                resultMoves.Add(new GameMoveData(move, GameMoveFlags.None));
                            }

                            continue;
                        }

                        if ((current & enemies).IsAny)
                        {
                            if (shouldGenerateCaptures)
                            {
                                var move = new GameMove(sourceSquare, current.GetFirstSquare());
                                resultMoves.Add(
                                    new GameMoveData(move, GameMoveFlags.IsRegularCapture));
                            }
                        }

                        break;
                    }
                }
            }
        }

        #endregion
    }
}