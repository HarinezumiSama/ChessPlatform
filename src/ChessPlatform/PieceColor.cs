using System;
using System.Linq;

namespace ChessPlatform
{
    public enum PieceColor
    {
        [FenChar('w')]
        White,

        [FenChar('b')]
        Black
    }
}