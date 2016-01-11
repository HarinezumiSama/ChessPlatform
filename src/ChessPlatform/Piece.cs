using System;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public enum Piece : byte
    {
        None = PieceType.None,

        [FenChar('P')]
        WhitePawn = PieceType.Pawn | PieceConstants.WhiteSide,

        [FenChar('N')]
        WhiteKnight = PieceType.Knight | PieceConstants.WhiteSide,

        [FenChar('K')]
        WhiteKing = PieceType.King | PieceConstants.WhiteSide,

        [FenChar('B')]
        WhiteBishop = PieceType.Bishop | PieceConstants.WhiteSide,

        [FenChar('R')]
        WhiteRook = PieceType.Rook | PieceConstants.WhiteSide,

        [FenChar('Q')]
        WhiteQueen = PieceType.Queen | PieceConstants.WhiteSide,

        [FenChar('p')]
        BlackPawn = PieceType.Pawn | PieceConstants.BlackSide,

        [FenChar('n')]
        BlackKnight = PieceType.Knight | PieceConstants.BlackSide,

        [FenChar('k')]
        BlackKing = PieceType.King | PieceConstants.BlackSide,

        [FenChar('b')]
        BlackBishop = PieceType.Bishop | PieceConstants.BlackSide,

        [FenChar('r')]
        BlackRook = PieceType.Rook | PieceConstants.BlackSide,

        [FenChar('q')]
        BlackQueen = PieceType.Queen | PieceConstants.BlackSide
    }
}