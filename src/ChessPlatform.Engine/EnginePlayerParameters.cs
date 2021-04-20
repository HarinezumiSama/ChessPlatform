using System;
using System.Linq;

namespace ChessPlatform.Engine
{
    public sealed class EnginePlayerParameters
    {
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

        public bool UseTranspositionTable
        {
            get;
            set;
        }

        public int TranspositionTableSizeInMegaBytes
        {
            get;
            set;
        }
    }
}