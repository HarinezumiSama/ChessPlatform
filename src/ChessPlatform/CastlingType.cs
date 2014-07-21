using System;
using System.Linq;

namespace ChessPlatform
{
    public enum CastlingType
    {
        WhiteKingSide = (PieceColor.White << 1) + CastlingSide.KingSide,
        WhiteQueenSide = (PieceColor.White << 1) + CastlingSide.QueenSide,
        BlackKingSide = (PieceColor.Black << 1) + CastlingSide.KingSide,
        BlackQueenSide = (PieceColor.Black << 1) + CastlingSide.QueenSide
    }
}