namespace ChessPlatform.Internal
{
    internal struct MovePieceData
    {
        internal MovePieceData(Piece movedPiece, Piece capturedPiece)
        {
            MovedPiece = movedPiece;
            CapturedPiece = capturedPiece;
        }

        public Piece MovedPiece
        {
            get;
        }

        public Piece CapturedPiece
        {
            get;
        }
    }
}