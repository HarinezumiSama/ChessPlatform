using System;
using System.Linq;

namespace ChessPlatform
{
    public enum GameState
    {
        Default = 0,
        Check = 10,
        Stalemate = 20,
        ForcedDrawTwoKingsOnly = 30,
        Checkmate = 40
    }
}