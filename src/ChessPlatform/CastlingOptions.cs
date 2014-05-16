using System;
using System.Linq;
using ChessPlatform.Internal;

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

        KingSideMask = WhiteKingSide | BlackKingSide,
        QueenSideMask = WhiteQueenSide | BlackQueenSide,

        All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide
    }
}