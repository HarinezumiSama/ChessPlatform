using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;
using Omnifactotum;

namespace ChessPlatform.Engine
{
    internal static class TranspositionTableHelper
    {
        #region Constants and Fields

        public static readonly ValueRange<int> SizeInMegaBytesRange = ValueRange.Create(1, 127 * 1024);

        #endregion

        #region Internal Methods

        //// ReSharper disable once InconsistentNaming
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EvaluationScore ConvertValueForTT(this EvaluationScore score, int plyDistance)
        {
            return score.ToLocal(plyDistance);
        }

        //// ReSharper disable once InconsistentNaming
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EvaluationScore ConvertValueFromTT(this EvaluationScore score, int plyDistance)
        {
            return score.ToRelative(plyDistance);
        }

        #endregion
    }
}