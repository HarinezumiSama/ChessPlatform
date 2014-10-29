using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class CastlingTypeExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CastlingOptions ToOption(this CastlingType castlingType)
        {
            return unchecked((CastlingOptions)(1 << (int)castlingType));
        }

        #endregion
    }
}