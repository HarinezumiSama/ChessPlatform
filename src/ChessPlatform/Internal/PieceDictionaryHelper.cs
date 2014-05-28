using System;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal static class PieceDictionaryHelper
    {
        #region Constants and Fields

        public static readonly int MaxIndex = EnumFactotum.GetAllValues<Piece>().Max(item => (int)item);

        public static readonly int Length = MaxIndex + 1;

        #endregion
    }
}