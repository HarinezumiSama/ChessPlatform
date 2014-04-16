using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Omnifactotum;

namespace ChessPlatform
{
    internal sealed class PieceData
    {
        #region Constants and Fields

        private readonly Stack<UndoMoveData> _undoMoveDatas = new Stack<UndoMoveData>();

        #endregion

        #region Constructors

        internal PieceData()
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            this.Pieces = new Piece[ChessConstants.X88Length];
            this.PieceOffsetMap = new Dictionary<Piece, HashSet<byte>>();
        }

        private PieceData(PieceData other)
        {
            #region Argument Check

            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            #endregion

            this.Pieces = other.Pieces.Copy();
            this.PieceOffsetMap = ChessHelper.CopyPieceOffsetMap(other.PieceOffsetMap);
        }

        #endregion

        #region Internal Properties

        internal Piece[] Pieces
        {
            get;
            private set;
        }

        internal Dictionary<Piece, HashSet<byte>> PieceOffsetMap
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public PieceData Copy()
        {
            return new PieceData(this);
        }

        public Piece GetPiece(Position position)
        {
            return this.Pieces[position.X88Value];
        }

        public PieceInfo GetPieceInfo(Position position)
        {
            var piece = GetPiece(position);
            return new PieceInfo(piece);
        }

        public Position[] GetPiecePositions(Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

            var offsets = this.PieceOffsetMap.GetValueOrDefault(piece);
            if (offsets == null)
            {
                return new Position[0];
            }

            var result = offsets.Select(item => new Position(item)).ToArray();
            return result;
        }

        public EnPassantCaptureInfo GetEnPassantCaptureTarget(PieceMove move)
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

        public CastlingInfo CheckCastlingMove(PieceMove move)
        {
            var pieceInfo = GetPieceInfo(move.From);
            if (pieceInfo.PieceType != PieceType.King || !pieceInfo.Color.HasValue)
            {
                return null;
            }

            var castlingOptions = ChessHelper.ColorToCastlingOptionsMap[pieceInfo.Color.Value];

            var result = ChessHelper.CastlingOptionToInfoMap
                .SingleOrDefault(pair => castlingOptions.Contains(pair.Key) && pair.Value.KingMove == move)
                .Value;

            return result;
        }

        public void GetFenSnippet(StringBuilder resultBuilder)
        {
            #region Argument Check

            if (resultBuilder == null)
            {
                throw new ArgumentNullException("resultBuilder");
            }

            #endregion

            var emptySquareCount = new ValueContainer<int>(0);
            Action writeEmptySquareCount =
                () =>
                {
                    if (emptySquareCount.Value > 0)
                    {
                        resultBuilder.Append(emptySquareCount.Value);
                        emptySquareCount.Value = 0;
                    }
                };

            for (var rank = ChessConstants.RankCount - 1; rank >= 0; rank--)
            {
                if (rank < ChessConstants.RankCount - 1)
                {
                    resultBuilder.Append('/');
                }

                for (var file = 0; file < ChessConstants.FileCount; file++)
                {
                    var piece = GetPiece(new Position((byte)file, (byte)rank));
                    if (piece == Piece.None)
                    {
                        emptySquareCount.Value++;
                        continue;
                    }

                    writeEmptySquareCount();
                    var fenChar = piece.GetFenChar();
                    resultBuilder.Append(fenChar);
                }

                writeEmptySquareCount();
            }
        }

        public string GetFenSnippet()
        {
            var resultBuilder = new StringBuilder();
            GetFenSnippet(resultBuilder);
            return resultBuilder.ToString();
        }

        public Position[] GetAttacks(Position targetPosition, PieceColor attackingColor)
        {
            var resultList = new List<Position>();

            var attackingKnights = ChessHelper.GetKnightMovePositions(targetPosition)
                .Where(p => this.Pieces[p.X88Value].GetColor() == attackingColor)
                .ToArray();

            resultList.AddRange(attackingKnights);

            foreach (var rayOffset in ChessHelper.AllRays)
            {
                for (var currentX88Value = (byte)(targetPosition.X88Value + rayOffset.Offset);
                    Position.IsValidX88Value(currentX88Value);
                    currentX88Value += rayOffset.Offset)
                {
                    var currentPosition = new Position(currentX88Value);

                    var piece = this.Pieces[currentPosition.X88Value];
                    var color = piece.GetColor();
                    if (piece == Piece.None || !color.HasValue)
                    {
                        continue;
                    }

                    if (color.Value != attackingColor)
                    {
                        break;
                    }

                    var pieceType = piece.GetPieceType();
                    if ((pieceType.IsSlidingStraight() && rayOffset.IsStraight)
                        || (pieceType.IsSlidingDiagonally() && !rayOffset.IsStraight))
                    {
                        resultList.Add(currentPosition);
                        break;
                    }

                    var difference = (byte)(targetPosition.X88Value - currentX88Value);
                    switch (pieceType)
                    {
                        case PieceType.Pawn:
                            if (ChessHelper.PawnAttackOffsetMap[attackingColor].Contains(difference))
                            {
                                resultList.Add(currentPosition);
                            }

                            break;

                        case PieceType.King:
                            if (ChessHelper.KingAttackOffsets.Contains(difference))
                            {
                                resultList.Add(currentPosition);
                            }

                            break;
                    }

                    break;
                }
            }

            return resultList.ToArray();
        }

