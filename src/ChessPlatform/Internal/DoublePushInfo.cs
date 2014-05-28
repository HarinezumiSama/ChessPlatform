using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct DoublePushInfo
    {
        #region Constants and Fields

        private readonly Position _destinationPosition;
        private readonly Position _intermediatePosition;

        #endregion

        #region Constructors

        internal DoublePushInfo(Position destinationPosition, Position intermediatePosition)
        {
            #region Argument Check

            if (destinationPosition == intermediatePosition)
            {
                throw new ArgumentException("Positions must be different.");
            }

            #endregion

            _destinationPosition = destinationPosition;
            _intermediatePosition = intermediatePosition;
        }

        #endregion

        #region Public Properties

        public Position DestinationPosition
        {
            [DebuggerStepThrough]
            get
            {
                return _destinationPosition;
            }
        }

        public Position IntermediatePosition
        {
            [DebuggerStepThrough]
            get
            {
                return _intermediatePosition;
            }
        }

        #endregion
    }
}