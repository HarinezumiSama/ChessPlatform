using System;
using System.Linq;

namespace ChessPlatform
{
    public enum GameManagerState
    {
        Paused,
        Running,
        GameFinished,
        UnhandledExceptionOccurred
    }
}