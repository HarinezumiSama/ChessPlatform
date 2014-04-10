using System;
using System.Linq;

namespace ChessPlatform
{
    public enum Piece : byte
    {
        None = PieceType.None,

        WhiteColor = 0x00,
        BlackColor = 0x08,

        WhitePawn = PieceType.Pawn | WhiteColor,
        WhiteKnight = PieceType.Knight | WhiteColor,
        WhiteKing = PieceType.King | WhiteColor,
        WhiteBishop = PieceType.Bishop | WhiteColor,
        WhiteRook = PieceType.Rook | WhiteColor,
        WhiteQueen = PieceType.Queen | WhiteColor,

        BlackPawn = PieceType.Pawn | BlackColor,
        BlackKnight = PieceType.Knight | BlackColor,
        BlackKing = PieceType.King | BlackColor,
        BlackBishop = PieceType.Bishop | BlackColor,
        BlackRook = PieceType.Rook | BlackColor,
        BlackQueen = PieceType.Queen | BlackColor,

        TypeMask = 0x07,
        ColorMask = BlackColor
    }
}