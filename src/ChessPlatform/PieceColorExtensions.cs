using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ChessPlatform
{
    public static class PieceColorExtensions
    {
        #region Public Methods

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceColor EnsureDefined(this PieceColor color)
        {
            switch (color)
            {
                case PieceColor.White:
                case PieceColor.Black:
                    return color;

                default:
                    throw new InvalidEnumArgumentException("color", (int)color, color.GetType());
            }
        }

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceColor Invert(this PieceColor color)
        {
            return (PieceColor)(PieceColor.Black - color);
        }

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFenSnippet(this PieceColor color)
        {
            return ChessConstants.ColorToFenSnippetMap[color];
        }

        #endregion
    }
}