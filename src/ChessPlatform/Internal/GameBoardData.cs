using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum;
using Omnifactotum.Annotations;

//// ReSharper disable LoopCanBeConvertedToQuery - Using simpler loops for speed optimization
//// ReSharper disable ForCanBeConvertedToForeach - Using simpler loops for speed optimization
//// ReSharper disable ReturnTypeCanBeEnumerable.Local - Using simpler types (such as arrays) for speed optimization
//// ReSharper disable SuggestBaseTypeForParameter - Using specific types (such as arrays) for speed optimization

namespace ChessPlatform.Internal
{
    internal sealed class GameBoardData
    {
        private static readonly ShiftDirection[] AllDirections = EnumFactotum.GetAllValues<ShiftDirection>();
        private static readonly ShiftDirection[] QueenDirections = AllDirections;

        private static readonly ShiftDirection[] RookDirections =
        {
            ShiftDirection.North,
            ShiftDirection.South,
            ShiftDirection.East,
            ShiftDirection.West
        };

        private static readonly ShiftDirection[] BishopDirections =
        {
            ShiftDirection.NorthEast,
            ShiftDirection.NorthWest,
            ShiftDirection.SouthEast,
            ShiftDirection.SouthWest
        };

        private static readonly Bitboard[] StraightSlidingAttacks = InitializeStraightSlidingAttacks();
        private static readonly Bitboard[] DiagonallySlidingAttacks = InitializeDiagonallySlidingAttacks();
        private static readonly Bitboard[] KnightAttacksOrMoves = InitializeKnightAttacks();
        private static readonly Bitboard[] KingAttacksOrMoves = InitializeKingAttacksOrMoves();
        private static readonly InternalCastlingInfo[] KingCastlingInfos = InitializeKingCastlingInfos();

        private static readonly Bitboard[] Connections = InitializeConnections();

        private static readonly Bitboard[] DefaultPinLimitations =
            Enumerable.Repeat(Bitboard.Everything, ChessConstants.SquareCount).ToArray();

        private readonly Stack<MakeMoveData> _undoMoveDatas;

        internal GameBoardData()
        {
            PiecePosition = new PiecePosition();
            _undoMoveDatas = new Stack<MakeMoveData>();
        }

        private GameBoardData([NotNull] GameBoardData other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            PiecePosition = other.PiecePosition.Copy();
            _undoMoveDatas = new Stack<MakeMoveData>();
        }

        public PiecePosition PiecePosition
        {
            get;
        }

        public override string ToString()
        {
            return PiecePosition.GetFenSnippet();
        }

        public GameBoardData Copy()
        {
            return new GameBoardData(this);
        }

        public bool IsSamePosition([NotNull] GameBoardData other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return PiecePosition.IsSamePosition(other.PiecePosition);
        }

