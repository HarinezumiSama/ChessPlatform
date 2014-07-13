using System;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public enum Piece : byte
    {
        None = PieceType.None,

        [FenChar('P')]
        WhitePawn = PieceType.Pawn | PieceConstants.WhiteColor,

        [FenChar('N')]
        WhiteKnight = PieceType.Knight | PieceConstants.WhiteColor,

        [FenChar('K')]
        WhiteKing = PieceType.King | PieceConstants.WhiteColor,

        [FenChar('B')]
        WhiteBishop = PieceType.Bishop | PieceConstants.WhiteColor,

        [FenChar('R')]
        WhiteRook = PieceType.Rook | PieceConstants.WhiteColor,

        [FenChar('Q')]
        WhiteQueen = PieceType.Queen | PieceConstants.WhiteColor,

        [FenChar('p')]
        BlackPawn = PieceType.Pawn | PieceConstants.BlackColor,

        [FenChar('n')]
        BlackKnight = PieceType.Knight | PieceConstants.BlackColor,

        [FenChar('k')]
        BlackKing = PieceType.King | PieceConstants.BlackColor,

        [FenChar('b')]
        BlackBishop = PieceType.Bishop | PieceConstants.BlackColor,

        [FenChar('r')]
        BlackRook = PieceType.Rook | PieceConstants.BlackColor,

        [FenChar('q')]
        BlackQueen = PieceType.Queen | PieceConstants.BlackColor
    }
}