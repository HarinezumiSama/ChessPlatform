using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.Engine
{
    internal static class LocalHelper
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTimestamp()
        {
            return DateTimeOffset.Now.ToFixedString();
        }

        #endregion
    }
}