using System;
using System.Linq;

namespace ChessPlatform
{
    public enum AutoDrawType
    {
        None = 0,
        InsufficientMaterial = 1,
        ThreefoldRepetition = 2,
        FiftyMoveRule = 3
    }
}