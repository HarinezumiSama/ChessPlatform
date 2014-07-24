using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal static class GameMoveFlagsExtensions
    {
        #region Public Methods

        public static bool IsAnySet(this GameMoveFlags value, GameMoveFlags flags)
        {
            return (value & flags) != 0;
        }

        #endregion
    }
}