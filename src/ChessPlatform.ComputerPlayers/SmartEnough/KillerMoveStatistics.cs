using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class KillerMoveStatistics
    {
        #region Constants and Fields

        private readonly object _syncLock;
        private readonly KillerMoveData[] _datas;

        #endregion

        #region Constructors

        public KillerMoveStatistics()
        {
            _syncLock = new object();
            _datas = new KillerMoveData[EngineConstants.MaxPlyDepthUpperLimit];
        }

        #endregion

        #region Public Methods

        public void RecordKiller(int plyDepth, [NotNull] GameMove move)
        {
            #region Argument Check

            if (plyDepth < EngineConstants.MaxPlyDepthLowerLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDepth),
                    plyDepth,
                    $@"The value cannot be less than {EngineConstants.MaxPlyDepthLowerLimit}.");
            }

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            lock (_syncLock)
            {
                _datas[plyDepth].RecordKiller(move);
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