using System;
using System.Linq;

namespace ChessPlatform
{
    internal static class PieceConstants
    {
        #region Constants and Fields

        public const int BlackColorShift = 3;

        public const int WhiteColor = 0x00;
        public const int BlackColor = 1 << BlackColorShift;

        public const int ColorMask = BlackColor;
        public const int TypeMask = 0x07;

        #endregion
    }
}