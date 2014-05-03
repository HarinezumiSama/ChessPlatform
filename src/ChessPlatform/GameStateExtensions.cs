using System;
using System.Linq;

namespace ChessPlatform
{
    public static class GameStateExtensions
    {
        #region Public Methods

        public static bool IsCheck(this GameState state)
        {
            return state == GameState.Check || state == GameState.DoubleCheck;
        }

        #endregion
    }
}