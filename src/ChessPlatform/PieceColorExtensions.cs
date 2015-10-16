using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class PieceColorExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceColor EnsureDefined(this PieceColor color)
        {
            switch (color)
            {
                case PieceColor.White:
                case PieceColor.Black:
                    return color;

                default:
                    throw new InvalidEnumArgumentException(nameof(color), (int)color, color.GetType());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceColor Invert(this PieceColor color)
        {
            return (PieceColor)(PieceColor.Black - color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFenSnippet(this PieceColor color)
        {
            return ChessConstants.ColorToFenSnippetMap[color];
        }

        #endregion
    }
}