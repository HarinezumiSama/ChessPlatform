using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.Internal
{
    [UnsafeValueType]
    internal unsafe struct EntireColorBitboardsData
    {
        #region Constants and Fields

        private const int Length = 2;

        private static readonly int ComputedLength = ChessConstants.PieceColors.Max(item => (int)item) + 1;

        public fixed long Bitboards[Length];

        #endregion

        #region Constructors

        static EntireColorBitboardsData()
        {
            if (Length != ComputedLength)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The constant length {0} does not match the computed value {1}.",
                        Length,
                        ComputedLength));
            }
        }

        public EntireColorBitboardsData(EntireColorBitboardsData other)
        {
            fixed (long* bitboards = Bitboards)
            {
                for (var index = 0; index < Length; index++)
                {
                    bitboards[index] = other.Bitboards[index];
                }
            }
        }

        #endregion
    }
}