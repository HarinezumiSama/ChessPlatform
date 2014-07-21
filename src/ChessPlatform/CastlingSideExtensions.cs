using System;
using System.Linq;

namespace ChessPlatform
{
    public static class CastlingSideExtensions
    {
        #region Public Methods

        public static CastlingType ToCastlingType(this CastlingSide castlingSide, PieceColor color)
        {
            return unchecked((CastlingType)(((int)color << 1) + castlingSide));
        }

        #endregion
    }
}