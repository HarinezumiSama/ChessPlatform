using System;
using System.Globalization;
using System.Linq;

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
        internal PerftResult(int depth, TimeSpan elapsed, ulong nodeCount)
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

            #endregion

            this.Depth = depth;
            this.Elapsed = elapsed;
            this.NodeCount = nodeCount;

            var totalSeconds = elapsed.TotalSeconds;
            this.NodesPerSecond = checked((ulong)(totalSeconds.IsZero() ? 0 : nodeCount / totalSeconds));
        }

        #endregion

        #region Public Properties

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