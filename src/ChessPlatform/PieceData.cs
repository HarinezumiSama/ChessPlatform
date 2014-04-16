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
            var moveData = MovePieceInternal(move);
            var capturedPiece = moveData.CapturedPiece;

            var isEnPassantCapture = IsEnPassantCapture(move, enPassantCaptureInfo);
            if (isEnPassantCapture)
            {
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

            var undoMoveData = new UndoMoveData(move, capturedPiece, castlingRookMove);
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

        #endregion
    }
}