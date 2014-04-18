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

        internal EnPassantInfo(PieceColor color)
        {
            #region Argument Check

            color.EnsureDefined();

            #endregion

            bool isWhite;
            switch (color)
            {
                case PieceColor.White:
                    isWhite = true;
                    break;

                case PieceColor.Black:
                    isWhite = false;
                    break;

                default:
                    throw color.CreateEnumValueNotSupportedException();
            }

            this.Color = color;
            this.StartRank = (byte)(isWhite ? 1 : ChessConstants.RankCount - 2);

            this.EndRank = (byte)(this.StartRank + (isWhite ? Difference : -Difference));
            this.CaptureTargetRank = (byte)((this.StartRank + this.EndRank) / 2);
        }

        #endregion

        #region Public Properties

        public PieceColor Color
        {
            get;
            private set;
        }

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