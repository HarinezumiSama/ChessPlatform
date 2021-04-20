using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class CastlingTypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CastlingOptions ToOption(this CastlingType castlingType)
        {
            return unchecked((CastlingOptions)(1 << (int)castlingType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CastlingSide GetSide(this CastlingType castlingType)
        {
            //// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            //// ReSharper disable once RedundantOverflowCheckingContext
            return (CastlingSide)unchecked((int)castlingType & 1);
        }
    }
}