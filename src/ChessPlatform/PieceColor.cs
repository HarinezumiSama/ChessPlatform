using System;
using System.Linq;

namespace ChessPlatform
{
    public enum PieceColor
    {
        [BaseFenChar('w')]
        White,

        [BaseFenChar('b')]
        Black
    }
}