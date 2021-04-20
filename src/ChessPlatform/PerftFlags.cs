using System;

namespace ChessPlatform
{
    [Flags]
    public enum PerftFlags
    {
        None = 0,
        IncludeExtraCountTypes = 0x01,
        IncludeDivideMap = 0x02,
        EnableParallelism = 0x04,
        All = IncludeExtraCountTypes | IncludeDivideMap | EnableParallelism
    }
}