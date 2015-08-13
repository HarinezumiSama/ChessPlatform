using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.Internal
{
    [UnsafeValueType]
    internal unsafe struct PiecesData
    {
        #region Constants and Fields

        private const int Length = ChessConstants.X88Length;

        public fixed byte Items[Length];

        #endregion

        #region Constructors

        public PiecesData(PiecesData other)
        {
            fixed (byte* items = Items)
            {
                for (var index = 0; index < Length; index++)
                {
                    items[index] = other.Items[index];
                }
            }
        }

        #endregion
    }
}