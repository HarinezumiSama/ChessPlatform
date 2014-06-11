using System;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public enum PieceColor
    {
        [FenChar('w')]
        White = 0,

        [FenChar('b')]
        Black = 1
    }
}