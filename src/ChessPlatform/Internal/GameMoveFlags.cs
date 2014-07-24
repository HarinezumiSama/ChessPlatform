using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    [Flags]
    internal enum GameMoveFlags
    {
        None = 0,
        IsPawnPromotion = 0x01,
        IsCapture = 0x02,
        IsEnPassantCapture = 0x04,
        IsKingCastling = 0x08
    }
}