using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum;

namespace ChessPlatform.Utilities
{
    public sealed class PositionDeterminant : FixedSizeDictionaryDeterminant<Position>
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
        public override int GetIndex(Position key)
        {
            return key.SquareIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Position GetKey(int index)
        {
            return new Position(index);
        }

        #endregion
    }
}