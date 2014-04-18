using System;
using System.Linq;

namespace ChessPlatform
{
    [Flags]
    public enum CastlingOptions : byte
    {
        None = 0,

        [FenChar('K')]
        WhiteKingSide = 0x01,

        [FenChar('Q')]
        WhiteQueenSide = 0x02,

        [FenChar('k')]
        BlackKingSide = 0x04,

        [FenChar('q')]
        BlackQueenSide = 0x08,

        WhiteMask = WhiteKingSide | WhiteQueenSide,
        BlackMask = BlackKingSide | BlackQueenSide,

        All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide
    }
}