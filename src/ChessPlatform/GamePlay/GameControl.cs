using System;
using System.Linq;

namespace ChessPlatform.GamePlay
{
    internal sealed class GameControl : IGameControl
    {
        #region Constants and Fields

        private readonly object _syncLock;

        #endregion

        #region Constructors

        public GameControl()
        {
            _syncLock = new object();
        }

        #endregion

        #region Public Properties

        public bool IsMoveNowRequested
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void RequestMoveNow()
        {
            lock (_syncLock)
            {
                IsMoveNowRequested = true;
            }
        }

        #endregion

        #region IGameControl Members

        public void ThrowIfMoveNowIsRequested()
        {
            lock (_syncLock)
            {
                if (IsMoveNowRequested)
                {
                    IsMoveNowRequested = false;
                    throw new MoveNowRequestedException();
                }
            }
        }

        #endregion
    }
}