using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChessPlatform.Engine
{
    using static TranspositionTableHelper;

    internal sealed class TranspositionTable
    {
        #region Constants and Fields

        private const long MegaByte = 1L << 20;

        internal static readonly int BucketSizeInBytes = Marshal.SizeOf<TranspositionTableBucket>();

        private readonly object _syncLock;
        private TranspositionTableBucket[] _buckets;
        private uint _version;
        private long _probeCount;
        private long _hitCount;

        #endregion

        #region Constructors

        public TranspositionTable(int sizeInMegaBytes)
        {
            _syncLock = new object();
            ResetVersionUnsafe();

            Resize(sizeInMegaBytes);
        }

        #endregion

        #region Public Properties

        public long ProbeCount => _probeCount;

        public long HitCount => _hitCount;

        #endregion

        #region Internal Properties

        internal uint Version => _version;

        internal int BucketCount => _buckets?.Length ?? 0;

        #endregion

        #region Public Methods

        public void Clear()
        {
            lock (_syncLock)
            {
                ClearUnsafe();
            }
        }

        public void NotifyNewSearch()
        {
            lock (_syncLock)
            {
                unchecked
                {
                    _version++;
                    if (_version == 0)
                    {
                        _version = 1;
                    }
                }
            }
        }

        public void Resize(int sizeInMegaBytes)
        {
            #region Argument Check

            if (!SizeInMegaBytesRange.Contains(sizeInMegaBytes))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeInMegaBytes),
                    sizeInMegaBytes,
                    $@"The value is out of the valid range ({SizeInMegaBytesRange.Lower:#,##0} .. {
                        SizeInMegaBytesRange.Upper:#,##0}).");
            }

            #endregion

            var rawCount = checked(Convert.ToInt32(sizeInMegaBytes * MegaByte / BucketSizeInBytes));
            var count = PrimeNumberHelper.FindPrimeNotGreaterThanSpecified(rawCount);

            lock (_syncLock)
            {
                Array.Resize(ref _buckets, count);
                _buckets.EnsureNotNull();

                ClearUnsafe();
                ResetVersionUnsafe();
            }

            Trace.WriteLine(
                $@"[{nameof(TranspositionTable)}.{nameof(Resize)}] Requested size: {
                    sizeInMegaBytes:#,##0} MB. Bucket count: {count:#,##0}.");
        }

        public TranspositionTableEntry? Probe(long key)
        {
            Interlocked.Increment(ref _probeCount);

            TranspositionTableEntry entry;
            lock (_syncLock)
            {
                var index = GetIndexUnsafe(key);
                entry = _buckets[index].Entry;
            }

            if (entry.Version == 0 || entry.Key != key)
            {
                return default(TranspositionTableEntry?);
            }

            Interlocked.Increment(ref _hitCount);
            return entry;
        }

        public void Save(ref TranspositionTableEntry entry)
        {
            var key = entry.Key;
            lock (_syncLock)
            {
                var index = GetIndexUnsafe(key);

                var oldEntry = _buckets[index].Entry;
                if (oldEntry.Version != 0 && oldEntry.Key == key && oldEntry.Depth > entry.Depth)
                {
                    return;
                }

                entry.Version = _version;
                _buckets[index].Entry = entry;
            }
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetIndexUnsafe(long key)
        {
            return unchecked((long)(((ulong)key) % (ulong)_buckets.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetVersionUnsafe()
        {
            _version = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearUnsafe()
        {
            _buckets.Initialize();
        }

        #endregion
    }
}