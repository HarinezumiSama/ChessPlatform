using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;
using Omnifactotum;

namespace ChessPlatform.Engine
{
    public static class TranspositionTableHelper
    {
        #region Constants and Fields

        public static readonly ValueRange<int> SizeInMegaBytesRange = ValueRange.Create(1, 128 * 1024);

        #endregion

        #region Internal Methods

        //// ReSharper disable once InconsistentNaming
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EvaluationScore ConvertValueForTT(this EvaluationScore score, int plyDistance)
        {
            return score.IsCheckmating()
                ? new EvaluationScore(score.Value + plyDistance)
                : (score.IsGettingCheckmated() ? new EvaluationScore(score.Value - plyDistance) : score);
        }

        //// ReSharper disable once InconsistentNaming
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EvaluationScore ConvertValueFromTT(this EvaluationScore score, int plyDistance)
        {
            return score.IsCheckmating()
                ? new EvaluationScore(score.Value - plyDistance)
                : (score.IsGettingCheckmated() ? new EvaluationScore(score.Value + plyDistance) : score);
        }

        #endregion
    }
}