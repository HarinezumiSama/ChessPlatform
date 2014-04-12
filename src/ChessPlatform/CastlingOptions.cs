using System;
using System.Linq;

namespace ChessPlatform
{
    [Flags]
    public enum CastlingOptions : byte
    {
        None = 0,

        WhiteKingSide = 0x01,
        WhiteQueenSide = 0x02,

        BlackKingSide = 0x04,
        BlackQueenSide = 0x08,

        WhiteMask = WhiteKingSide | WhiteQueenSide,
        BlackMask = BlackKingSide | BlackQueenSide,

        All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide
    }
}