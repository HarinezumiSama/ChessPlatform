using System;
using System.Linq;

namespace ChessPlatform.ComputerPlayers
{
    internal static class LocalHelper
    {
        #region Public Methods

        public static string GetTimestamp()
        {
            return DateTimeOffset.Now.ToFixedString();
        }

        #endregion
    }
}