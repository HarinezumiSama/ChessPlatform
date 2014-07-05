﻿using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct DoublePushData
    {
        #region Constants and Fields

        private readonly Position _destinationPosition;
        private readonly Bitboard _emptyPositions;

        #endregion

        #region Constructors

        internal DoublePushData(Position destinationPosition, Bitboard emptyPositions)
        {
            #region Argument Check

            if ((destinationPosition.Bitboard & emptyPositions).IsNone)
            {
                throw new ArgumentException("Empty positions should contain destination position.");
            }

            #endregion

            _destinationPosition = destinationPosition;
            _emptyPositions = emptyPositions;
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

        public Bitboard EmptyPositions
        {
            [DebuggerStepThrough]
            get
            {
                return _emptyPositions;
            }
        }

        #endregion
    }
}