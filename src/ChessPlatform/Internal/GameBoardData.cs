using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal sealed class GameBoardData
    {
        #region Constants and Fields

        private static readonly int PieceArrayLength = ChessConstants.Pieces.Max(item => (int)item) + 1;
        private static readonly int ColorArrayLength = ChessConstants.PieceColors.Max(item => (int)item) + 1;

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

        private readonly Bitboard[] _bitboards;
        private readonly Bitboard[] _entireColorBitboards;
        private readonly Piece[] _pieces;

        #endregion

        #region Constructors

        internal GameBoardData()
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            _undoMoveDatas = new Stack<MakeMoveData>();
            _pieces = new Piece[ChessConstants.X88Length];

            _bitboards = new Bitboard[PieceArrayLength];
            _bitboards[GetPieceArrayIndexInternal(Piece.None)] = Bitboard.Everything;

            _entireColorBitboards = new Bitboard[ColorArrayLength];
            ZobristKey = ComputeZobristKey();
        }

        private GameBoardData(GameBoardData other)
        {
            #region Argument Check

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            #endregion

            _undoMoveDatas = new Stack<MakeMoveData>();
            _pieces = other._pieces.Copy();
            _bitboards = other._bitboards.Copy();
            _entireColorBitboards = other._entireColorBitboards.Copy();
            ZobristKey = other.ZobristKey;
        }

        #endregion

        #region Public Properties

        public Piece this[Position position]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _pieces[position.X88Value];
            }
        }

        public long ZobristKey
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.GetFenSnippet();
        }

        public void EnsureConsistency()
        {
            if (!DebugConstants.EnsurePieceDataConsistency)
            {
                return;
            }

            EnsureConsistencyInternal();
        }

        public GameBoardData Copy()
        {
            return new GameBoardData(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PieceInfo GetPieceInfo(Position position)
        {
            var piece = this[position];
            return piece.GetPieceInfo();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard GetBitboard(Piece piece)
        {
            var index = GetPieceArrayIndexInternal(piece);

            var bitboard = _bitboards[index];
            return bitboard;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard GetBitboard(PieceColor color)
        {
            var bitboard = _entireColorBitboards[GetColorArrayIndexInternal(color)];
            return bitboard;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Position[] GetPositions(Piece piece)
        {
            var bitboard = GetBitboard(piece);
            return bitboard.GetPositions();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Position[] GetPositions(PieceColor color)
        {
            var bitboard = GetBitboard(color);
            return bitboard.GetPositions();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPieceCount(Piece piece)
        {
            var bitboard = GetBitboard(piece);
            return bitboard.GetBitSetCount();
        }

        [CanBeNull]
        public EnPassantCaptureInfo GetEnPassantCaptureInfo([NotNull] GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            var pieceInfo = GetPieceInfo(move.From);
            if (!pieceInfo.Color.HasValue || pieceInfo.PieceType != PieceType.Pawn || move.From.File != move.To.File)
            {
                return null;
            }

            var enPassantInfo = ChessConstants.ColorToEnPassantInfoMap[pieceInfo.Color.Value].EnsureNotNull();
            var isEnPassant = move.From.Rank == enPassantInfo.StartRank && move.To.Rank == enPassantInfo.EndRank;
            if (!isEnPassant)
            {
                return null;
            }

            var capturePosition = new Position(false, move.From.File, enPassantInfo.CaptureTargetRank);
            var targetPiecePosition = new Position(false, move.From.File, enPassantInfo.EndRank);

            return new EnPassantCaptureInfo(capturePosition, targetPiecePosition);
        }

        public bool IsEnPassantCapture(
            Position source,
            Position destination,
            EnPassantCaptureInfo enPassantCaptureInfo)
        {
            if (enPassantCaptureInfo == null || enPassantCaptureInfo.CapturePosition != destination)
            {
                return false;
            }

            var pieceInfo = GetPieceInfo(source);
            if (pieceInfo.PieceType != PieceType.Pawn)
            {
                return false;
            }

            var targetPieceInfo = GetPieceInfo(enPassantCaptureInfo.TargetPiecePosition);
            return targetPieceInfo.PieceType == PieceType.Pawn;
        }

        public bool IsPawnPromotion(Position from, Position to)
        {
            var fromBitboard = from.Bitboard;
            var toBitboard = to.Bitboard;

            var whitePawns = GetBitboard(Piece.WhitePawn);
            var blackPawns = GetBitboard(Piece.BlackPawn);

            return ((fromBitboard & whitePawns).IsAny && (toBitboard & Bitboards.Rank8).IsAny)
                || ((fromBitboard & blackPawns).IsAny && (toBitboard & Bitboards.Rank1).IsAny);
        }

        public CastlingInfo CheckCastlingMove([NotNull] GameMove move)
        {
            var pieceInfo = GetPieceInfo(move.From);
            if (pieceInfo.PieceType != PieceType.King || !pieceInfo.Color.HasValue)
            {
                return null;
            }

            return ChessHelper.KingMoveToCastlingInfoMap.GetValueOrDefault(move);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard GetAttackers(Position targetPosition, PieceColor attackingColor)
        {
            return GetAttackersInternal(targetPosition, attackingColor, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUnderAttack(Position targetPosition, PieceColor attackingColor)
        {
            var bitboard = GetAttackersInternal(targetPosition, attackingColor, true);
            return bitboard.IsAny;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnyUnderAttack(
            [NotNull] IEnumerable<Position> targetPositions,
            PieceColor attackingColor)
        {
            #region Argument Check

            if (targetPositions == null)
            {
                throw new ArgumentNullException(nameof(targetPositions));
            }

            #endregion

            var result = targetPositions.Any(targetPosition => IsUnderAttack(targetPosition, attackingColor));
            return result;
        }

        public Bitboard[] GetPinLimitations(int valuablePieceSquareIndex, PieceColor attackingColor)
        {
            var result = DefaultPinLimitations.Copy();

            var enemyPieces = GetBitboard(attackingColor);
            var ownPieces = GetBitboard(attackingColor.Invert());

            var queens = GetBitboard(PieceType.Queen.ToPiece(attackingColor));
            var bishops = GetBitboard(PieceType.Bishop.ToPiece(attackingColor));
            var rooks = GetBitboard(PieceType.Rook.ToPiece(attackingColor));

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

        public bool IsInCheck(PieceColor kingColor)
        {
            var king = PieceType.King.ToPiece(kingColor);
            var oppositeColor = kingColor.Invert();
            var kingPositions = GetPositions(king);
            return kingPositions.Length != 0 && kingPositions.Any(position => IsUnderAttack(position, oppositeColor));
            ////return kingPositions.Length > 0 && IsUnderAttack(kingPositions[0], oppositeColor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInsufficientMaterialState()
        {
            return IsKingLeftOnly(PieceColor.White) && IsKingLeftOnly(PieceColor.Black);
        }

        public Position[] GetPotentialMovePositions(
            CastlingOptions castlingOptions,
            [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo,
            Position sourcePosition)
        {
            var pieceInfo = GetPieceInfo(sourcePosition);
            if (pieceInfo.PieceType == PieceType.None || !pieceInfo.Color.HasValue)
            {
                throw new ArgumentException("No piece at the source position.", nameof(sourcePosition));
            }

            var pieceColor = pieceInfo.Color.Value;

            if (pieceInfo.PieceType == PieceType.Knight)
            {
                var result = ChessHelper.GetKnightMovePositions(sourcePosition)
                    .Where(position => GetPieceInfo(position).Color != pieceColor)
                    .ToArray();

                return result;
            }

            if (pieceInfo.PieceType == PieceType.King)
            {
                throw new InvalidOperationException("MUST NOT go into this branch anymore.");
            }

            if (pieceInfo.PieceType == PieceType.Pawn)
            {
                throw new InvalidOperationException("MUST NOT go into this branch anymore.");
            }

            var resultList = new List<Position>();

            if (pieceInfo.PieceType.IsSlidingStraight())
            {
                GetPotentialMovePositionsByRays(
                    sourcePosition,
                    pieceColor,
                    ChessHelper.StraightRays,
                    ChessHelper.MaxSlidingPieceDistance,
                    true,
                    resultList);
            }

            if (pieceInfo.PieceType.IsSlidingDiagonally())
            {
                GetPotentialMovePositionsByRays(
                    sourcePosition,
                    pieceColor,
                    ChessHelper.DiagonalRays,
                    ChessHelper.MaxSlidingPieceDistance,
                    true,
                    resultList);
            }

            return resultList.ToArray();
        }

        public void GeneratePawnMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            PieceColor color,
            GeneratedMoveTypes moveTypes,
            Bitboard enPassantCaptureTarget,
            Bitboard target)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            var pawnPiece = PieceType.Pawn.ToPiece(color);
            var pawns = GetBitboard(pawnPiece);
            if (pawns.IsNone)
            {
                return;
            }

            var rank8 = color == PieceColor.White ? Bitboards.Rank8 : Bitboards.Rank1;

            if (moveTypes.IsAnySet(GeneratedMoveTypes.Quiet))
            {
                var forwardDirection = color == PieceColor.White ? ShiftDirection.North : ShiftDirection.South;
                var emptySquares = GetBitboard(Piece.None);
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
                    var rank3 = color == PieceColor.White ? Bitboards.Rank3 : Bitboards.Rank6;
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
                var enemies = GetBitboard(color.Invert());
                var enemyTargets = enemies & target;

                var leftCaptureOffset = color == PieceColor.White
                    ? ShiftDirection.NorthWest
                    : ShiftDirection.SouthEast;
                PopulatePawnCaptures(
                    resultMoves,
                    pawns,
                    enemyTargets,
                    leftCaptureOffset,
                    rank8,
                    enPassantCaptureTarget);

                var rightCaptureOffset = color == PieceColor.White
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
            PieceColor color,
            CastlingOptions allowedCastlingOptions,
            Bitboard target)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            var kingPiece = PieceType.King.ToPiece(color);
            var king = GetBitboard(kingPiece);
            if (king.IsNone)
            {
                return;
            }

            if (!king.IsExactlyOneBitSet())
            {
                throw new ChessPlatformException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "There are multiple {0} pieces ({1}) on the board.",
                        kingPiece.GetDescription(),
                        king.GetBitSetCount()));
            }

            var kingSquareIndex = king.FindFirstBitSetIndex();
            var sourcePosition = Position.FromSquareIndex(kingSquareIndex);
            var directTargets = KingAttacksOrMoves[kingSquareIndex] & target;

            var emptySquares = GetBitboard(Piece.None);
            var nonCaptures = directTargets & emptySquares;
            PopulateSimpleMoves(resultMoves, sourcePosition, nonCaptures, GameMoveFlags.None);

            var enemies = GetBitboard(color.Invert());
            var captures = directTargets & enemies;
            PopulateSimpleMoves(resultMoves, sourcePosition, captures, GameMoveFlags.IsCapture);

            var nonEmptySquares = ~emptySquares;

            PopulateKingCastlingMoves(
                resultMoves,
                sourcePosition,
                allowedCastlingOptions,
                nonEmptySquares,
                CastlingSide.KingSide.ToCastlingType(color));

            PopulateKingCastlingMoves(
                resultMoves,
                sourcePosition,
                allowedCastlingOptions,
                nonEmptySquares,
                CastlingSide.QueenSide.ToCastlingType(color));
        }

        public void GenerateKnightMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            PieceColor color,
            GeneratedMoveTypes moveTypes,
            Bitboard target)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            var emptySquares = GetBitboard(Piece.None);
            var enemies = GetBitboard(color.Invert());

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

            var knightPiece = PieceType.Knight.ToPiece(color);
            var knights = GetBitboard(knightPiece);

            while (knights.IsAny)
            {
                var sourceSquareIndex = Bitboard.PopFirstBitSetIndex(ref knights);
                var moves = KnightAttacksOrMoves[sourceSquareIndex];
                var movesOnTarget = moves & actualTarget;
                if (movesOnTarget.IsNone)
                {
                    continue;
                }

                var sourcePosition = Position.FromSquareIndex(sourceSquareIndex);
                if (moveTypes.IsAnySet(GeneratedMoveTypes.Capture))
                {
                    var captures = movesOnTarget & enemies;
                    PopulateSimpleMoves(resultMoves, sourcePosition, captures, GameMoveFlags.IsCapture);
                }

                if (moveTypes.IsAnySet(GeneratedMoveTypes.Quiet))
                {
                    var nonCaptures = movesOnTarget & emptySquares;
                    PopulateSimpleMoves(resultMoves, sourcePosition, nonCaptures, GameMoveFlags.None);
                }
            }
        }

        public void GenerateQueenMoves(
            [NotNull] List<GameMoveData> resultMoves,
            PieceColor color,
            GeneratedMoveTypes moveTypes)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            GenerateSlidingPieceMoves(resultMoves, color, moveTypes, PieceType.Queen, QueenDirections);
        }

        public void GenerateRookMoves(
            [NotNull] List<GameMoveData> resultMoves,
            PieceColor color,
            GeneratedMoveTypes moveTypes)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            GenerateSlidingPieceMoves(resultMoves, color, moveTypes, PieceType.Rook, RookDirections);
        }

        public void GenerateBishopMoves(
            [NotNull] List<GameMoveData> resultMoves,
            PieceColor color,
            GeneratedMoveTypes moveTypes)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException(nameof(resultMoves));
            }

            #endregion

            GenerateSlidingPieceMoves(resultMoves, color, moveTypes, PieceType.Bishop, BishopDirections);
        }

        #endregion

        #region Internal Methods

        internal Piece SetPiece(Position position, Piece piece)
        {
            var x88Value = position.X88Value;
            var bitboardBit = position.Bitboard;

            var oldPiece = _pieces[x88Value];
            _pieces[x88Value] = piece;

            _bitboards[GetPieceArrayIndexInternal(oldPiece)] &= ~bitboardBit;

            var oldPieceColor = oldPiece.GetColor();
            if (oldPieceColor.HasValue)
            {
                _entireColorBitboards[GetColorArrayIndexInternal(oldPieceColor.Value)] &= ~bitboardBit;
            }

            _bitboards[GetPieceArrayIndexInternal(piece)] |= bitboardBit;

            var pieceColor = piece.GetColor();
            if (pieceColor.HasValue)
            {
                _entireColorBitboards[GetColorArrayIndexInternal(pieceColor.Value)] |= bitboardBit;
            }

            ZobristKey ^= ZobristHashHelper.GetPieceHash(position, oldPiece)
                ^ ZobristHashHelper.GetPieceHash(position, piece);

            return oldPiece;
        }

        internal void SetupNewPiece(Piece piece, Position position)
        {
            #region Argument Check

            if (piece == Piece.None)
            {
                throw new ArgumentException("Must be a piece rather than empty square.", nameof(piece));
            }

            #endregion

            var existingPiece = this[position];
            if (existingPiece != Piece.None)
            {
                throw new ChessPlatformException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The board square '{0}' is already occupied by '{1}'.",
                        position,
                        existingPiece));
            }

            SetPiece(position, piece);
        }

        internal void SetupByFenSnippet(string fenSnippet)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(fenSnippet))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(fenSnippet));
            }

            #endregion

            const string InvalidFenMessage = "Invalid FEN.";

            var currentRank = ChessConstants.RankCount - 1;
            var currentFile = 0;
            foreach (var ch in fenSnippet)
            {
                if (ch == ChessConstants.FenRankSeparator)
                {
                    if (currentFile != ChessConstants.FileCount)
                    {
                        throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                    }

                    currentFile = 0;
                    currentRank--;

                    if (currentRank < 0)
                    {
                        throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                    }

                    continue;
                }

                if (currentFile >= ChessConstants.FileCount)
                {
                    throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                }

                Piece piece;
                if (ChessConstants.FenCharToPieceMap.TryGetValue(ch, out piece))
                {
                    var position = new Position(Convert.ToByte(currentFile), Convert.ToByte(currentRank));
                    SetupNewPiece(piece, position);
                    currentFile++;
                    continue;
                }

                var emptySquareCount = byte.Parse(new string(ch, 1));
                if (emptySquareCount == 0)
                {
                    throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
                }

                currentFile += emptySquareCount;
            }

            if (currentFile != ChessConstants.FileCount)
            {
                throw new ArgumentException(InvalidFenMessage, nameof(fenSnippet));
            }
        }

        internal MakeMoveData MakeMove(
            [NotNull] GameMove move,
            PieceColor movingColor,
            [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo,
            ref CastlingOptions castlingOptions)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            var pieceInfo = GetPieceInfo(move.From);
            if (pieceInfo.PieceType == PieceType.None || pieceInfo.Color != movingColor)
            {
                throw new ArgumentException("Invalid move.", nameof(move));
            }

            #endregion

            GameMove castlingRookMove = null;
            Position? enPassantCapturedPiecePosition = null;

            var movingColorAllCastlingOptions = ChessHelper.ColorToCastlingOptionsMap[movingColor];

            // Performing checks before actual move!
            var castlingInfo = CheckCastlingMove(move);
            var isEnPassantCapture = IsEnPassantCapture(move.From, move.To, enPassantCaptureInfo);
            var isPawnPromotion = IsPawnPromotion(move.From, move.To);

            var moveData = MovePieceInternal(move);
            var capturedPiece = moveData.CapturedPiece;

            if (isEnPassantCapture)
            {
                if (enPassantCaptureInfo == null)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }

                enPassantCapturedPiecePosition = enPassantCaptureInfo.TargetPiecePosition;
                capturedPiece = SetPiece(enPassantCaptureInfo.TargetPiecePosition, Piece.None);
                if (capturedPiece.GetPieceType() != PieceType.Pawn)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }
            }
            else if (isPawnPromotion)
            {
                if (move.PromotionResult == PieceType.None)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Promoted piece type is not specified ({0}).",
                            move));
                }

                var previousPiece = SetPiece(move.To, move.PromotionResult.ToPiece(movingColor));
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
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The castling {{{0}}} ({1}) is not allowed.",
                            move,
                            castlingInfo.CastlingType.GetName()));
                }

                castlingRookMove = castlingInfo.RookMove;
                var rookMoveData = MovePieceInternal(castlingRookMove);
                if (rookMoveData.CapturedPiece != Piece.None)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }

                castlingOptions &= ~movingColorAllCastlingOptions;
            }

            var movingColorCurrentCastlingOptions = castlingOptions & movingColorAllCastlingOptions;
            if (movingColorCurrentCastlingOptions != CastlingOptions.None)
            {
                switch (pieceInfo.PieceType)
                {
                    case PieceType.King:
                        castlingOptions &= ~movingColorAllCastlingOptions;
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

            var oppositeColor = movingColor.Invert();
            var oppositeColorAllCastlingOptions = ChessHelper.ColorToCastlingOptionsMap[oppositeColor];
            var oppositeColorCurrentCastlingOptions = castlingOptions & oppositeColorAllCastlingOptions;
            if (oppositeColorCurrentCastlingOptions != CastlingOptions.None
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
                enPassantCapturedPiecePosition);

            _undoMoveDatas.Push(undoMoveData);

            EnsureConsistency();

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

            SetPiece(data.Move.From, data.MovedPiece);
            SetPiece(data.Move.To, Piece.None);

            if (data.CapturedPiece != Piece.None)
            {
                SetPiece(data.CapturedPiecePosition, data.CapturedPiece);
            }
            else if (data.CastlingRookMove != null)
            {
                var castlingRook = SetPiece(data.CastlingRookMove.To, Piece.None);
                if (castlingRook.GetPieceType() != PieceType.Rook
                    || castlingRook.GetColor() != data.MovedPiece.GetColor())
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }

                SetPiece(data.CastlingRookMove.From, castlingRook);
            }

            EnsureConsistency();
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPieceArrayIndexInternal(Piece piece)
        {
            return (int)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetColorArrayIndexInternal(PieceColor color)
        {
            return (int)color;
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
                var attackerBitboard = Bitboard.PopFirstBitSetInternal(ref currentValue);
                var attackerSquareIndex = Bitboard.FindFirstBitSetIndexInternal(attackerBitboard);
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
                var targetSquareIndex = Bitboard.PopFirstBitSetIndex(ref destinationsBitboard);

                var move = new GameMove(
                    Position.FromSquareIndex(targetSquareIndex - moveOffset),
                    Position.FromSquareIndex(targetSquareIndex));

                var moveInfo = new GameMoveInfo(moveFlags);
                if (isPawnPromotion)
                {
                    var promotionMoves = move.MakeAllPromotions();
                    foreach (var promotionMove in promotionMoves)
                    {
                        resultMoves.Add(new GameMoveData(promotionMove, moveInfo));
                    }
                }
                else
                {
                    resultMoves.Add(new GameMoveData(move, moveInfo));
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
            PopulatePawnMoves(resultMoves, nonPromotionCaptures, (int)captureDirection, GameMoveFlags.IsCapture);

            var promotionCaptures = captures & rank8;
            PopulatePawnMoves(
                resultMoves,
                promotionCaptures,
                (int)captureDirection,
                GameMoveFlags.IsCapture | GameMoveFlags.IsPawnPromotion);
        }

        private static void PopulateSimpleMoves(
            ICollection<GameMoveData> resultMoves,
            Position sourcePosition,
            Bitboard destinationsBitboard,
            GameMoveFlags moveFlags)
        {
            var moveInfo = new GameMoveInfo(moveFlags);
            while (destinationsBitboard.IsAny)
            {
                var targetSquareIndex = Bitboard.PopFirstBitSetIndex(ref destinationsBitboard);

                var move = new GameMove(sourcePosition, Position.FromSquareIndex(targetSquareIndex));
                resultMoves.Add(new GameMoveData(move, moveInfo));
            }
        }

        private static void PopulateKingCastlingMoves(
            ICollection<GameMoveData> resultMoves,
            Position sourcePosition,
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
            if (info.KingMove.From != sourcePosition || (nonEmptySquares & info.EmptySquares).IsAny)
            {
                return;
            }

            var moveData = new GameMoveData(info.KingMove, new GameMoveInfo(GameMoveFlags.IsKingCastling));
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
                var attackerSquareIndex = Bitboard.PopFirstBitSetIndex(ref current);
                var connection = GetConnection(squareIndex, attackerSquareIndex);

                var pinned = ownPieces & connection;
                var enemiesOnConnection = enemyPieces & connection;
                if (enemiesOnConnection.IsAny || !pinned.IsExactlyOneBitSet())
                {
                    continue;
                }

                var pinnedSquareIndex = pinned.FindFirstBitSetIndex();
                pinLimitations[pinnedSquareIndex] = connection | Bitboard.FromSquareIndex(attackerSquareIndex);
            }
        }

        private static Bitboard[] InitializeStraightSlidingAttacks()
        {
            var result = new Bitboard[ChessConstants.SquareCount];

            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var position = Position.FromSquareIndex(squareIndex);
                var attackBitboard = new Bitboard(
                    Position.GenerateRank(position.Rank).Concat(Position.GenerateFile(position.File)));
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
                var position = Position.FromSquareIndex(squareIndex);
                var knightMovePositions = ChessHelper.GetKnightMovePositions(position);
                var bitboard = new Bitboard(knightMovePositions);
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
                    new Bitboard(info.EmptySquares.Concat(info.PassedPosition.AsArray())));
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
                        var anotherSquareIndex = current.FindFirstBitSetIndex();
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

        private void EnsureConsistencyInternal()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var position in ChessHelper.AllPositions)
            {
                var x88Value = position.X88Value;
                var bitboardBit = position.Bitboard;

                var piece = _pieces[x88Value];

                foreach (var currentPiece in ChessConstants.Pieces)
                {
                    var isSet = (GetBitboard(currentPiece) & bitboardBit).IsAny;
                    if ((piece == currentPiece) != isSet)
                    {
                        throw new ChessPlatformException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Bitboard inconsistency for the piece '{0}' at '{1}'.",
                                piece.GetName(),
                                position));
                    }
                }
            }

            var allBitboards = ChessConstants.Pieces.Select(this.GetBitboard).ToArray();
            for (var outerIndex = 0; outerIndex < allBitboards.Length; outerIndex++)
            {
                var outerBitboard = allBitboards[outerIndex];
                for (var innerIndex = outerIndex + 1; innerIndex < allBitboards.Length; innerIndex++)
                {
                    var innerBitboard = allBitboards[innerIndex];
                    var intersectionBitboard = outerBitboard & innerBitboard;
                    if (intersectionBitboard.IsNone)
                    {
                        continue;
                    }

                    var intersectingPositions =
                        intersectionBitboard.GetPositions().Select(item => item.ToString()).Join("', '");

                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Bitboard inconsistency at '{0}'.",
                            intersectingPositions));
                }
            }

            foreach (var color in ChessConstants.PieceColors)
            {
                var actual = GetBitboard(color);
                var expected = GetEntireColorBitboardNonCached(color);
                if (actual != expected)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Entire-color-bitboard inconsistency: expected '{0}', actual '{1}'.",
                            expected,
                            actual));
                }
            }
        }

        private long ComputeZobristKey()
        {
            var pieces = ChessHelper.AllPositions
                .Select(position => new { Position = position, Piece = this[position] })
                .Where(obj => obj.Piece != Piece.None)
                .ToArray();

            var result = pieces.Aggregate(
                0L,
                (accumulator, obj) => accumulator ^ ZobristHashHelper.GetPieceHash(obj.Position, obj.Piece));

            return result;
        }

        private MovePieceData MovePieceInternal(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            var movedPiece = SetPiece(move.From, Piece.None);
            if (movedPiece == Piece.None)
            {
                throw new ChessPlatformException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The source square of the move {{{0}}} is empty.",
                        move));
            }

            var capturedPiece = SetPiece(move.To, movedPiece);
            if (capturedPiece != Piece.None)
            {
                if (capturedPiece.GetColor() == movedPiece.GetColor())
                {
                    throw new ChessPlatformException("Cannot capture a piece of the same color.");
                }
            }

            return new MovePieceData(movedPiece, capturedPiece);
        }

        private void GetPotentialMovePositionsByRays(
            Position sourcePosition,
            PieceColor sourceColor,
            IEnumerable<RayInfo> rays,
            int maxDistance,
            bool allowCapturing,
            ICollection<Position> resultCollection)
        {
            foreach (var ray in rays)
            {
                for (byte currentX88Value = (byte)(sourcePosition.X88Value + ray.Offset), distance = 1;
                    Position.IsValidX88Value(currentX88Value) && distance <= maxDistance;
                    currentX88Value += ray.Offset, distance++)
                {
                    var currentPosition = new Position(currentX88Value);

                    var pieceInfo = GetPieceInfo(currentPosition);
                    var currentColor = pieceInfo.Color;
                    if (pieceInfo.Piece == Piece.None || !currentColor.HasValue)
                    {
                        resultCollection.Add(currentPosition);
                        continue;
                    }

                    if (currentColor.Value != sourceColor && allowCapturing)
                    {
                        resultCollection.Add(currentPosition);
                    }

                    break;
                }
            }
        }

        private Bitboard GetEntireColorBitboardNonCached(PieceColor color)
        {
            return ChessConstants.ColorToPiecesMap[color].Aggregate(
                Bitboard.None,
                (a, piece) => a | GetBitboard(piece));
        }

        private Bitboard GetAttackersInternal(
            Position targetPosition,
            PieceColor attackingColor,
            bool findFirstAttackOnly)
        {
            var result = new Bitboard();

            var targetBitboard = targetPosition.Bitboard;
            var targetSquareIndex = targetPosition.SquareIndex;

            var opponentPawns = GetBitboard(PieceType.Pawn.ToPiece(attackingColor));
            if (opponentPawns.IsAny)
            {
                ShiftDirection left;
                ShiftDirection right;
                if (attackingColor == PieceColor.White)
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

            var opponentKnights = GetBitboard(PieceType.Knight.ToPiece(attackingColor));
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

            var opponentKings = GetBitboard(PieceType.King.ToPiece(attackingColor));
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

            var emptySquareBitboard = GetBitboard(Piece.None);

            var opponentQueens = GetBitboard(PieceType.Queen.ToPiece(attackingColor));

            var opponentRooks = GetBitboard(PieceType.Rook.ToPiece(attackingColor));
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

            var opponentBishops = GetBitboard(PieceType.Bishop.ToPiece(attackingColor));
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

        private bool IsKingLeftOnly(PieceColor color)
        {
            var entire = GetBitboard(color);
            var king = GetBitboard(PieceType.King.ToPiece(color));
            var otherPieces = entire & ~king;
            return otherPieces.IsNone;
        }

        private void GenerateSlidingPieceMoves(
            [NotNull] ICollection<GameMoveData> resultMoves,
            PieceColor color,
            GeneratedMoveTypes moveTypes,
            PieceType pieceType,
            ShiftDirection[] directions)
        {
            var piece = pieceType.ToPiece(color);
            var pieces = GetBitboard(piece);

            var emptySquares = GetBitboard(Piece.None);
            var enemies = GetBitboard(color.Invert());

            var shouldGenerateQuiets = moveTypes.IsAnySet(GeneratedMoveTypes.Quiet);
            var shouldGenerateCaptures = moveTypes.IsAnySet(GeneratedMoveTypes.Capture);

            while (pieces.IsAny)
            {
                var sourceSquareIndex = Bitboard.PopFirstBitSetIndex(ref pieces);
                var sourceBitboard = Bitboard.FromSquareIndex(sourceSquareIndex);
                var sourcePosition = Position.FromSquareIndex(sourceSquareIndex);

                foreach (var direction in directions)
                {
                    var current = sourceBitboard;

                    while ((current = current.Shift(direction)).IsAny)
                    {
                        if ((current & emptySquares).IsAny)
                        {
                            if (shouldGenerateQuiets)
                            {
                                var move = new GameMove(sourcePosition, current.GetFirstPosition());
                                resultMoves.Add(new GameMoveData(move, new GameMoveInfo(GameMoveFlags.None)));
                            }

                            continue;
                        }

                        if ((current & enemies).IsAny)
                        {
                            if (shouldGenerateCaptures)
                            {
                                var move = new GameMove(sourcePosition, current.GetFirstPosition());
                                resultMoves.Add(new GameMoveData(move, new GameMoveInfo(GameMoveFlags.IsCapture)));
                            }
                        }

                        break;
                    }
                }
            }
        }

        #endregion

        #region InternalCastlingInfo Structure

        internal struct InternalCastlingInfo
        {
            #region Constants and Fields

            private readonly GameMove _kingMove;
            private readonly Bitboard _emptySquares;

            #endregion

            #region Constructors

            public InternalCastlingInfo(GameMove kingMove, Bitboard emptySquares)
            {
                _kingMove = kingMove.EnsureNotNull();
                _emptySquares = emptySquares;
            }

            #endregion

            #region Public Properties

            public GameMove KingMove
            {
                [DebuggerStepThrough]
                get
                {
                    return _kingMove;
                }
            }

            public Bitboard EmptySquares
            {
                [DebuggerStepThrough]
                get
                {
                    return _emptySquares;
                }
            }

            #endregion
        }

        #endregion
    }
}