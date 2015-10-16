using System;
using System.Diagnostics;
using System.Linq;
using ChessPlatform.GamePlay;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal static class LocalConstants
    {
        #region Constants and Fields

        public static readonly int MateScoreAbs = InitializeMateScoreAbs();

        public static readonly int RootAlpha = checked(-MateScoreAbs - 1);

        public static readonly PrincipalVariationInfo RootAlphaInfo = new PrincipalVariationInfo(RootAlpha);

        #endregion

        #region Private Methods

        private static int InitializeMateScoreAbs()
        {
            // ReSharper disable once ConvertToConstant.Local
            var result = 1000000000;

            if (result >= int.MaxValue / 2 || result <= int.MaxValue / 3)
            {
                throw new InvalidOperationException("The value is out of the proper range.");
            }

            return result;
        }

        #endregion
    }
}