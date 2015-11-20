using System;
using System.Linq;

namespace ChessPlatform.GamePlay
{
    public interface IGameControl
    {
        #region Methods

        void ThrowIfMoveNowIsRequested();

        #endregion
    }
}