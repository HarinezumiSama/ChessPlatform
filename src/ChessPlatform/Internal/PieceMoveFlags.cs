using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    [Flags]
    public enum PieceMoveFlags
    {
        None = 0,
        IsPawnPromotion = 0x01,
        IsCapture = 0x02,
        IsEnPassantCapture = 0x04
    }
}