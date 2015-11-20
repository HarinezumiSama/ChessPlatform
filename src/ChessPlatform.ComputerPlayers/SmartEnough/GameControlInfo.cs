using System;
using System.Linq;
using System.Threading;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class GameControlInfo
    {
        #region Constructors

        public GameControlInfo([NotNull] IGameControl gameControl, CancellationToken cancellationToken)
        {
            GameControl = gameControl.EnsureNotNull();
            CancellationToken = cancellationToken;
        }

        #endregion

        #region Public Properties

        public IGameControl GameControl
        {
            get;
        }

        public CancellationToken CancellationToken
        {
            get;
        }

        public bool IsMoveNowAllowed
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void AllowMoveNow()
        {
            IsMoveNowAllowed = true;
        }

        public void CheckInterruptions()
        {
            CancellationToken.ThrowIfCancellationRequested();

            if (IsMoveNowAllowed)
            {
                GameControl.ThrowIfMoveNowIsRequested();
            }
        }

        #endregion
    }
}