using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    //// TODO [vmcl] Count also: captures, promotions, checks etc.

    [CLSCompliant(false)]
    public sealed class PerftResult
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PerftResult"/> class.
        /// </summary>
        internal PerftResult(
            PerftFlags flags,
            int depth,
            TimeSpan elapsed,
            ulong nodeCount,
            IDictionary<GameMove, ulong> dividedMoves,
            ulong? checkCount,
            ulong? checkmateCount)
        {
            #region Argument Check

            if (depth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "depth",
                    depth,
                    @"The value cannot be negative.");
            }

            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    "elapsed",
                    elapsed,
                    @"The value cannot be negative.");
            }

            if (dividedMoves == null)
            {
                throw new ArgumentNullException("dividedMoves");
            }

            #endregion

            this.Flags = flags;
            this.Depth = depth;
            this.Elapsed = elapsed;
            this.NodeCount = nodeCount;
            this.DividedMoves = dividedMoves.AsReadOnly();
            this.CheckCount = checkCount;
            this.CheckmateCount = checkmateCount;

            var totalSeconds = elapsed.TotalSeconds;
            this.NodesPerSecond = checked((ulong)(totalSeconds.IsZero() ? 0 : nodeCount / totalSeconds));
        }

        #endregion

        #region Public Properties

        public PerftFlags Flags
        {
            get;
            private set;
        }

        public int Depth
        {
            get;
            private set;
        }

        public TimeSpan Elapsed
        {
            get;
            private set;
        }

        public ulong NodeCount
        {
            get;
            private set;
        }

        public ReadOnlyDictionary<GameMove, ulong> DividedMoves
        {
            get;
            private set;
        }

        public ulong? CheckCount
        {
            get;
            private set;
        }

        public ulong? CheckmateCount
        {
            get;
            private set;
        }

        public ulong NodesPerSecond
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{Perft({0}) = {1} [{2}, {3} nps]}}",
                this.Depth,
                this.NodeCount,
                this.Elapsed,
                this.NodesPerSecond);
        }

        #endregion
    }
}