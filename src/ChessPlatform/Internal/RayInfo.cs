using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct RayInfo
    {
        #region Constructors

        internal RayInfo(byte offset, bool isStraight)
            : this()
        {
            Offset = offset;
            IsStraight = isStraight;
        }

        #endregion

        #region Public Properties

        public byte Offset
        {
            get;
        }

        public bool IsStraight
        {
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{{0x{Offset:X2}, {(IsStraight ? "Straight" : "Diagonal")}}}";
        }

        #endregion
    }
}