using System;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal sealed class PositionDeterminant : FixedSizeDictionaryDeterminant<Position>
    {
        #region Public Properties

        public override int Size
        {
            get
            {
                return ChessConstants.SquareCount;
            }
        }

        #endregion

        #region Public Methods

        public override int GetIndex(Position key)
        {
            return key.SquareIndex;
        }

        public override Position GetKey(int index)
        {
            return Position.FromSquareIndex(index);
        }

        #endregion
    }
}