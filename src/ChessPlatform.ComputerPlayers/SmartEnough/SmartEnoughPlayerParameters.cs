using System;
using System.Linq;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    public sealed class SmartEnoughPlayerParameters
    {
        #region Public Properties

        public bool UseOpeningBook
        {
            get;
            set;
        }

        public int MaxPlyDepth
        {
            get;
            set;
        }

        public TimeSpan? MaxTimePerMove
        {
            get;
            set;
        }

        public bool UseMultipleProcessors
        {
            get;
            set;
        }

        #endregion
    }
}