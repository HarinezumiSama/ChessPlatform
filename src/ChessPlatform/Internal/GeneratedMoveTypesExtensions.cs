using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    public static class GeneratedMoveTypesExtensions
    {
        #region Public Methods

        public static bool IsAnySet(this GeneratedMoveTypes value, GeneratedMoveTypes flags)
        {
            return (value & flags) != 0;
        }

        #endregion
    }
}