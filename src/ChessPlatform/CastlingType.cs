namespace ChessPlatform
{
    public enum CastlingType
    {
        // ReSharper disable once ShiftExpressionZeroLeftOperand :: Keeping the same style for the expressions in the enum
        WhiteKingSide = (GameSide.White << 1) + CastlingSide.KingSide,

        // ReSharper disable once ShiftExpressionZeroLeftOperand :: Keeping the same style for the expressions in the enum
        WhiteQueenSide = (GameSide.White << 1) + CastlingSide.QueenSide,

        BlackKingSide = (GameSide.Black << 1) + CastlingSide.KingSide,

        BlackQueenSide = (GameSide.Black << 1) + CastlingSide.QueenSide
    }
}