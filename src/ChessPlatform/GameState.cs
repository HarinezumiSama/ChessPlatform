﻿using System;
using System.Linq;

namespace ChessPlatform
{
    public enum GameState
    {
        Stalemate = -2,
        ForcedDrawInsufficientMaterial = -1,
        Default = 0,
        Check = 1,
        DoubleCheck = 2,
        Checkmate = 3
    }
}