        public bool IsUnderAttack(Position targetPosition, PieceColor attackingColor)
        {
            var attacks = GetAttacks(targetPosition, attackingColor);
            return attacks.Length != 0;
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
            var kingPosition = new Position(this.PieceOffsetMap[king].Single());

            return IsUnderAttack(kingPosition, kingColor.Invert());
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
                    .Where(position => GetPiece(position).GetColor() != pieceColor)
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

        internal void SetupNewPiece(PieceType pieceType, PieceColor color, Position position)
        {
            var offset = position.X88Value;
            var existingPiece = this.Pieces[offset];
            if (existingPiece != Piece.None)
            {
                throw new ChessPlatformException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The board square '{0}' is already occupied by '{1}'.",
                        position,
                        existingPiece));
            }

            var piece = pieceType.ToPiece(color);
            this.Pieces[offset] = piece;

            var added = this.PieceOffsetMap.GetValueOrCreate(piece).Add(offset);
            if (!added)
            {
                throw new ChessPlatformException("Inconsistent state of the piece offset map.");
            }
        }

        //// TODO [vmcl] Remove SetPiece method
        internal Piece SetPiece(Position position, Piece piece)
        {
            var offset = position.X88Value;
            var oldPiece = this.Pieces[offset];
            this.Pieces[offset] = piece;

            return oldPiece;
        }

        internal UndoMoveData MakeMove(PieceMove move, EnPassantCaptureInfo enPassantCaptureInfo)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            PieceMove castlingRookMove = null;
            Position? enPassantCapturedPiecePosition = null;

            var moveData = MovePieceInternal(move);
            var capturedPiece = moveData.CapturedPiece;

            var isEnPassantCapture = IsEnPassantCapture(move, enPassantCaptureInfo);
            if (isEnPassantCapture)
            {
                enPassantCapturedPiecePosition = enPassantCaptureInfo.TargetPiecePosition;
                capturedPiece = RemovePieceInternal(enPassantCaptureInfo.TargetPiecePosition);
                if (capturedPiece.GetPieceType() != PieceType.Pawn)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }
            }
            else
            {
                var castlingInfo = CheckCastlingMove(move);
                if (castlingInfo != null)
                {
                    castlingRookMove = castlingInfo.RookMove;
                    var rookMoveData = MovePieceInternal(castlingRookMove);
                    if (rookMoveData.CapturedPiece != Piece.None)
                    {
                        throw ChessPlatformException.CreateInconsistentStateError();
                    }
                }
            }

            var undoMoveData = new UndoMoveData(move, capturedPiece, castlingRookMove, enPassantCapturedPiecePosition);
            _undoMoveDatas.Push(undoMoveData);

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
                throw new InvalidOperationException("Undo cannot performed: no moves.");
            }

            var undoMoveData = _undoMoveDatas.Pop();
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        private MovePieceData MovePieceInternal(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var movedPiece = this.Pieces[move.From.X88Value];
            if (movedPiece == Piece.None)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The source square of the move {{{0}}} is empty.",
                        move));
            }

            var capturedPiece = this.Pieces[move.To.X88Value];
            if (capturedPiece != Piece.None)
            {
                if (!this.PieceOffsetMap.GetValueOrCreate(capturedPiece).Remove(move.To.X88Value))
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }
            }

            var movedPieceOffsets = this.PieceOffsetMap.GetValueOrCreate(movedPiece);

            if (!movedPieceOffsets.Remove(move.From.X88Value))
            {
                throw ChessPlatformException.CreateInconsistentStateError();
            }

            if (!movedPieceOffsets.Add(move.To.X88Value))
            {
                throw ChessPlatformException.CreateInconsistentStateError();
            }

            return new MovePieceData(movedPiece, capturedPiece);
        }

        private Piece RemovePieceInternal(Position position)
        {
            var x88Value = position.X88Value;
            var piece = this.Pieces[x88Value];

            if (piece != Piece.None)
            {
                this.Pieces[x88Value] = Piece.None;

                var removed = this.PieceOffsetMap.GetValueOrCreate(piece).Remove(x88Value);
                if (!removed)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }
            }

            return piece;
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

                    var currentPiece = this.Pieces[currentPosition.X88Value];
                    var currentColor = currentPiece.GetColor();
                    if (currentPiece == Piece.None || !currentColor.HasValue)
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
                ChessHelper.MaxKingMoveDistance,
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

        #endregion
    }
}