using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.GamePlay
{
    public static class EvaluationScoreExtensions
    {
        #region Constants and Fields

        private const int MateScoreLowerBound =
            EvaluationScore.MateValue - CommonEngineConstants.MaxPlyDepthUpperLimit;

        #endregion

        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyMate(this EvaluationScore evaluationScore)
        {
            return evaluationScore.Value.Abs() >= MateScoreLowerBound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCheckmating(this EvaluationScore evaluationScore)
        {
            return evaluationScore.Value >= MateScoreLowerBound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGettingCheckmated(this EvaluationScore evaluationScore)
        {
            return -evaluationScore.Value >= MateScoreLowerBound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int? GetMateMoveDistance(this EvaluationScore evaluationScore)
        {
            if (!evaluationScore.IsAnyMate())
            {
                return null;
            }

            var score = evaluationScore.Value;

            var plyDistance = EvaluationScore.MateValue - score.Abs();
            if (plyDistance < 0)
            {
                throw new InvalidOperationException($@"Invalid score value: {score}.");
            }

            var mateMoveDistance = (plyDistance + 1) / 2;
            return evaluationScore.Value > 0 ? mateMoveDistance : -mateMoveDistance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore ToLocal(this EvaluationScore score, int plyDistance)
        {
            return score.IsCheckmating()
                ? new EvaluationScore(score.Value + plyDistance)
                : (score.IsGettingCheckmated() ? new EvaluationScore(score.Value - plyDistance) : score);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore ToRelative(this EvaluationScore score, int plyDistance)
        {
            return score.IsCheckmating()
                ? new EvaluationScore(score.Value - plyDistance)
                : (score.IsGettingCheckmated() ? new EvaluationScore(score.Value + plyDistance) : score);
        }

        #endregion
    }
}