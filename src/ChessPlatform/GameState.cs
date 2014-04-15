using System;
using System.Linq;

namespace ChessPlatform
{
    public enum GameState
    {
        Default = 0,
        Check = 1,
        Stalemate = 2,
        Checkmate = 3
    }
}