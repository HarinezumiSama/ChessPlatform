using System;
using System.Linq;

namespace ChessPlatform.Internal
{
    [Flags]
    public enum GameMoveFlags
    {
        None = 0,
        IsPawnPromotion = 0x01,
        IsRegularCapture = 0x02,
        IsEnPassantCapture = 0x04,
        IsKingCastling = 0x08
    }
}