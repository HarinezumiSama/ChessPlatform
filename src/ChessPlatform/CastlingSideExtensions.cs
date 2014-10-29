using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class CastlingSideExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CastlingType ToCastlingType(this CastlingSide castlingSide, PieceColor color)
        {
            return unchecked((CastlingType)(((int)color << 1) + castlingSide));
        }

        #endregion
    }
}