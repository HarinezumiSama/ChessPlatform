using System;
using System.Linq;

namespace ChessPlatform
{
    public static class GameStateExtensions
    {
        #region Public Methods

        /// <summary>
        ///     Determines whether the specified state indicates a check (but not a checkmate).
        /// </summary>
        /// <param name="state">
        ///     The state to verify.
        /// </param>
        /// <returns>
        ///     <b>true</b> the specified state indicates a check (but not a checkmate);
        ///     otherwise, <b>false</b>.
        /// </returns>
        public static bool IsCheck(this GameState state)
        {
            switch (state)
            {
                case GameState.DoubleCheck:
                case GameState.Check:
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Determines whether the specified state indicates a check or a checkmate.
        /// </summary>
        /// <param name="state">
        ///     The state to verify.
        /// </param>
        /// <returns>
        ///     <b>true</b> the specified state indicates a check or a checkmate;
        ///     otherwise, <b>false</b>.
        /// </returns>
        public static bool IsAnyCheck(this GameState state)
        {
            return IsCheck(state) || state == GameState.Checkmate;
        }

        /// <summary>
        ///     Determines whether the specified state indicates that the game is strictly finished.
        /// </summary>
        /// <param name="state">
        ///     The state to verify.
        /// </param>
        /// <returns>
        ///     <b>true</b> the specified state indicates that the game is strictly finished;
        ///     otherwise, <b>false</b>.
        /// </returns>
        public static bool IsGameFinished(this GameState state)
        {
            return state == GameState.Checkmate || state == GameState.Stalemate;
        }

        #endregion
    }
}