        [CanBeNull]
        public EnPassantCaptureInfo GetEnPassantCaptureInfo([NotNull] GameMove move)
        {
            if (move is null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            var piece = PiecePosition[move.From];
            var side = piece.GetSide();
            if (!side.HasValue || piece.GetPieceType() != PieceType.Pawn || move.From.File != move.To.File)
            {
                return null;
            }

            var enPassantInfo = ChessConstants.GameSideToDoublePushInfoMap[side.Value].EnsureNotNull();
            var isEnPassant = move.From.Rank == enPassantInfo.StartRank && move.To.Rank == enPassantInfo.EndRank;
            if (!isEnPassant)
            {
                return null;
            }

            var captureSquare = new Square(move.From.File, enPassantInfo.CaptureTargetRank);
            var targetPieceSquare = new Square(move.From.File, enPassantInfo.EndRank);

            return new EnPassantCaptureInfo(captureSquare, targetPieceSquare);
        }

        public bool IsEnPassantCapture(
            Square source,
            Square destination,
            EnPassantCaptureInfo enPassantCaptureInfo)
        {
            if (enPassantCaptureInfo is null || enPassantCaptureInfo.CaptureSquare != destination)
            {
                return false;
            }

            var piece = PiecePosition[source];
            if (piece.GetPieceType() != PieceType.Pawn)
            {
                return false;
            }

            var targetPiece = PiecePosition[enPassantCaptureInfo.TargetPieceSquare];
            return targetPiece.GetPieceType() == PieceType.Pawn;
        }

        public bool IsPawnPromotion(Square from, Square to)
        {
            var fromBitboard = from.Bitboard;
            var toBitboard = to.Bitboard;

            var whitePawns = PiecePosition[Piece.WhitePawn];
            var blackPawns = PiecePosition[Piece.BlackPawn];

            //// ReSharper disable ArrangeRedundantParentheses
            return ((fromBitboard & whitePawns).IsAny && (toBitboard & Bitboards.Rank8).IsAny)
                || ((fromBitboard & blackPawns).IsAny && (toBitboard & Bitboards.Rank1).IsAny);
            //// ReSharper restore ArrangeRedundantParentheses
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CastlingInfo CheckCastlingMove([NotNull] GameMove move)
        {
            var piece = PiecePosition[move.From];

            return piece.Is(PieceType.King)
                ? ChessHelper.KingMoveToCastlingInfoMap.GetValueOrDefault(move)
                : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard GetAttackers(Square targetSquare, GameSide attackingSide)
        {
            return GetAttackersInternal(targetSquare, attackingSide, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUnderAttack(Square targetSquare, GameSide attackingSide)
        {
            var bitboard = GetAttackersInternal(targetSquare, attackingSide, true);
            return bitboard.IsAny;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyUnderAttack(
            [NotNull] IEnumerable<Square> targetSquares,
            GameSide attackingSide)
        {
            if (targetSquares is null)
            {
                throw new ArgumentNullException(nameof(targetSquares));
            }

            var result = targetSquares.Any(targetSquare => IsUnderAttack(targetSquare, attackingSide));
            return result;
        }

        public Bitboard[] GetPinLimitations(int valuablePieceSquareIndex, GameSide attackingSide)
        {
            var result = DefaultPinLimitations.Copy();

            var enemyPieces = PiecePosition[attackingSide];
            var ownPieces = PiecePosition[attackingSide.Invert()];

            var queens = PiecePosition[attackingSide.ToPiece(PieceType.Queen)];
            var bishops = PiecePosition[attackingSide.ToPiece(PieceType.Bishop)];
            var rooks = PiecePosition[attackingSide.ToPiece(PieceType.Rook)];

            PopulatePinLimitations(
                result,
                enemyPieces,
                valuablePieceSquareIndex,
                ownPieces,
                DiagonallySlidingAttacks,
                queens | bishops);

            PopulatePinLimitations(
                result,
                enemyPieces,
                valuablePieceSquareIndex,
                ownPieces,
                StraightSlidingAttacks,
                queens | rooks);

            return result;
        }

        public bool IsInCheck(GameSide side)
        {
            var king = side.ToPiece(PieceType.King);
            var oppositeSide = side.Invert();
            var kingSquares = PiecePosition[king].GetSquares();
            return kingSquares.Length != 0 && IsAnyUnderAttack(kingSquares, oppositeSide);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInsufficientMaterialState()
        {
            return IsKingLeftOnly(GameSide.White) && IsKingLeftOnly(GameSide.Black);
        }

        public Square[] GetPotentialMoveSquares(
            CastlingOptions castlingOptions,
            [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo,
            Square sourceSquare)
        {
            var piece = PiecePosition[sourceSquare];
            var pieceType = piece.GetPieceType();

            if (pieceType == PieceType.None)
            {
                throw new ArgumentException(
                    $@"No piece at the source square '{sourceSquare}'.",
                    nameof(sourceSquare));
            }

            var pieceSide = piece.GetSide().EnsureNotNull();

            switch (pieceType)
            {
                case PieceType.Knight:
                    //// TODO [HarinezumiSama] Use bitboard instead of squares
                    var result = ChessHelper.GetKnightMoveSquares(sourceSquare)
                        .Where(square => PiecePosition[square].GetSide() != pieceSide)
                        .ToArray();

                    return result;

                case PieceType.King:
                case PieceType.Pawn:
                    throw new InvalidOperationException("MUST NOT go into this branch anymore.");

                case PieceType.None:
                    throw new ArgumentException(
                        $@"No piece at the source square '{sourceSquare}'.",
                        nameof(sourceSquare));

                case PieceType.Bishop:
                case PieceType.Rook:
                case PieceType.Queen:
                    // Just go ahead
                    break;

                default:
                    throw pieceType.CreateEnumValueNotImplementedException();
            }

            var resultList = new List<Square>();

            if (pieceType.IsSlidingStraight())
            {
                GetPotentialMoveSquaresByRays(
                    sourceSquare,
                    pieceSide,
                    ChessHelper.StraightRays,
                    ChessHelper.MaxSlidingPieceDistance,
                    true,
                    resultList);
            }

            if (pieceType.IsSlidingDiagonally())
            {
                GetPotentialMoveSquaresByRays(
                    sourceSquare,
                    pieceSide,
                    ChessHelper.DiagonalRays,
                    ChessHelper.MaxSlidingPieceDistance,
                    true,
                    resultList);
            }

            return resultList.ToArray();
        }

        public void GeneratePawnMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes,
            Bitboard enPassantCaptureTarget,
            Bitboard target)
        {
            if (resultMoves is null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

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

        public void GenerateKingMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            CastlingOptions allowedCastlingOptions,
            Bitboard target)
        {
            if (resultMoves is null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            var kingPiece = side.ToPiece(PieceType.King);
            var king = PiecePosition[kingPiece];
            if (king.IsNone)
            {
                return;
            }

            if (!king.IsExactlyOneSquare())
            {
                throw new ChessPlatformException(
                    $@"There are multiple {kingPiece.GetDescription()} pieces ({king.GetSquareCount()}) on the board.");
            }

            var kingSquareIndex = king.FindFirstSquareIndex();
            var sourceSquare = new Square(kingSquareIndex);
            var directTargets = KingAttacksOrMoves[kingSquareIndex] & target;

            var emptySquares = PiecePosition[Piece.None];
            var nonCaptures = directTargets & emptySquares;
            PopulateSimpleMoves(resultMoves, sourceSquare, nonCaptures, GameMoveFlags.None);

            var enemies = PiecePosition[side.Invert()];
            var captures = directTargets & enemies;
            PopulateSimpleMoves(resultMoves, sourceSquare, captures, GameMoveFlags.IsRegularCapture);

            var nonEmptySquares = ~emptySquares;

            PopulateKingCastlingMoves(
                resultMoves,
                sourceSquare,
                allowedCastlingOptions,
                nonEmptySquares,
                CastlingSide.KingSide.ToCastlingType(side));

            PopulateKingCastlingMoves(
                resultMoves,
                sourceSquare,
                allowedCastlingOptions,
                nonEmptySquares,
                CastlingSide.QueenSide.ToCastlingType(side));
        }

        public void GenerateKnightMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes,
            Bitboard target)
        {
            if (resultMoves is null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

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

        public void GenerateQueenMoves(
            [NotNull] List<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes)
        {
            if (resultMoves is null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            GenerateSlidingPieceMoves(resultMoves, side, moveTypes, PieceType.Queen, QueenDirections);
        }

        public void GenerateRookMoves(
            [NotNull] List<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes)
        {
            if (resultMoves is null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            GenerateSlidingPieceMoves(resultMoves, side, moveTypes, PieceType.Rook, RookDirections);
        }

        public void GenerateBishopMoves(
            [NotNull] List<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes)
        {
            if (resultMoves is null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            GenerateSlidingPieceMoves(resultMoves, side, moveTypes, PieceType.Bishop, BishopDirections);
        }

        internal MakeMoveData MakeMove(
            [NotNull] GameMove move,
            GameSide movingSide,
            [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo,
            ref CastlingOptions castlingOptions)
        {
            if (move is null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            var piece = PiecePosition[move.From];
            if (piece == Piece.None || piece.GetSide() != movingSide)
            {
                throw new ArgumentException($@"Invalid move '{move}' in the position.", nameof(move));
            }

            GameMove castlingRookMove = null;
            Square? enPassantCapturedPieceSquare = null;

            var movingSideAllCastlingOptions = ChessHelper.GameSideToCastlingOptionsMap[movingSide];

            // Performing checks before actual move!
            var castlingInfo = CheckCastlingMove(move);
            var isEnPassantCapture = IsEnPassantCapture(move.From, move.To, enPassantCaptureInfo);
            var isPawnPromotion = IsPawnPromotion(move.From, move.To);

            var moveData = MovePieceInternal(move);
            var capturedPiece = moveData.CapturedPiece;

            if (isEnPassantCapture)
            {
                if (enPassantCaptureInfo is null)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }

                enPassantCapturedPieceSquare = enPassantCaptureInfo.TargetPieceSquare;
                capturedPiece = PiecePosition.SetPiece(enPassantCaptureInfo.TargetPieceSquare, Piece.None);
                if (capturedPiece.GetPieceType() != PieceType.Pawn)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }
            }
            else if (isPawnPromotion)
            {
                if (move.PromotionResult == PieceType.None)
                {
                    throw new ChessPlatformException($@"Promoted piece type is not specified ({move}).");
                }

                var previousPiece = PiecePosition.SetPiece(move.To, move.PromotionResult.ToPiece(movingSide));
                if (previousPiece.GetPieceType() != PieceType.Pawn)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }
            }
            else if (castlingInfo != null)
            {
                if (!castlingOptions.IsAllSet(castlingInfo.Option))
                {
                    throw new ChessPlatformException(
                        $@"The castling {{{move}}} ({castlingInfo.CastlingType.GetName()}) is not allowed.");
                }

                castlingRookMove = castlingInfo.RookMove;
                var rookMoveData = MovePieceInternal(castlingRookMove);
                if (rookMoveData.CapturedPiece != Piece.None)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }

                castlingOptions &= ~movingSideAllCastlingOptions;
            }

            var movingSideCurrentCastlingOptions = castlingOptions & movingSideAllCastlingOptions;
            if (movingSideCurrentCastlingOptions != CastlingOptions.None)
            {
                switch (piece.GetPieceType())
                {
                    case PieceType.King:
                        castlingOptions &= ~movingSideAllCastlingOptions;
                        break;

                    case PieceType.Rook:
                        {
                            var castlingInfoByRook =
                                ChessConstants.AllCastlingInfos.SingleOrDefault(obj => obj.RookMove.From == move.From);

                            if (castlingInfoByRook != null)
                            {
                                castlingOptions &= ~castlingInfoByRook.Option;
                            }
                        }

                        break;
                }
            }

            var oppositeSide = movingSide.Invert();
            var oppositeSideAllCastlingOptions = ChessHelper.GameSideToCastlingOptionsMap[oppositeSide];
            var oppositeSideCurrentCastlingOptions = castlingOptions & oppositeSideAllCastlingOptions;
            if (oppositeSideCurrentCastlingOptions != CastlingOptions.None
                && capturedPiece.GetPieceType() == PieceType.Rook)
            {
                var oppositeCastlingInfo =
                    ChessConstants.AllCastlingInfos.SingleOrDefault(obj => obj.RookMove.From == move.To);

                if (oppositeCastlingInfo != null)
                {
                    castlingOptions &= ~oppositeCastlingInfo.Option;
                }
            }

            var undoMoveData = new MakeMoveData(
                move,
                moveData.MovedPiece,
                capturedPiece,
                castlingRookMove,
                enPassantCapturedPieceSquare);

            _undoMoveDatas.Push(undoMoveData);

            PiecePosition.EnsureConsistency();

            return undoMoveData;
        }

        internal bool CanUndoMove()
        {
            return _undoMoveDatas.Count != 0;
        }

        internal void UndoMove()
        {
            if (!CanUndoMove())
            {
                throw new InvalidOperationException("Undo cannot be performed: no moves.");
            }

            var data = _undoMoveDatas.Pop();

            PiecePosition.SetPiece(data.Move.From, data.MovedPiece);
            PiecePosition.SetPiece(data.Move.To, Piece.None);

            if (data.CapturedPiece != Piece.None)
            {
                PiecePosition.SetPiece(data.CapturedPieceSquare, data.CapturedPiece);
            }
            else if (data.CastlingRookMove != null)
            {
                var castlingRook = PiecePosition.SetPiece(data.CastlingRookMove.To, Piece.None);
                if (castlingRook.GetPieceType() != PieceType.Rook
                    || castlingRook.GetSide() != data.MovedPiece.GetSide())
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }

                PiecePosition.SetPiece(data.CastlingRookMove.From, castlingRook);
            }

            PiecePosition.EnsureConsistency();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCastlingTypeArrayIndexInternal(CastlingType castlingType)
        {
            return (int)castlingType;
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
                var connection = GetConnection(targetSquareIndex, attackerSquareIndex);
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

        private static void PopulateKingCastlingMoves(
            ICollection<GameMoveData> resultMoves,
            Square sourceSquare,
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
            if (info.KingMove.From != sourceSquare || (nonEmptySquares & info.ExpectedEmptySquares).IsAny)
            {
                return;
            }

            var moveData = new GameMoveData(info.KingMove, GameMoveFlags.IsKingCastling);
            resultMoves.Add(moveData);
        }

        private static void PopulatePinLimitations(
            Bitboard[] pinLimitations,
            Bitboard enemyPieces,
            int squareIndex,
            Bitboard ownPieces,
            Bitboard[] slidingAttacks,
            Bitboard slidingPieces)
        {
            var slidingAttack = slidingAttacks[squareIndex];
            var potentialPinners = slidingPieces & slidingAttack;

            var current = potentialPinners;
            while (current.IsAny)
            {
                var attackerSquareIndex = Bitboard.PopFirstSquareIndex(ref current);
                var connection = GetConnection(squareIndex, attackerSquareIndex);

                var pinned = ownPieces & connection;
                var enemiesOnConnection = enemyPieces & connection;
                if (enemiesOnConnection.IsAny || !pinned.IsExactlyOneSquare())
                {
                    continue;
                }

                var pinnedSquareIndex = pinned.FindFirstSquareIndex();
                pinLimitations[pinnedSquareIndex] = connection | Bitboard.FromSquareIndex(attackerSquareIndex);
            }
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

        private static Bitboard[] InitializeKnightAttacks()
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
            Debug.Assert(squareIndex1 >= 0, "squareIndex1 must be non-negative.");
            Debug.Assert(squareIndex2 >= 0, "squareIndex2 must be non-negative.");

            return squareIndex1 + squareIndex2 * ChessConstants.SquareCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Bitboard GetConnection(int squareIndex1, int squareIndex2)
        {
            var index = GetConnectionIndex(squareIndex1, squareIndex2);
            return Connections[index];
        }

        private MovePieceData MovePieceInternal(GameMove move)
        {
            if (move is null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            var movedPiece = PiecePosition.SetPiece(move.From, Piece.None);
            if (movedPiece == Piece.None)
            {
                throw new ChessPlatformException($@"The source square of the move {{{move}}} is empty.");
            }

            var capturedPiece = PiecePosition.SetPiece(move.To, movedPiece);
            if (capturedPiece != Piece.None && capturedPiece.GetSide() == movedPiece.GetSide())
            {
                throw new ChessPlatformException("Cannot capture an own piece.");
            }

            return new MovePieceData(movedPiece, capturedPiece);
        }

        private void GetPotentialMoveSquaresByRays(
            Square sourceSquare,
            GameSide sourceSide,
            IEnumerable<SquareShift> rays,
            int maxDistance,
            bool allowCapturing,
            ICollection<Square> resultCollection)
        {
            foreach (var ray in rays)
            {
                var distance = 1;
                for (var square = sourceSquare + ray;
                    square.HasValue && distance <= maxDistance;
                    square = square.Value + ray, distance++)
                {
                    var currentSquare = square.Value;

                    var piece = PiecePosition[currentSquare];
                    var currentSide = piece.GetSide();
                    if (piece == Piece.None || !currentSide.HasValue)
                    {
                        resultCollection.Add(currentSquare);
                        continue;
                    }

                    if (currentSide.Value != sourceSide && allowCapturing)
                    {
                        resultCollection.Add(currentSquare);
                    }

                    break;
                }
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

            var opponentPawns = PiecePosition[attackingSide.ToPiece(PieceType.Pawn)];
            if (opponentPawns.IsAny)
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

                var attackingPawns = (targetBitboard.Shift(left) | targetBitboard.Shift(right)) & opponentPawns;
                result |= attackingPawns;
                if (findFirstAttackOnly && result.IsAny)
                {
                    return result;
                }
            }

            var opponentKnights = PiecePosition[attackingSide.ToPiece(PieceType.Knight)];
            if (opponentKnights.IsAny)
            {
                var knightAttacks = KnightAttacksOrMoves[targetSquareIndex];
                var attackingKnights = knightAttacks & opponentKnights;
                result |= attackingKnights;
                if (findFirstAttackOnly && result.IsAny)
                {
                    return result;
                }
            }

            var opponentKings = PiecePosition[attackingSide.ToPiece(PieceType.King)];
            if (opponentKings.IsAny)
            {
                var kingAttacks = KingAttacksOrMoves[targetSquareIndex];
                var attackingKings = kingAttacks & opponentKings;
                result |= attackingKings;
                if (findFirstAttackOnly && result.IsAny)
                {
                    return result;
                }
            }

            var emptySquareBitboard = PiecePosition[Piece.None];

            var opponentQueens = PiecePosition[attackingSide.ToPiece(PieceType.Queen)];
            var opponentRooks = PiecePosition[attackingSide.ToPiece(PieceType.Rook)];

            var opponentSlidingStraightPieces = opponentQueens | opponentRooks;
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

            var opponentBishops = PiecePosition[attackingSide.ToPiece(PieceType.Bishop)];

            var opponentSlidingDiagonallyPieces = opponentQueens | opponentBishops;
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

        private bool IsKingLeftOnly(GameSide side)
        {
            var sideBitboard = PiecePosition[side];
            var kingBitboard = PiecePosition[side.ToPiece(PieceType.King)];
            var otherPieces = sideBitboard & ~kingBitboard;
            return otherPieces.IsNone;
        }

        private void GenerateSlidingPieceMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            GameSide side,
            GeneratedMoveTypes moveTypes,
            PieceType pieceType,
            ShiftDirection[] directions)
        {
            var piece = pieceType.ToPiece(side);
            var pieces = PiecePosition[piece];

            var emptySquares = PiecePosition[Piece.None];
            var enemies = PiecePosition[side.Invert()];

            var shouldGenerateQuiets = moveTypes.IsAnySet(GeneratedMoveTypes.Quiet);
            var shouldGenerateCaptures = moveTypes.IsAnySet(GeneratedMoveTypes.Capture);

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
    }
}