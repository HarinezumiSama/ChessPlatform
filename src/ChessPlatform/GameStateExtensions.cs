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

        public static bool IsGameFinished(this GameState state)
        {
            return state == GameState.Checkmate || state == GameState.Stalemate;
        }

        #endregion
    }
}