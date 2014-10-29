using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.Internal
{
    internal static class GameMoveFlagsExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnySet(this GameMoveFlags value, GameMoveFlags flags)
        {
            return (value & flags) != 0;
        }

        #endregion
    }
}