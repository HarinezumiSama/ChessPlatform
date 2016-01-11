using System;
using System.Linq;

namespace ChessPlatform
{
    public sealed class DoublePushInfo
    {
        #region Constants and Fields

        private const int Difference = 2;

        #endregion

        #region Constructors

        internal DoublePushInfo(PieceColor color)
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

            Color = color;
            StartRank = isWhite ? 1 : ChessConstants.RankCount - 2;

            EndRank = StartRank + (isWhite ? Difference : -Difference);
            CaptureTargetRank = (StartRank + EndRank) / 2;
        }

        #endregion

        #region Public Properties

        public PieceColor Color
        {
            get;
        }

        public int StartRank
        {
            get;
        }

        public int EndRank
        {
            get;
        }

        public int CaptureTargetRank
        {
            get;
        }

        #endregion
    }
}