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
            ulong captureCount,
            ulong enPassantCaptureCount,
            IDictionary<GameMove, ulong> dividedMoves,
            ulong? checkCount,
            ulong? checkmateCount)
        {
            #region Argument Check

            if (depth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth),
                    depth,
                    @"The value cannot be negative.");
            }

            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsed),
                    elapsed,
                    @"The value cannot be negative.");
            }

            if (dividedMoves == null)
            {
                throw new ArgumentNullException(nameof(dividedMoves));
            }

            #endregion

            Flags = flags;
            Depth = depth;
            Elapsed = elapsed;
            NodeCount = nodeCount;
            CaptureCount = captureCount;
            EnPassantCaptureCount = enPassantCaptureCount;
            DividedMoves = dividedMoves.AsReadOnly();
            CheckCount = checkCount;
            CheckmateCount = checkmateCount;

            var totalSeconds = elapsed.TotalSeconds;
            NodesPerSecond = checked((ulong)(totalSeconds.IsZero() ? 0 : nodeCount / totalSeconds));
        }

        #endregion

        #region Public Properties

        public PerftFlags Flags
        {
            get;
        }

        public int Depth
        {
            get;
        }

        public TimeSpan Elapsed
        {
            get;
        }

        public ulong NodeCount
        {
            get;
        }

        public ulong CaptureCount
        {
            get;
        }

        public ulong EnPassantCaptureCount
        {
            get;
        }

        public ReadOnlyDictionary<GameMove, ulong> DividedMoves
        {
            get;
        }

        public ulong? CheckCount
        {
            get;
        }

        public ulong? CheckmateCount
        {
            get;
        }

        public ulong NodesPerSecond
        {
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{Perft({0}) = {1} [{2}, {3} nps]}}",
                Depth,
                NodeCount,
                Elapsed,
                NodesPerSecond);
        }

        #endregion
    }
}