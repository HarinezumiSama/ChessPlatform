using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public readonly struct SquareShift
    {
        public SquareShift(int fileOffset, int rankOffset)
        {
            FileOffset = fileOffset;
            RankOffset = rankOffset;
        }

        public int FileOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get;
        }

        public int RankOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [DebuggerStepThrough]
            get;
        }
    }
}