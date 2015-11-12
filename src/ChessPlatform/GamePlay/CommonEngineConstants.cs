using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform.GamePlay
{
    public static class CommonEngineConstants
    {
        #region Constants and Fields

        public const int MaxPlyDepthLowerLimit = 1;

        public const int MaxPlyDepthUpperLimit = 100;

        public static readonly int MateScoreAbs = InitializeMateScoreAbs();

        public static readonly int RootAlphaValue = checked(-MateScoreAbs - 1);

        public static readonly PrincipalVariationInfo RootAlphaInfo = new PrincipalVariationInfo(RootAlphaValue);

        public static readonly PrincipalVariationInfo RootBetaInfo = -RootAlphaInfo;

        public static readonly int ScoreInfinite = checked(MateScoreAbs * 2);

        public static readonly PrincipalVariationInfo InfiniteInfo = new PrincipalVariationInfo(ScoreInfinite);

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