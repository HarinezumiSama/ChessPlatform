using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal sealed class GameBoardData
    {
        #region Constants and Fields

        private const int ColorAndPositionArrayLength = ChessConstants.SquareCount * 2;

        private static readonly int PieceArrayLength = ChessConstants.Pieces.Max(item => (int)item) + 1;
        private static readonly int ColorArrayLength = ChessConstants.PieceColors.Max(item => (int)item) + 1;

        private static readonly int[] PawnPushes = InitializePawnPushes();
        private static readonly DoublePushData[] PawnDoublePushes = InitializePawnDoublePushes();
        private static readonly PieceAttackInfo[] PawnAttackMoves = InitializePawnAttackMoves();

        private static readonly Bitboard[] StraightSlidingAttacks = InitializeStraightSlidingAttacks();
        private static readonly Bitboard[] DiagonallySlidingAttacks = InitializeDiagonallySlidingAttacks();
        private static readonly Bitboard[] KnightAttacksOrMoves = InitializeKnightAttacks();
        private static readonly Bitboard[] KingAttacksOrMoves = InitializeKingAttacksOrMoves();

        private static readonly Bitboard[] Connections = InitializeConnections();

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
        }

        private GameBoardData(GameBoardData other)
        {
            #region Argument Check

            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            #endregion

            _undoMoveDatas = new Stack<MakeMoveData>();
            _pieces = other._pieces.Copy();
            _bitboards = other._bitboards.Copy();
            _entireColorBitboards = other._entireColorBitboards.Copy();
        }

        #endregion

        #region Public Properties

        public Piece this[Position position]
        {
            get
            {
                return _pieces[position.X88Value];
            }
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

        public PieceInfo GetPieceInfo(Position position)
        {
            var piece = this[position];
            return piece.GetPieceInfo();
        }

        public Bitboard GetBitboard(Piece piece)
        {
            var index = GetPieceArrayIndexInternal(piece);

            var bitboard = _bitboards[index];
            return bitboard;
        }

        public Bitboard GetBitboard(PieceColor color)
        {
            var bitboard = _entireColorBitboards[GetColorArrayIndexInternal(color)];
            return bitboard;
        }

        public Position[] GetPositions(Piece piece)
        {
            var bitboard = GetBitboard(piece);
            return bitboard.GetPositions();
        }

        public Position[] GetPositions(PieceColor color)
        {
            var bitboard = GetBitboard(color);
            return bitboard.GetPositions();
        }

        public int GetPieceCount(Piece piece)
        {
            var bitboard = GetBitboard(piece);
            return bitboard.GetCount();
        }

        public EnPassantCaptureInfo GetEnPassantCaptureInfo([NotNull] GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
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
            var pieceInfo = GetPieceInfo(from);

            var result = pieceInfo.PieceType == PieceType.Pawn && pieceInfo.Color.HasValue
                && to.Rank
                    == (pieceInfo.Color.Value == PieceColor.White
                        ? ChessConstants.WhitePawnPromotionRank
                        : ChessConstants.BlackPawnPromotionRank);

            return result;
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

        public Bitboard GetAttackers(Position targetPosition, PieceColor attackingColor)
        {
            return GetAttackersInternal(targetPosition, attackingColor, false);
        }

        public bool IsUnderAttack(Position targetPosition, PieceColor attackingColor)
        {
            var bitboard = GetAttackersInternal(targetPosition, attackingColor, true);
            return bitboard.IsAny;
        }

        public bool IsAnyUnderAttack(
            [NotNull] IEnumerable<Position> targetPositions,
            PieceColor attackingColor)
        {
            #region Argument Check

            if (targetPositions == null)
            {
                throw new ArgumentNullException("targetPositions");
            }

            #endregion

            var result = targetPositions.Any(targetPosition => IsUnderAttack(targetPosition, attackingColor));
            return result;
        }

        public PinnedPieceInfo[] GetPinnedPieceInfos(Position targetPosition)
        {
            var targetPieceInfo = GetPieceInfo(targetPosition);
            if (targetPieceInfo.Piece == Piece.None || !targetPieceInfo.Color.HasValue)
            {
                throw new ArgumentException("Empty square.", "targetPosition");
            }

            var resultList = new List<PinnedPieceInfo>();

            var targetColor = targetPieceInfo.Color.Value;
            var attackingColor = targetColor.Invert();

            var targetColorBitboard = GetBitboard(targetColor);
            var attackingColorBitboard = GetBitboard(attackingColor);

            var attackInfoKey = new AttackInfoKey(targetPosition, attackingColor);
            var attackInfo = ChessHelper.TargetPositionToAttackInfoMap[attackInfoKey];

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var pair in attackInfo.Attacks)
            {
                var pieceAttackInfo = pair.Value;
                if (pieceAttackInfo.IsDirectAttack)
                {
                    continue;
                }

                var attackingPiece = pair.Key.ToPiece(attackingColor);
                var bitboard = GetBitboard(attackingPiece);

                var attackBitboard = bitboard & pieceAttackInfo.Bitboard;
                if (attackBitboard.IsNone)
                {
                    continue;
                }

                var potentialPositions = attackBitboard.GetPositions();

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var potentialPosition in potentialPositions)
                {
                    var positionBridgeKey = new PositionBridgeKey(targetPosition, potentialPosition);
                    var positionBridge = ChessHelper.PositionBridgeMap[positionBridgeKey];

                    if ((attackingColorBitboard & positionBridge).IsAny)
                    {
                        continue;
                    }

                    var pinnedPieceBitboard = targetColorBitboard & positionBridge;
                    if (!pinnedPieceBitboard.IsExactlyOneBitSet())
                    {
                        continue;
                    }

                    var index = pinnedPieceBitboard.FindFirstBitSetIndex();
                    var pinnedPiecePosition = Position.FromSquareIndex(index);

                    var allowedMoves = (positionBridge & ~pinnedPieceBitboard) | potentialPosition.Bitboard;
                    var pinnedPieceInfo = new PinnedPieceInfo(pinnedPiecePosition, allowedMoves);

                    resultList.Add(pinnedPieceInfo);
                }
            }

            return resultList.ToArray();
        }

        public bool IsInCheck(PieceColor kingColor)
        {
            var king = PieceType.King.ToPiece(kingColor);
            var oppositeColor = kingColor.Invert();
            var kingPositions = GetPositions(king);
            return kingPositions.Length != 0 && kingPositions.Any(position => IsUnderAttack(position, oppositeColor));
            //return kingPositions.Length > 0 && IsUnderAttack(kingPositions[0], oppositeColor);
        }

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
                throw new ArgumentException("No piece at the source position.", "sourcePosition");
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
                var result = GetKingPotentialMovePositions(castlingOptions, sourcePosition, pieceColor);
                return result;
            }

            if (pieceInfo.PieceType == PieceType.Pawn)
            {
                var result = GetPawnPotentialMovePositions(enPassantCaptureInfo, sourcePosition, pieceColor);
                return result;
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

        public GameMove GetEnPassantMove(Position sourcePosition)
        {
            var pieceInfo = GetPieceInfo(sourcePosition);
            if (pieceInfo.PieceType != PieceType.Pawn || !pieceInfo.Color.HasValue)
            {
                return null;
            }

            var enPassantInfo = ChessConstants.ColorToEnPassantInfoMap[pieceInfo.Color.Value].EnsureNotNull();
            if (sourcePosition.Rank != enPassantInfo.StartRank)
            {
                return null;
            }

            var destinationPosition = new Position(false, sourcePosition.File, enPassantInfo.EndRank);
            var intermediatePosition = new Position(false, sourcePosition.File, enPassantInfo.CaptureTargetRank);
            var isEnPassant = CheckSquares(Piece.None, intermediatePosition, destinationPosition);

            return isEnPassant ? new GameMove(sourcePosition, destinationPosition) : null;
        }

        public void GeneratePawnMoves(
            List<GameMoveData> resultMoves,
            PieceColor color,
            Bitboard enPassantCaptureTarget,
            Bitboard target)
        {
            #region Argument Check

            if (resultMoves == null)
            {
                throw new ArgumentNullException("resultMoves");
            }

            #endregion

            var pawn = PieceType.Pawn.ToPiece(color);
            var pawns = GetBitboard(pawn);
            if (pawns.IsNone)
            {
                return;
            }

            var forwardDirection = color == PieceColor.White ? ShiftDirection.North : ShiftDirection.South;
            var rank8 = color == PieceColor.White ? Bitboards.Rank8 : Bitboards.Rank1;

            var emptySquares = GetBitboard(Piece.None);
            var emptyTargetSquares = emptySquares & target;
            var enemies = GetBitboard(color.Invert());
            var enemyTargets = enemies & target;

            var pushes = pawns.Shift(forwardDirection) & emptyTargetSquares;
            if (pushes.IsAny)
            {

                var nonPromotionPushes = pushes & ~rank8;
                PopulatePawnMoves(resultMoves, nonPromotionPushes, (int)forwardDirection, GameMoveFlags.None);

                var promotionPushes = pushes & rank8;
                PopulatePawnMoves(resultMoves, promotionPushes, (int)forwardDirection, GameMoveFlags.IsPawnPromotion);

                var rank3 = color == PieceColor.White ? Bitboards.Rank3 : Bitboards.Rank6;
                var doublePushes = (pushes & rank3).Shift(forwardDirection) & emptyTargetSquares;
                PopulatePawnMoves(
                    resultMoves,
                    doublePushes,
                    (int)forwardDirection + (int)forwardDirection,
                    GameMoveFlags.None);
            }

            var leftCaptureOffset = color == PieceColor.White ? ShiftDirection.NorthWest : ShiftDirection.SouthEast;
            PopulatePawnCaptures(resultMoves, pawns, enemyTargets, leftCaptureOffset, rank8, enPassantCaptureTarget);

            var rightCaptureOffset = color == PieceColor.White ? ShiftDirection.NorthEast : ShiftDirection.SouthWest;
            PopulatePawnCaptures(resultMoves, pawns, enemyTargets, rightCaptureOffset, rank8, enPassantCaptureTarget);
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

            return oldPiece;
        }

        internal void SetupNewPiece(Piece piece, Position position)
        {
            #region Argument Check

            if (piece == Piece.None)
            {
                throw new ArgumentException("Must be a piece rather than empty square.", "piece");
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
                    "fenSnippet");
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
                        throw new ArgumentException(InvalidFenMessage, "fenSnippet");
                    }

                    currentFile = 0;
                    currentRank--;

                    if (currentRank < 0)
                    {
                        throw new ArgumentException(InvalidFenMessage, "fenSnippet");
                    }

                    continue;
                }

                if (currentFile >= ChessConstants.FileCount)
                {
                    throw new ArgumentException(InvalidFenMessage, "fenSnippet");
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
                    throw new ArgumentException(InvalidFenMessage, "fenSnippet");
                }

                currentFile += emptySquareCount;
            }

            if (currentFile != ChessConstants.FileCount)
            {
                throw new ArgumentException(InvalidFenMessage, "fenSnippet");
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
                throw new ArgumentNullException("move");
            }

            var pieceInfo = GetPieceInfo(move.From);
            if (pieceInfo.PieceType == PieceType.None || pieceInfo.Color != movingColor)
            {
                throw new ArgumentException("Invalid move.", "move");
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
                            castlingInfo.Option.GetName()));
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

        private static int GetColorAndPositionIndexInternal(PieceColor color, Position position)
        {
            return ((int)color) * ChessConstants.SquareCount + position.SquareIndex;
        }

        private static int GetPieceArrayIndexInternal(Piece piece)
        {
            return (int)piece;
        }

        private static int GetColorArrayIndexInternal(PieceColor color)
        {
            return (int)color;
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
                    var promotions = move.MakeAllPromotions();
                    foreach (var promotion in promotions)
                    {
                        resultMoves.Add(new GameMoveData(promotion, moveInfo));
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
            PopulatePawnMoves(resultMoves, enPassantCapture, (int)captureDirection, GameMoveFlags.IsEnPassantCapture);

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

        private static int[] InitializePawnPushes()
        {
            var result = new int[ColorAndPositionArrayLength];
            result.Initialize(i => int.MaxValue);

            foreach (var pieceColor in ChessConstants.PieceColors)
            {
                var moveRay = ChessHelper.PawnMoveRayMap[pieceColor];

                foreach (var sourcePosition in ChessHelper.AllPawnPositions)
                {
                    var destinationPosition = new Position((byte)(sourcePosition.X88Value + moveRay.Offset));
                    var index = GetColorAndPositionIndexInternal(pieceColor, sourcePosition);
                    result[index] = destinationPosition.SquareIndex;
                }
            }

            return result;
        }

        private static DoublePushData[] InitializePawnDoublePushes()
        {
            var result = new DoublePushData[ColorAndPositionArrayLength];
            result.Initialize(i => new DoublePushData(new Position(), Bitboard.Everything));

            foreach (var pieceColor in ChessConstants.PieceColors)
            {
                var enPassantInfo = ChessConstants.ColorToEnPassantInfoMap[pieceColor];
                var moveRay = ChessHelper.PawnEnPassantMoveRayMap[pieceColor];
                var intermediateRay = ChessHelper.PawnMoveRayMap[pieceColor];

                foreach (var sourcePosition in ChessHelper.AllPawnPositions)
                {
                    if (sourcePosition.Rank != enPassantInfo.StartRank)
                    {
                        continue;
                    }

                    var destinationPosition = new Position((byte)(sourcePosition.X88Value + moveRay.Offset));
                    var intermediatePosition = new Position((byte)(sourcePosition.X88Value + intermediateRay.Offset));

                    var index = GetColorAndPositionIndexInternal(pieceColor, sourcePosition);

                    result[index] = new DoublePushData(
                        destinationPosition,
                        destinationPosition.Bitboard | intermediatePosition.Bitboard);
                }
            }

            return result;
        }

        private static PieceAttackInfo[] InitializePawnAttackMoves()
        {
            var result = new PieceAttackInfo[ColorAndPositionArrayLength];
            result.Initialize(i => new PieceAttackInfo());

            foreach (var pieceColor in ChessConstants.PieceColors)
            {
                var attackOffsets = ChessHelper.PawnAttackOffsetMap[pieceColor];

                foreach (var sourcePosition in ChessHelper.AllPawnPositions)
                {
                    var attackPositions = ChessHelper.GetOnboardPositions(sourcePosition, attackOffsets);
                    var index = GetColorAndPositionIndexInternal(pieceColor, sourcePosition);
                    result[index] = new PieceAttackInfo(attackPositions, true);
                }
            }

            return result;
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

        private static Bitboard[] InitializeConnections()
        {
            var result = new Bitboard[ChessConstants.SquareCount * ChessConstants.SquareCount];
            result.Initialize(i => Bitboard.Everything);

            var directions = EnumFactotum.GetAllValues<ShiftDirection>();

            for (var squareIndex = 0; squareIndex < ChessConstants.SquareCount; squareIndex++)
            {
                var source = Bitboard.FromSquareIndex(squareIndex);

                foreach (var direction in directions)
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

        private static int GetConnectionIndex(int squareIndex1, int squareIndex2)
        {
            Debug.Assert(squareIndex1 >= 0);
            Debug.Assert(squareIndex2 >= 0);

            return squareIndex1 + squareIndex2 * ChessConstants.SquareCount;
        }

        private static Bitboard GetConnection(int squareIndex1, int squareIndex2)
        {
            var index = GetConnectionIndex(squareIndex1, squareIndex2);
            return Connections[index];
        }

        private static bool IsDoublePushPotentiallyAllowed(Position pawnPosition, PieceColor pawnColor)
        {
            var allowedStartRank = 1 + (5 * (int)pawnColor);
            return pawnPosition.Rank == allowedStartRank;
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

        private MovePieceData MovePieceInternal(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
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

        private void GetPotentialCastlingMovePositions(
            Position sourcePosition,
            PieceColor color,
            CastlingOptions castlingOptions,
            ICollection<Position> resultCollection)
        {
            switch (color)
            {
                case PieceColor.White:
                    {
                        GetPotentialCastlingMove(
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.WhiteKingSide,
                            resultCollection);

                        GetPotentialCastlingMove(
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.WhiteQueenSide,
                            resultCollection);
                    }
                    break;

                case PieceColor.Black:
                    {
                        GetPotentialCastlingMove(
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.BlackKingSide,
                            resultCollection);

                        GetPotentialCastlingMove(
                            sourcePosition,
                            castlingOptions,
                            CastlingOptions.BlackQueenSide,
                            resultCollection);
                    }
                    break;

                default:
                    throw color.CreateEnumValueNotSupportedException();
            }
        }

        private bool CheckSquares(Piece expectedPiece, IEnumerable<Position> positions)
        {
            return positions.All(position => this[position] == expectedPiece);
        }

        private bool CheckSquares(Piece expectedPiece, params Position[] positions)
        {
            return CheckSquares(expectedPiece, (IEnumerable<Position>)positions);
        }

        private Bitboard GetEntireColorBitboardNonCached(PieceColor color)
        {
            return ChessConstants.ColorToPiecesMap[color].Aggregate(
                Bitboard.None,
                (a, piece) => a | GetBitboard(piece));
        }

        private void GetPotentialCastlingMove(
            Position sourcePosition,
            CastlingOptions castlingOptions,
            CastlingOptions option,
            ICollection<Position> resultCollection)
        {
            var castlingInfo = ChessHelper.CastlingOptionToInfoMap[option];

            var isPotentiallyPossible = (castlingOptions & option) == option
                && sourcePosition == castlingInfo.KingMove.From
                && CheckSquares(Piece.None, castlingInfo.EmptySquares);

            if (isPotentiallyPossible)
            {
                resultCollection.Add(castlingInfo.KingMove.To);
            }
        }

        private Position[] GetKingPotentialMovePositions(
            CastlingOptions castlingOptions,
            Position sourcePosition,
            PieceColor pieceColor)
        {
            var resultList = new List<Position>();

            GetPotentialMovePositionsByRays(
                sourcePosition,
                pieceColor,
                ChessHelper.KingAttackRays,
                ChessHelper.MaxKingMoveOrAttackDistance,
                true,
                resultList);

            GetPotentialCastlingMovePositions(
                sourcePosition,
                pieceColor,
                castlingOptions,
                resultList);

            return resultList.ToArray();
        }

        private Position[] GetPawnPotentialMovePositions(
            [CanBeNull] EnPassantCaptureInfo enPassantCaptureTarget,
            Position sourcePosition,
            PieceColor pieceColor)
        {
            var resultList = new List<Position>(4);

            var colorAndPositionIndex = GetColorAndPositionIndexInternal(pieceColor, sourcePosition);
            var pawnPush = PawnPushes[colorAndPositionIndex];
            if ((GetBitboard(Piece.None) & Bitboard.FromSquareIndex(pawnPush)).IsAny)
            {
                resultList.Add(Position.FromSquareIndex(pawnPush));
            }

            if (IsDoublePushPotentiallyAllowed(sourcePosition, pieceColor))
            {
                var pawnPushInfo = PawnDoublePushes[colorAndPositionIndex];
                if ((GetBitboard(Piece.None) & pawnPushInfo.EmptyPositions) == pawnPushInfo.EmptyPositions)
                {
                    resultList.Add(pawnPushInfo.DestinationPosition);
                }
            }

            var pieceAttackInfo = PawnAttackMoves[colorAndPositionIndex];
            var attackPositions = pieceAttackInfo.Bitboard.GetPositions();
            var oppositeColor = pieceColor.Invert();

            //// ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var attackPosition in attackPositions)
            {
                var attackedPieceInfo = GetPieceInfo(attackPosition);
                if (attackedPieceInfo.Color == oppositeColor
                    || (enPassantCaptureTarget != null
                        && attackPosition == enPassantCaptureTarget.CapturePosition))
                {
                    resultList.Add(attackPosition);
                }
            }

            return resultList.ToArray();
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
                if (result.IsAny && findFirstAttackOnly)
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
                if (result.IsAny && findFirstAttackOnly)
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
                if (result.IsAny && findFirstAttackOnly)
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
            if (result.IsAny && findFirstAttackOnly)
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

        #endregion
    }
}