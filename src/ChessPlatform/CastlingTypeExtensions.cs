using System;
using System.Linq;

namespace ChessPlatform
{
    public static class CastlingTypeExtensions
    {
        #region Public Methods

        public static CastlingOptions ToOption(this CastlingType castlingType)
        {
            return unchecked((CastlingOptions)(1 << (int)castlingType));
        }

        #endregion
    }
}