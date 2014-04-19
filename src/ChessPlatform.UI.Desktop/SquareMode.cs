using System;
using System.Linq;

namespace ChessPlatform.UI.Desktop
{
    internal enum SquareMode
    {
        Default,
        ValidMoveSource,
        ValidMoveTarget,
        CurrentMoveSource,
        CurrentMoveTarget
    }
}