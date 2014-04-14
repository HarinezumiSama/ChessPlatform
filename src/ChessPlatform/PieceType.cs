using System;
using System.Linq;

namespace ChessPlatform
{
    public enum PieceType : byte
    {
        None = 0x00,

        [BaseFenChar('P')]
        Pawn = 0x01,

        [BaseFenChar('N')]
        Knight = 0x02,

        [BaseFenChar('K')]
        King = 0x03,

        [BaseFenChar('B')]
        Bishop = 0x05,

        [BaseFenChar('R')]
        Rook = 0x06,

        [BaseFenChar('Q')]
        Queen = 0x07,

        SlidingMask = 0x04,

        SlidingDiagonallyMask = SlidingMask | 0x01,

        SlidingStraightMask = SlidingMask | 0x02,

        SlidingAllWaysMask = SlidingDiagonallyMask | SlidingStraightMask
    }
}