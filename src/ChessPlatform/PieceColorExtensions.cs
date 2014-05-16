using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public static class PieceColorExtensions
    {
        #region Public Methods

        public static PieceColor EnsureDefined(this PieceColor color)
        {
            if (DebugConstants.EnsureEnumValuesDefined && !ChessConstants.PieceColors.Contains(color))
            {
                throw new InvalidEnumArgumentException("color", (int)color, color.GetType());
            }

            return color;
        }

        public static PieceColor Invert(this PieceColor color)
        {
            #region Argument Check

            color.EnsureDefined();

            #endregion

            return color == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        }

        public static string GetFenSnippet(this PieceColor color)
        {
            return ChessConstants.ColorToFenSnippetMap[color];
        }

        #endregion
    }
}