using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    /// <summary>
    ///     Contains the extension methods for the <see cref="Square"/> structure.
    /// </summary>
    public static class SquareExtensions
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDark(this Square square) => ((square.File + square.Rank) & 0x1) == 0;
    }
}