using System;
using System.Linq;

namespace ChessPlatform
{
    public enum Piece : byte
    {
        None = PieceType.None,

        WhiteColor = 0x00,
        BlackColor = 0x08,

        [FenChar('P')]
        WhitePawn = PieceType.Pawn | WhiteColor,

        [FenChar('N')]
        WhiteKnight = PieceType.Knight | WhiteColor,

        [FenChar('K')]
        WhiteKing = PieceType.King | WhiteColor,

        [FenChar('B')]
        WhiteBishop = PieceType.Bishop | WhiteColor,

        [FenChar('R')]
        WhiteRook = PieceType.Rook | WhiteColor,

        [FenChar('Q')]
        WhiteQueen = PieceType.Queen | WhiteColor,

        [FenChar('p')]
        BlackPawn = PieceType.Pawn | BlackColor,

        [FenChar('n')]
        BlackKnight = PieceType.Knight | BlackColor,

        [FenChar('k')]
        BlackKing = PieceType.King | BlackColor,

        [FenChar('b')]
        BlackBishop = PieceType.Bishop | BlackColor,

        [FenChar('r')]
        BlackRook = PieceType.Rook | BlackColor,

        [FenChar('q')]
        BlackQueen = PieceType.Queen | BlackColor,

        TypeMask = 0x07,
        ColorMask = BlackColor
    }
}