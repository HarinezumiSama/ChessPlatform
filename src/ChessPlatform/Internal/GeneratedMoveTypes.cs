﻿using System;

namespace ChessPlatform.Internal
{
    //// TODO [HarinezumiSama] NEW-DESIGN: Include GeneratedMoveTypes.Check for generating checks separately
    [Flags]
    public enum GeneratedMoveTypes
    {
        Quiet = 1 << 0,
        Capture = 1 << 1,

        All = unchecked((int)0xFFFFFFFF)
    }
}