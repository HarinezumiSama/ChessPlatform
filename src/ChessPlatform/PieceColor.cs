using System;
using System.Linq;
using ChessPlatform.Internal;

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