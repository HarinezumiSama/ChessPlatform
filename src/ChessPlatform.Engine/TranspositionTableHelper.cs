using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;
using Omnifactotum;

namespace ChessPlatform.Engine
{
    public static class TranspositionTableHelper
    {
        public static readonly ValueRange<int> SizeInMegaBytesRange = ValueRange.Create(1, 127 * 1024);

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
    }
}