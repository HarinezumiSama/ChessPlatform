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

        internal DoublePushInfo(GameSide side)
        {
            #region Argument Check

            side.EnsureDefined();

            #endregion

            bool isWhite;
            switch (side)
            {
                case GameSide.White:
                    isWhite = true;
                    break;

                case GameSide.Black:
                    isWhite = false;
                    break;

                default:
                    throw side.CreateEnumValueNotSupportedException();
            }

            Side = side;
            StartRank = isWhite ? 1 : ChessConstants.RankCount - 2;

            EndRank = StartRank + (isWhite ? Difference : -Difference);
            CaptureTargetRank = (StartRank + EndRank) / 2;
        }

        #endregion

        #region Public Properties

        public GameSide Side
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