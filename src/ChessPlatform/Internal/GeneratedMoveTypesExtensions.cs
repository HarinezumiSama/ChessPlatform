using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    public static class GeneratedMoveTypesExtensions
    {
        #region Public Methods

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnySet(this GeneratedMoveTypes value, GeneratedMoveTypes flags)
        {
            return (value & flags) != 0;
        }

        #endregion
    }
}