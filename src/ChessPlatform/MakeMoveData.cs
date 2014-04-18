using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class MakeMoveData
    {
        #region Constructors

        internal MakeMoveData(
            [NotNull] PieceMove move,
            Piece movedPiece,
            Piece capturedPiece,
            [CanBeNull] PieceMove castlingRookMove,
            [CanBeNull] Position? enPassantCapturedPiecePosition)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            if (movedPiece == Piece.None)
            {
                throw new ArgumentException("Invalid moved piece.", "movedPiece");
            }

            if (castlingRookMove != null && enPassantCapturedPiecePosition.HasValue)
            {
                throw new ArgumentException("Castling and en passant capture could not occur simultaneously.");
            }

            if (castlingRookMove != null && capturedPiece != Piece.None)
            {
                throw new ArgumentException("Castling and capture could not occur simultaneously.");
            }

            #endregion

            this.Move = move;
            this.MovedPiece = movedPiece;
            this.CapturedPiece = capturedPiece;
            this.CastlingRookMove = castlingRookMove;

            this.CapturedPiecePosition = enPassantCapturedPiecePosition.HasValue
                ? enPassantCapturedPiecePosition.Value
                : move.To;
        }

        #endregion

        #region Public Properties

        public PieceMove Move
        {
            get;
            private set;
        }

        public Piece MovedPiece
        {
            get;
            private set;
        }

        public Piece CapturedPiece
        {
            get;
            private set;
        }

        public PieceMove CastlingRookMove
        {
            get;
            private set;
        }

        public Position CapturedPiecePosition
        {
            get;
            private set;
        }

        #endregion

        #region Internal Properties

        internal bool ShouldKeepCountingBy50MoveRule
        {
            [DebuggerNonUserCode]
            get
            {
                return this.MovedPiece.GetPieceType() != PieceType.Pawn && this.CapturedPiece == Piece.None;
            }
        }

        #endregion
    }
}