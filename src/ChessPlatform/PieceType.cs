using System;
using System.Linq;

namespace ChessPlatform
{
    public enum PieceType : byte
    {
        None = 0x00,

        [FenChar('P')]
        Pawn = 0x01,

        [FenChar('N')]
        Knight = 0x02,

        [FenChar('K')]
        King = 0x03,

        [FenChar('B')]
        Bishop = 0x05,

        [FenChar('R')]
        Rook = 0x06,

        [FenChar('Q')]
        Queen = 0x07
    }
}