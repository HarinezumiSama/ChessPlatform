using System;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public enum Piece : byte
    {
        /// <summary>
        ///     The empty square.
        /// </summary>
        None = PieceType.None,

        /// <summary>
        ///     The white pawn.
        /// </summary>
        [FenChar('P')]
        WhitePawn = PieceType.Pawn | PieceConstants.WhiteSide,

        /// <summary>
        ///     The white knight.
        /// </summary>
        [FenChar('N')]
        WhiteKnight = PieceType.Knight | PieceConstants.WhiteSide,

        /// <summary>
        ///     The white king.
        /// </summary>
        [FenChar('K')]
        WhiteKing = PieceType.King | PieceConstants.WhiteSide,

        /// <summary>
        ///     The white bishop.
        /// </summary>
        [FenChar('B')]
        WhiteBishop = PieceType.Bishop | PieceConstants.WhiteSide,

        /// <summary>
        ///     The white rook.
        /// </summary>
        [FenChar('R')]
        WhiteRook = PieceType.Rook | PieceConstants.WhiteSide,

        /// <summary>
        ///     The white queen.
        /// </summary>
        [FenChar('Q')]
        WhiteQueen = PieceType.Queen | PieceConstants.WhiteSide,

        /// <summary>
        ///     The black pawn.
        /// </summary>
        [FenChar('p')]
        BlackPawn = PieceType.Pawn | PieceConstants.BlackSide,

        /// <summary>
        ///     The black knight.
        /// </summary>
        [FenChar('n')]
        BlackKnight = PieceType.Knight | PieceConstants.BlackSide,

        /// <summary>
        ///     The black king.
        /// </summary>
        [FenChar('k')]
        BlackKing = PieceType.King | PieceConstants.BlackSide,

        /// <summary>
        ///     The black bishop.
        /// </summary>
        [FenChar('b')]
        BlackBishop = PieceType.Bishop | PieceConstants.BlackSide,

        /// <summary>
        ///     The black rook.
        /// </summary>
        [FenChar('r')]
        BlackRook = PieceType.Rook | PieceConstants.BlackSide,

        /// <summary>
        ///     The black queen.
        /// </summary>
        [FenChar('q')]
        BlackQueen = PieceType.Queen | PieceConstants.BlackSide
    }
}