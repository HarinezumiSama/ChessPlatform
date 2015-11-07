using System;
using System.Linq;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers
{
    internal static class LocalHelper
    {
        #region Constants and Fields

        private static readonly int MateScoreLowerBound = EngineConstants.MateScoreAbs
            - EngineConstants.MaxPlyDepthUpperLimit;

        #endregion

        #region Public Methods

        public static string GetTimestamp()
        {
            return DateTimeOffset.Now.ToFixedString();
        }

        public static bool IsAnyMate([NotNull] this PrincipalVariationInfo principalVariationInfo)
        {
            #region Argument Check

            if (principalVariationInfo == null)
            {
                throw new ArgumentNullException(nameof(principalVariationInfo));
            }

            #endregion

            return principalVariationInfo.Value.Abs() >= MateScoreLowerBound;
        }

        public static bool IsCheckmating([NotNull] this PrincipalVariationInfo principalVariationInfo)
        {
            #region Argument Check

            if (principalVariationInfo == null)
            {
                throw new ArgumentNullException(nameof(principalVariationInfo));
            }

            #endregion

            return principalVariationInfo.Value >= MateScoreLowerBound;
        }

        public static bool IsGettingCheckmated([NotNull] this PrincipalVariationInfo principalVariationInfo)
        {
            #region Argument Check

            if (principalVariationInfo == null)
            {
                throw new ArgumentNullException(nameof(principalVariationInfo));
            }

            #endregion

            return -principalVariationInfo.Value >= MateScoreLowerBound;
        }

        #endregion
    }
}