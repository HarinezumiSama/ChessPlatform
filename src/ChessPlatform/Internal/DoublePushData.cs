using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct DoublePushData
    {
        #region Constructors

        internal DoublePushData(Position destinationPosition, Bitboard emptyPositions)
        {
            #region Argument Check

            if ((destinationPosition.Bitboard & emptyPositions).IsNone)
            {
                throw new ArgumentException("Empty positions should contain destination position.");
            }

            #endregion

            DestinationPosition = destinationPosition;
            EmptyPositions = emptyPositions;
        }

        #endregion

        #region Public Properties

        public Position DestinationPosition
        {
            [DebuggerStepThrough]
            get;
        }

        public Bitboard EmptyPositions
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion
    }
}