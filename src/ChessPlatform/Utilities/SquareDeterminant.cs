using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum;

namespace ChessPlatform.Utilities
{
    public sealed class SquareDeterminant : FixedSizeDictionaryDeterminant<Square>
    {
        public override int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ChessConstants.SquareCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetIndex(Square key)
        {
            return key.SquareIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Square GetKey(int index)
        {
            return new Square(index);
        }
    }
}