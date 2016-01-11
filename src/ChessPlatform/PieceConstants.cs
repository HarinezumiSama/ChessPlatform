using System;
using System.Linq;

namespace ChessPlatform
{
    internal static class PieceConstants
    {
        #region Constants and Fields

        public const int BlackSideShift = 3;

        public const int WhiteSide = 0x00;
        public const int BlackSide = 1 << BlackSideShift;

        public const int SideMask = BlackSide;
        public const int TypeMask = 0x07;

        #endregion
    }
}