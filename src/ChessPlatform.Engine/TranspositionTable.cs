﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChessPlatform.Engine
{
    internal sealed class TranspositionTable
    {
        #region Constants and Fields

        public const int DefaultSizeInMegaBytes = 16;
        private const int MegaByte = 1 << 20;

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
            _version = 1;

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
                _buckets.Initialize();
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

            if (sizeInMegaBytes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeInMegaBytes),
                    sizeInMegaBytes,
                    @"The value cannot be negative.");
            }

            #endregion

            if (sizeInMegaBytes == 0)
            {
                sizeInMegaBytes = DefaultSizeInMegaBytes;
            }

            var rawCount = sizeInMegaBytes * MegaByte / BucketSizeInBytes;
            var count = 1 << ((int)Math.Truncate(Math.Log(rawCount, 2)));

            lock (_syncLock)
            {
                _buckets = new TranspositionTableBucket[count].EnsureNotNull();
            }

            Trace.WriteLine($@"[{nameof(TranspositionTable)}] New bucket count: {count}.");
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
            var mask = _buckets.Length - 1;
            return key & mask;
        }

        #endregion
    }
}