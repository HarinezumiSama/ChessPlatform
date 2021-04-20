using System;
using System.Linq;

namespace ChessPlatform.GamePlay
{
    public interface IGameControl
    {
        void ThrowIfMoveNowIsRequested();
    }
}