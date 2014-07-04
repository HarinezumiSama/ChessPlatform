using System;
using System.Linq;

namespace ChessPlatform.GamePlay
{
    public enum GameManagerState
    {
        Paused,
        Running,
        GameFinished,
        UnhandledExceptionOccurred
    }
}