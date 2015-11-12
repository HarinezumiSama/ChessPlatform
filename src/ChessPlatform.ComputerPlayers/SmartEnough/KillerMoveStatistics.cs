using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class KillerMoveStatistics
    {
        #region Constants and Fields

        public const int MinDepth = 1;
        public const int MaxDepth = CommonEngineConstants.MaxPlyDepthUpperLimit;

        public static readonly ValueRange<int> DepthRange = ValueRange.Create(MinDepth, MaxDepth);

        private readonly object _syncLock;
        private readonly KillerMoveData[] _datas;

        #endregion

        #region Constructors

        public KillerMoveStatistics()
        {
            _syncLock = new object();
            _datas = new KillerMoveData[MaxDepth];
        }

        #endregion

        #region Public Properties

        public KillerMoveData this[int plyDepth]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (_syncLock)
                {
                    return _datas[plyDepth - 1];
                }
            }
        }

        #endregion

        #region Public Methods

        public void RecordKiller(int plyDepth, [NotNull] GameMove move)
        {
            #region Argument Check

            if (plyDepth < MinDepth || plyDepth > MaxDepth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDepth),
                    plyDepth,
                    $@"The value is out of the valid range ({MinDepth} .. {MaxDepth}).");
            }

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            lock (_syncLock)
            {
                _datas[plyDepth - 1].RecordKiller(move);
            }
        }

        public KillerMoveData[] GetKillersData()
        {
            lock (_syncLock)
            {
                return _datas.ToArray();
            }
        }

        #endregion
    }
}