using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public static class PrincipalVariationInfoExtensions
    {
        #region Constants and Fields

        private static readonly int MateScoreLowerBound = CommonEngineConstants.MateScoreAbs
            - CommonEngineConstants.MaxPlyDepthUpperLimit;

        #endregion

        #region Public Methods

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

        public static int? GetMateMoveDistance([NotNull] this PrincipalVariationInfo principalVariationInfo)
        {
            #region Argument Check

            if (principalVariationInfo == null)
            {
                throw new ArgumentNullException(nameof(principalVariationInfo));
            }

            #endregion

            if (!principalVariationInfo.IsAnyMate())
            {
                return null;
            }

            var score = principalVariationInfo.Value;

            var plyDistance = CommonEngineConstants.MateScoreAbs - score.Abs();
            if (plyDistance < 0)
            {
                throw new InvalidOperationException($@"Invalid PVI score: {score}.");
            }

            var mateMoveDistance = (plyDistance + 1) / 2;
            return principalVariationInfo.Value > 0 ? mateMoveDistance : -mateMoveDistance;
        }

        #endregion
    }
}