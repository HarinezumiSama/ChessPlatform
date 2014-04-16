using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class UndoMoveData
    {
        #region Constructors

        internal UndoMoveData([NotNull] PieceMove move, Piece capturedPiece, PieceMove castlingRookMove)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            this.Move = move;
            this.CapturedPiece = capturedPiece;
            this.CastlingRookMove = castlingRookMove;
        }

        #endregion

        #region Public Properties

        public PieceMove Move
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

        #endregion
    }
}