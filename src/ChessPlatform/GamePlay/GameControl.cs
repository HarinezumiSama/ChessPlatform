using System;
using System.Linq;

namespace ChessPlatform.GamePlay
{
    internal sealed class GameControl : IGameControl
    {
        private readonly object _syncLock;

        public GameControl()
        {
            _syncLock = new object();
        }

        public bool IsMoveNowRequested
        {
            get;
            private set;
        }

        public void RequestMoveNow()
        {
            lock (_syncLock)
            {
                IsMoveNowRequested = true;
            }
        }

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
    }
}