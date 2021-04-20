using System;

namespace ChessPlatform.Engine
{
    [Flags]
    internal enum ScoreBound
    {
        None = 0x00,
        Upper = 0x01,
        Lower = 0x02,
        Exact = Lower | Upper
    }
}