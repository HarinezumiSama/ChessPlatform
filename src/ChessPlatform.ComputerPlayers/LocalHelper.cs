using System;
using System.Linq;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

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