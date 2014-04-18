﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessPlatform
{
    public static class PieceColorExtensions
    {
        #region Public Methods

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