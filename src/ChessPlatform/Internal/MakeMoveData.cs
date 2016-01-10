using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal sealed class MakeMoveData
    {
        #region Constructors

        internal MakeMoveData(
            [NotNull] GameMove move,
            Piece movedPiece,
            Piece capturedPiece,
            [CanBeNull] GameMove castlingRookMove,
            [CanBeNull] Square? enPassantCapturedPieceSquare)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (movedPiece == Piece.None)
            {
                throw new ArgumentException("Invalid moved piece.", nameof(movedPiece));
            }

            if (castlingRookMove != null && enPassantCapturedPieceSquare.HasValue)
            {
                throw new ArgumentException("Castling and en passant capture could not occur simultaneously.");
            }

            if (castlingRookMove != null && capturedPiece != Piece.None)
            {
                throw new ArgumentException("Castling and capture could not occur simultaneously.");
            }

            #endregion

            Move = move;
            MovedPiece = movedPiece;
            CapturedPiece = capturedPiece;
            CastlingRookMove = castlingRookMove;

            CapturedPieceSquare = enPassantCapturedPieceSquare ?? move.To;
        }

        #endregion

        #region Public Properties

        public GameMove Move
        {
            get;
        }

        public Piece MovedPiece
        {
            get;
        }

        public Piece CapturedPiece
        {
            get;
        }

        public GameMove CastlingRookMove
        {
            get;
        }

        public Square CapturedPieceSquare
        {
            get;
        }

        #endregion

        #region Internal Properties

        internal bool ShouldKeepCountingBy50MoveRule
        {
            [DebuggerNonUserCode]
            get
            {
                return MovedPiece.GetPieceType() != PieceType.Pawn && CapturedPiece == Piece.None;
            }
        }

        #endregion
    }
}