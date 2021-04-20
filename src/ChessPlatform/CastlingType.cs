namespace ChessPlatform
{
    public enum CastlingType
    {
        WhiteKingSide = (GameSide.White << 1) + CastlingSide.KingSide,
        WhiteQueenSide = (GameSide.White << 1) + CastlingSide.QueenSide,
        BlackKingSide = (GameSide.Black << 1) + CastlingSide.KingSide,
        BlackQueenSide = (GameSide.Black << 1) + CastlingSide.QueenSide
    }
}