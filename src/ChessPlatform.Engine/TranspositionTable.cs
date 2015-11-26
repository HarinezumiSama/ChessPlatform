using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ChessPlatform.Engine
{
    internal sealed class TranspositionTable
    {
        #region Constants and Fields

        public const int DefaultSizeInMB = 16;

        private readonly object _syncLock;
        private TranspositionTableEntry[] _entries;
        private int _version;
        private long _probeCount;
        private long _hitCount;

        #endregion

        #region Constructors

        public TranspositionTable()
        {
            _syncLock = new object();
            Resize(DefaultSizeInMB);
        }

        #endregion

        #region Public Properties

        public long ProbeCount => _probeCount;

        public long HitCount => _hitCount;

        #endregion

        #region Public Methods

        public void NotifyNewSearch()
        {
            Interlocked.Increment(ref _version);
        }

        public void Resize(int sizeInMB)
        {
            #region Argument Check

            if (sizeInMB <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeInMB),
                    sizeInMB,
                    @"The value must be positive.");
            }

            #endregion

            var rawCount = sizeInMB * 1024 * 1024 / TranspositionTableEntry.SizeInBytes;
            var count = 1 << ((int)Math.Truncate(Math.Log(rawCount, 2)));

            lock (_syncLock)
            {
                _entries = new TranspositionTableEntry[count].EnsureNotNull();
            }
        }

        public TranspositionTableEntry? Probe(long key)
        {
            Interlocked.Increment(ref _probeCount);

            TranspositionTableEntry entry;
            lock (_syncLock)
            {
                var index = GetIndexUnsafe(key);
                entry = _entries[index];
            }

            if (entry.Key != key)
            {
                return default(TranspositionTableEntry?);
            }

            Interlocked.Increment(ref _hitCount);
            return entry;
        }

        public void Save(TranspositionTableEntry entry)
        {
            lock (_syncLock)
            {
                var key = entry.Key;

                var index = GetIndexUnsafe(key);

                var oldEntry = _entries[index];
                if (oldEntry.Key == key && entry.Depth < oldEntry.Depth)
                {
                    return;
                }

                entry.Version = _version;
                _entries[index] = entry;
            }
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetIndexUnsafe(long key)
        {
            var mask = _entries.Length - 1;
            return key & mask;
        }

        #endregion
    }
}