using System;
using System.Linq;

namespace ChessPlatform
{
    internal struct MovePieceData
    {
        #region Constructors

        internal MovePieceData(Piece movedPiece, Piece capturedPiece)
            : this()
        {
            this.MovedPiece = movedPiece;
            this.CapturedPiece = capturedPiece;
        }

        #endregion

        #region Public Properties

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

        #endregion
    }
}