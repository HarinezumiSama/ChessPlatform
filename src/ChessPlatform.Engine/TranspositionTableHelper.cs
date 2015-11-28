using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;

namespace ChessPlatform.Engine
{
    internal static class TranspositionTableHelper
    {
        #region Public Methods

        //// ReSharper disable once InconsistentNaming
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore ConvertValueForTT(this EvaluationScore score, int plyDistance)
        {
            return score.IsCheckmating()
                ? new EvaluationScore(score.Value + plyDistance)
                : (score.IsGettingCheckmated() ? new EvaluationScore(score.Value - plyDistance) : score);
        }

        //// ReSharper disable once InconsistentNaming
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore ConvertValueFromTT(this EvaluationScore score, int plyDistance)
        {
            return score.IsCheckmating()
                ? new EvaluationScore(score.Value - plyDistance)
                : (score.IsGettingCheckmated() ? new EvaluationScore(score.Value + plyDistance) : score);
        }

        #endregion
    }
}