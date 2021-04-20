using System;
using System.Collections.Generic;
using Omnifactotum;

namespace ChessPlatform
{
    [CLSCompliant(false)]
    public sealed class PerftResult
    {
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

        public override string ToString()
        {
            return $@"{{Perft({Depth}) = {NodeCount} [{Elapsed}, {NodesPerSecond} nps]}}";
        }
    }
}