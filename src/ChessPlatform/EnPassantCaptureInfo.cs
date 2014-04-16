using System;
using System.Linq;

namespace ChessPlatform
{
    public sealed class EnPassantCaptureInfo
    {
        #region Constructors

        internal EnPassantCaptureInfo(Position capturePosition, Position targetPiecePosition)
        {
            this.CapturePosition = capturePosition;
            this.TargetPiecePosition = targetPiecePosition;
        }

        #endregion

        #region Public Properties

        public Position CapturePosition
        {
            get;
            private set;
        }

        public Position TargetPiecePosition
        {
            get;
            private set;
        }

        #endregion
    }
}