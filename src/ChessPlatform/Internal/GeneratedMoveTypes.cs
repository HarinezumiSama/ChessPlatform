using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    [Flags]
    public enum GeneratedMoveTypes
    {
        Quiet = 1 << 0,
        Capture = 1 << 1,

        All = unchecked((int)0xFFFFFFFF)
    }
}