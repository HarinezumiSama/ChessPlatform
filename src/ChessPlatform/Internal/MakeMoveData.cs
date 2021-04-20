using System;
using System.Diagnostics;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal sealed class MakeMoveData
    {
        internal MakeMoveData(
            [NotNull] GameMove move,
            Piece movedPiece,
            Piece capturedPiece,
            [CanBeNull] GameMove castlingRookMove,
            [CanBeNull] Square? enPassantCapturedPieceSquare)
        {
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

            Move = move;
            MovedPiece = movedPiece;
            CapturedPiece = capturedPiece;
            CastlingRookMove = castlingRookMove;

            CapturedPieceSquare = enPassantCapturedPieceSquare ?? move.To;
        }

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

        internal bool ShouldKeepCountingBy50MoveRule
        {
            [DebuggerNonUserCode]
            get => MovedPiece.GetPieceType() != PieceType.Pawn && CapturedPiece == Piece.None;
        }
    }
}