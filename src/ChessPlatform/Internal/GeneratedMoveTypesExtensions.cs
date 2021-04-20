using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.Internal
{
    public static class GeneratedMoveTypesExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnySet(this GeneratedMoveTypes value, GeneratedMoveTypes flags)
        {
            return (value & flags) != 0;
        }
    }
}