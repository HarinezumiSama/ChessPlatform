﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    internal sealed class PieceData
    {
        #region Constants and Fields

        private static readonly ReadOnlySet<PieceType> ForcedDrawPieceTypes =
            new[] { PieceType.None, PieceType.King }.ToHashSet().AsReadOnly();

        private readonly Stack<MakeMoveData> _undoMoveDatas = new Stack<MakeMoveData>();

        private readonly Dictionary<Piece, ulong> _bitboards = ChessConstants.Pieces.ToDictionary(
            Factotum.Identity,
            item => item == Piece.None ? ulong.MaxValue : 0UL);

        private readonly Piece[] _pieces;
        private readonly Dictionary<Piece, HashSet<byte>> _pieceOffsetMap;

        #endregion

        #region Constructors

        internal PieceData()
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            _pieces = new Piece[ChessConstants.X88Length];
            _pieceOffsetMap = new Dictionary<Piece, HashSet<byte>>
            {
                { Piece.None, ChessHelper.AllPositions.Select(position => position.X88Value).ToHashSet() }
            };
        }

        private PieceData(PieceData other)
        {
            #region Argument Check

            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            #endregion

            _pieces = other._pieces.Copy();
            _pieceOffsetMap = CopyPieceOffsetMap(other._pieceOffsetMap);
            _bitboards = new Dictionary<Piece, ulong>(other._bitboards);
        }

        #endregion

        #region Public Methods

        public void EnsureConsistency()
        {
            EnsureConsistencyInternal();
        }

        public PieceData Copy()
        {
            return new PieceData(this);
        }

        public Piece GetPiece(Position position)
        {
            return _pieces[position.X88Value];
        }

        public PieceInfo GetPieceInfo(Position position)
        {
            var piece = GetPiece(position);
            return piece.GetPieceInfo();
        }

        public Position[] GetPiecePositions(Piece piece)
        {
            var x88Values = _pieceOffsetMap.GetValueOrDefault(piece);

            var result = x88Values == null
                ? new Position[0]
                : x88Values.Select(ChessHelper.GetPositionFromX88Value).ToArray();

            return result;
        }

        public Position[] GetPiecePositions(PieceColor color)
        {
            var pieces =
                ChessConstants.PieceTypes.Where(item => item != PieceType.None).Select(item => item.ToPiece(color));

            var result = pieces.SelectMany(this.GetPiecePositions).ToArray();
            return result;
        }

        public int GetPieceCount(Piece piece)
        {
            var x88Values = _pieceOffsetMap.GetValueOrDefault(piece);

            var result = x88Values.Morph(obj => obj.Count, 0);
            return result;
        }

        public EnPassantCaptureInfo GetEnPassantCaptureInfo(PieceMove move)
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

            var capturePosition = new Position(move.From.File, enPassantInfo.CaptureTargetRank);
            var targetPiecePosition = new Position(move.From.File, enPassantInfo.EndRank);

            return new EnPassantCaptureInfo(capturePosition, targetPiecePosition);
        }

        public bool IsEnPassantCapture(PieceMove move, EnPassantCaptureInfo enPassantCaptureInfo)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            if (enPassantCaptureInfo == null)
            {
                return false;
            }

            var targetPieceInfo = GetPieceInfo(enPassantCaptureInfo.TargetPiecePosition);
            var pieceInfo = GetPieceInfo(move.From);

            var result = pieceInfo.PieceType == PieceType.Pawn && targetPieceInfo.PieceType == PieceType.Pawn
                && enPassantCaptureInfo.CapturePosition == move.To;

            return result;
        }

        public bool IsPawnPromotion(Position from, Position to)
        {
            var pieceInfo = GetPieceInfo(from);

            var result = pieceInfo.PieceType == PieceType.Pawn && pieceInfo.Color.HasValue
                && to.Rank == ChessHelper.ColorToPawnPromotionRankMap[pieceInfo.Color.Value];

            return result;
        }

        public bool IsPawnPromotion(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            return IsPawnPromotion(move.From, move.To);
        }

        public CastlingInfo CheckCastlingMove(PieceMove move)
        {
            var pieceInfo = GetPieceInfo(move.From);
            if (pieceInfo.PieceType != PieceType.King || !pieceInfo.Color.HasValue)
            {
                return null;
            }

            var castlingOptions = ChessHelper.ColorToCastlingOptionSetMap[pieceInfo.Color.Value];

            var result = ChessHelper.CastlingOptionToInfoMap
                .SingleOrDefault(pair => castlingOptions.Contains(pair.Key) && pair.Value.KingMove == move)
                .Value;

            return result;
        }

        public Position[] GetAttacks(Position targetPosition, PieceColor attackingColor)
        {
            List<Position> result;
            GetAttacksInternal(targetPosition, attackingColor, false, out result);
            return result.EnsureNotNull().ToArray();
        }

        public bool IsUnderAttack(Position targetPosition, PieceColor attackingColor)
        {
            List<Position> attackingPositions;
            var result = GetAttacksInternal(targetPosition, attackingColor, true, out attackingPositions);
            return result;
        }

        public bool IsAnyUnderAttack(
            IEnumerable<Position> targetPositions,
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

        public bool IsInCheck(PieceColor kingColor)
        {
            var king = PieceType.King.ToPiece(kingColor);
            var oppositeColor = kingColor.Invert();
            var kingPositions = GetPiecePositions(king);
            return kingPositions.Length != 0 && kingPositions.Any(position => IsUnderAttack(position, oppositeColor));
            //return kingPositions.Length > 0 && IsUnderAttack(kingPositions[0], oppositeColor);
        }

        public bool IsInsufficientMaterialState()
        {
            var result = _pieces.All(piece => ForcedDrawPieceTypes.Contains(piece.GetPieceType()));
            return result;
        }

        public Position[] GetPotentialMovePositions(
            CastlingOptions castlingOptions,
            EnPassantCaptureInfo enPassantCaptureTarget,
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
                var result = GetPawnPotentialMovePositions(enPassantCaptureTarget, sourcePosition, pieceColor);
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

        public PieceMove GetEnPassantMove(Position sourcePosition)
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

            var destinationPosition = new Position(sourcePosition.File, enPassantInfo.EndRank);
            var intermediatePosition = new Position(sourcePosition.File, enPassantInfo.CaptureTargetRank);
            var isEnPassant = CheckSquares(Piece.None, intermediatePosition, destinationPosition);

            return isEnPassant ? new PieceMove(sourcePosition, destinationPosition) : null;
        }

        #endregion

        #region Internal Methods

        internal void SetupNewPiece(Piece piece, Position position)
        {
            #region Argument Check

            if (piece == Piece.None)
            {
                throw new ArgumentException("Must be a piece rather than empty square.", "piece");
            }

            #endregion

            var existingPiece = GetPiece(position);
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
            [NotNull] PieceMove move,
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

            PieceMove castlingRookMove = null;
            Position? enPassantCapturedPiecePosition = null;

            var movingColorAllCastlingOptions = ChessHelper.ColorToCastlingOptionsMap[movingColor];

            // Performing checks before actual move!
            var castlingInfo = CheckCastlingMove(move);
            var isEnPassantCapture = IsEnPassantCapture(move, enPassantCaptureInfo);
            var isPawnPromotion = IsPawnPromotion(move);

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

        private static Dictionary<Piece, HashSet<byte>> CopyPieceOffsetMap(
            ICollection<KeyValuePair<Piece, HashSet<byte>>> pieceOffsetMap)
        {
            #region Argument Check

            if (pieceOffsetMap == null)
            {
                throw new ArgumentNullException("pieceOffsetMap");
            }

            #endregion

            var result = new Dictionary<Piece, HashSet<byte>>(pieceOffsetMap.Count);
            foreach (var pair in pieceOffsetMap)
            {
                result.Add(pair.Key, new HashSet<byte>(pair.Value.EnsureNotNull()));
            }

            return result;
        }

        private void EnsureConsistencyInternal()
        {
            if (!DebugConstants.EnsurePieceDataConsistency)
            {
                return;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var position in ChessHelper.AllPositions)
            {
                var x88Value = position.X88Value;
                var bitboardBit = position.BitboardBit;

                var piece = _pieces[x88Value];

                var x88Values = _pieceOffsetMap.GetValueOrDefault(piece);
                if (x88Values == null || !x88Values.Contains(x88Value))
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Inconsistency for the piece '{0}' at '{1}'.",
                            piece.GetName(),
                            position));
                }

                foreach (var currentPiece in ChessConstants.Pieces)
                {
                    var isSet = (_bitboards[currentPiece] & bitboardBit) != 0;
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

            foreach (var pair in _pieceOffsetMap.Where(p => p.Value != null))
            {
                foreach (var x88Value in pair.Value)
                {
                    var piece = _pieces[x88Value];
                    if (piece != pair.Key)
                    {
                        throw new ChessPlatformException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Inconsistency for the piece '{0}' at 0x{1:X2}.",
                                pair.Key.GetName(),
                                x88Value));
                    }
                }
            }
        }

        private MovePieceData MovePieceInternal(PieceMove move)
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

        private Piece SetPiece(Position position, Piece piece)
        {
            var x88Value = position.X88Value;
            var bitboardBit = position.BitboardBit;

            var oldPiece = _pieces[x88Value];
            _pieces[x88Value] = piece;
            _bitboards[oldPiece] &= ~bitboardBit;
            _bitboards[piece] |= bitboardBit;

            var oldPieceRemoved = _pieceOffsetMap.GetValueOrCreate(oldPiece).Remove(x88Value);
            if (!oldPieceRemoved)
            {
                throw ChessPlatformException.CreateInconsistentStateError();
            }

            var added = _pieceOffsetMap.GetValueOrCreate(piece).Add(x88Value);
            if (!added)
            {
                throw ChessPlatformException.CreateInconsistentStateError();
            }

            return oldPiece;
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
                    var currentPosition = currentX88Value.GetPositionFromX88Value();

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
            return positions.All(position => GetPiece(position) == expectedPiece);
        }

        private bool CheckSquares(Piece expectedPiece, params Position[] positions)
        {
            return CheckSquares(expectedPiece, (IEnumerable<Position>)positions);
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
            EnPassantCaptureInfo enPassantCaptureTarget,
            Position sourcePosition,
            PieceColor pieceColor)
        {
            var resultList = new List<Position>();

            GetPotentialMovePositionsByRays(
                sourcePosition,
                pieceColor,
                ChessHelper.PawnMoveRayMap[pieceColor].AsCollection(),
                ChessHelper.MaxPawnAttackOrMoveDistance,
                false,
                resultList);

            var enPassantMove = GetEnPassantMove(sourcePosition);
            if (enPassantMove != null)
            {
                resultList.Add(enPassantMove.To);
            }

            var pawnAttackOffsets = ChessHelper.PawnAttackOffsetMap[pieceColor];
            var attackPositions = ChessHelper.GetOnboardPositions(sourcePosition, pawnAttackOffsets);
            var oppositeColor = pieceColor.Invert();

            //// ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var attackPosition in attackPositions)
            {
                var attackedPieceInfo = GetPieceInfo(attackPosition);
                if (attackedPieceInfo.Color == oppositeColor
                    || (enPassantCaptureTarget != null && attackPosition == enPassantCaptureTarget.CapturePosition))
                {
                    resultList.Add(attackPosition);
                }
            }

            return resultList.ToArray();
        }

        private bool GetAttacksInternal(
            Position targetPosition,
            PieceColor attackingColor,
            bool findFirstAttackOnly,
            out List<Position> attackingPositions)
        {
            attackingPositions = findFirstAttackOnly ? null : new List<Position>();

            var attackingPiecePositions = GetPiecePositions(attackingColor);
            foreach (var attackingPiecePosition in attackingPiecePositions)
            {
                var attackingPiece = GetPiece(attackingPiecePosition);
                var positionArrays = ChessHelper.GetAttackedPositionArrays(attackingPiecePosition, attackingPiece);

                var shouldStop = false;
                foreach (var positionArray in positionArrays)
                {
                    foreach (var position in positionArray)
                    {
                        if (position == targetPosition)
                        {
                            if (findFirstAttackOnly)
                            {
                                return true;
                            }

                            attackingPositions.Add(attackingPiecePosition);
                            shouldStop = true;
                            break;
                        }

                        var pieceInfo = GetPieceInfo(position);
                        if (pieceInfo.Piece != Piece.None && pieceInfo.Color.HasValue)
                        {
                            break;
                        }
                    }

                    if (shouldStop)
                    {
                        break;
                    }
                }
            }

            return !findFirstAttackOnly && attackingPositions.Count != 0;
        }

        #endregion
    }
}