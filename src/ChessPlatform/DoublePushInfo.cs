using System;
using System.Linq;

namespace ChessPlatform
{
    public sealed class DoublePushInfo
    {
        private const int Difference = 2;

        internal DoublePushInfo(GameSide side)
        {
            side.EnsureDefined();

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
    }
}