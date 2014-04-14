using System;
using System.Linq;

namespace ChessPlatform
{
    [Flags]
    public enum CastlingOptions : byte
    {
        None = 0,

        [BaseFenChar('K')]
        WhiteKingSide = 0x01,

        [BaseFenChar('Q')]
        WhiteQueenSide = 0x02,

        [BaseFenChar('k')]
        BlackKingSide = 0x04,

        [BaseFenChar('q')]
        BlackQueenSide = 0x08,

        WhiteMask = WhiteKingSide | WhiteQueenSide,
        BlackMask = BlackKingSide | BlackQueenSide,

        All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide
    }
}