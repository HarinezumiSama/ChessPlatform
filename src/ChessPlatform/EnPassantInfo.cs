using System;
using System.Linq;

namespace ChessPlatform
{
    public sealed class EnPassantInfo
    {
        #region Constants and Fields

        private const int Difference = 2;

        #endregion

        #region Constructors

        internal EnPassantInfo(bool whiteDirection)
        {
            this.StartRank = (byte)(whiteDirection ? 1 : ChessConstants.RankCount - 2);

            this.EndRank = (byte)(this.StartRank + (whiteDirection ? Difference : -Difference));
            this.CaptureTargetRank = (byte)((this.StartRank + this.EndRank) / 2);
        }

        #endregion

        #region Public Properties

        public byte StartRank
        {
            get;
            private set;
        }

        public byte EndRank
        {
            get;
            private set;
        }

        public byte CaptureTargetRank
        {
            get;
            private set;
        }

        #endregion
    }
}