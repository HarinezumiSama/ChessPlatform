using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ChessPlatform.GamePlay;

namespace ChessPlatform.Engine
{
    using static TranspositionTableHelper;

    internal sealed class TranspositionTable : IDisposable
    {
        private const long MegaByte = 1L << 20;

        internal static readonly int BucketSizeInBytes = Marshal.SizeOf<TranspositionTableBucket>();

        private readonly ReaderWriterLockSlim _lockSlim;
        private bool _isDisposed;
        private TranspositionTableBucket[] _buckets;
        private uint _version;
        private long _probeCount;
        private long _hitCount;
        private long _saveCount;

        public TranspositionTable(int sizeInMegaBytes)
        {
            _lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            ResetVersionUnsafe();

            Resize(sizeInMegaBytes);
        }

        public long ProbeCount => _probeCount;

        public long HitCount => _hitCount;

        public long SaveCount => _saveCount;

        internal uint Version => _version;

        internal int BucketCount => _buckets?.Length ?? 0;

        public void Clear()
        {
            EnsureNotDisposed();

            _lockSlim.EnterWriteLock();
            try
            {
                ClearUnsafe();
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public void NotifyNewSearch()
        {
            EnsureNotDisposed();

            _lockSlim.EnterWriteLock();
            try
            {
                unchecked
                {
                    _version++;
                    if (_version == 0)
                    {
                        _version = 1;
                    }
                }

                _probeCount = 0;
                _hitCount = 0;
                _saveCount = 0;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public void Resize(int sizeInMegaBytes)
        {
            if (!SizeInMegaBytesRange.Contains(sizeInMegaBytes))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeInMegaBytes),
                    sizeInMegaBytes,
                    $@"The value is out of the valid range ({SizeInMegaBytesRange.Lower:#,##0} .. {
                        SizeInMegaBytesRange.Upper:#,##0}).");
            }

            EnsureNotDisposed();

            var rawCount = checked(Convert.ToInt32(sizeInMegaBytes * MegaByte / BucketSizeInBytes));
            var count = PrimeNumberHelper.FindPrimeNotGreaterThanSpecified(rawCount);

            _lockSlim.EnterWriteLock();
            try
            {
                Array.Resize(ref _buckets, count);
                _buckets.EnsureNotNull();

                ClearUnsafe();
                ResetVersionUnsafe();
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            Trace.WriteLine(
                $@"[{nameof(TranspositionTable)}.{nameof(Resize)}] Requested size: {
                    sizeInMegaBytes:#,##0} MB. Bucket count: {count:#,##0}.");
        }

        public TranspositionTableEntry? Probe(long key)
        {
            EnsureNotDisposed();

            Interlocked.Increment(ref _probeCount);

            TranspositionTableEntry entry1;
            TranspositionTableEntry entry2;

            _lockSlim.EnterReadLock();
            try
            {
                var index = GetIndexUnsafe(key);
                entry1 = _buckets[index].Entry1;
                entry2 = _buckets[index].Entry2;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            var match1 = entry1.Version != 0 && entry1.Key == key;
            var match2 = entry2.Version != 0 && entry2.Key == key;

            if (match1)
            {
                Interlocked.Increment(ref _hitCount);

                return match2
                    ? (entry1.Depth == entry2.Depth
                        ? (entry1.Version > entry2.Version ? entry1 : entry2)
                        : (entry1.Depth > entry2.Depth ? entry1 : entry2))
                    : entry1;
            }

            if (match2)
            {
                Interlocked.Increment(ref _hitCount);
                return entry2;
            }

            return default(TranspositionTableEntry?);
        }

        public void Save(ref TranspositionTableEntry entry)
        {
            if (entry.Score.Value.Abs() > EvaluationScore.MateValue)
            {
                throw new ArgumentException(
                    $@"The entry contains invalid score: {entry.Score.Value:#,##0}.",
                    nameof(entry));
            }

            EnsureNotDisposed();

            var key = entry.Key;

            _lockSlim.EnterUpgradeableReadLock();
            try
            {
                bool shouldOverwriteEntry1;

                var index = GetIndexUnsafe(key);
                var oldEntry1 = _buckets[index].Entry1;
                var oldEntry2 = _buckets[index].Entry2;

                if (oldEntry1.Version == 0 || oldEntry1.Key == key)
                {
                    shouldOverwriteEntry1 = true;
                }
                else if (oldEntry2.Version == 0 || oldEntry2.Key == key)
                {
                    shouldOverwriteEntry1 = false;
                }
                else if ((oldEntry1.Version == _version || oldEntry1.Bound == ScoreBound.Exact)
                    == (oldEntry2.Version == _version || oldEntry2.Bound == ScoreBound.Exact))
                {
                    shouldOverwriteEntry1 = oldEntry1.Depth < oldEntry2.Depth;
                }
                else
                {
                    shouldOverwriteEntry1 = oldEntry1.Version != _version && oldEntry1.Bound != ScoreBound.Exact;
                }

                _lockSlim.EnterWriteLock();
                try
                {
                    entry.Version = _version;
                    if (shouldOverwriteEntry1)
                    {
                        _buckets[index].Entry1 = entry;
                    }
                    else
                    {
                        _buckets[index].Entry2 = entry;
                    }
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }
            }
            finally
            {
                _lockSlim.ExitUpgradeableReadLock();
            }

            Interlocked.Increment(ref _saveCount);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _lockSlim.Dispose();
            Array.Resize(ref _buckets, 0);
            _buckets = null;
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().GetFullName());
            }
        }

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
    }
}