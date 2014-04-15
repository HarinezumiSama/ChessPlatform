using System;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public static class PieceColorExtensions
    {
        #region Constants and Fields

        private static readonly Dictionary<PieceColor, char> ColorToFenCharMap =
            ChessConstants.PieceColors.ToDictionary(
                Factotum.Identity,
                item => BaseFenCharAttribute.GetBaseFenCharNonCached(item));

        #endregion

        #region Public Methods

        public static PieceColor Invert(this PieceColor color)
        {
            #region Argument Check

            color.EnsureDefined();

            #endregion

            return color == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        }

        public static char GetFenSnippet(this PieceColor color)
        {
            return ColorToFenCharMap[color];
        }

        #endregion
    }
}