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
        WhiteKingSide = 1 << CastlingType.WhiteKingSide,

        [FenChar('Q')]
        WhiteQueenSide = 1 << CastlingType.WhiteQueenSide,

        [FenChar('k')]
        BlackKingSide = 1 << CastlingType.BlackKingSide,

        [FenChar('q')]
        BlackQueenSide = 1 << CastlingType.BlackQueenSide,

        WhiteMask = WhiteKingSide | WhiteQueenSide,
        BlackMask = BlackKingSide | BlackQueenSide,

        KingSideMask = WhiteKingSide | BlackKingSide,
        QueenSideMask = WhiteQueenSide | BlackQueenSide,

        All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide
    }
}