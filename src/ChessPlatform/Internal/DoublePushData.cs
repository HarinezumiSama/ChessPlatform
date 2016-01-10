using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct DoublePushData
    {
        #region Constructors

        internal DoublePushData(Square destinationSquare, Bitboard emptySquares)
        {
            #region Argument Check

            if ((destinationSquare.Bitboard & emptySquares).IsNone)
            {
                throw new ArgumentException("Empty squares should contain destination square.");
            }

            #endregion

            DestinationSquare = destinationSquare;
            EmptySquares = emptySquares;
        }

        #endregion

        #region Public Properties

        public Square DestinationSquare
        {
            [DebuggerStepThrough]
            get;
        }

        public Bitboard EmptySquares
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion
    }
}