using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct MovePieceData
    {
        #region Constructors

        internal MovePieceData(Piece movedPiece, Piece capturedPiece)
        {
            MovedPiece = movedPiece;
            CapturedPiece = capturedPiece;
        }

        #endregion

        #region Public Properties

        public Piece MovedPiece
        {
            get;
        }

        public Piece CapturedPiece
        {
            get;
        }

        #endregion
    }
}