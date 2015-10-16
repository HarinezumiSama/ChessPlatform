using System;
using System.Globalization;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct RayInfo
    {
        #region Constructors

        internal RayInfo(byte offset, bool isStraight)
            : this()
        {
            this.Offset = offset;
            this.IsStraight = isStraight;
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
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{0x{0:X2}, {1}}}",
                this.Offset,
                this.IsStraight ? "Straight" : "Diagonal");
        }

        #endregion
    }
}