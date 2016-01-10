using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum;

namespace ChessPlatform.Utilities
{
    public sealed class SquareDeterminant : FixedSizeDictionaryDeterminant<Square>
    {
        #region Public Properties

        public override int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ChessConstants.SquareCount;
            }
        }

        #endregion

        #region Public Methods

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

        #endregion
    }
}