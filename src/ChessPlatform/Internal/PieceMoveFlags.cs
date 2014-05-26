using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    [Flags]
    public enum PieceMoveFlags
    {
        None = 0,
        IsCapture = 0x01,
        IsPawnPromotion = 0x02
    }
}