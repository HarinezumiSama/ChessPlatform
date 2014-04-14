using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Omnifactotum;

namespace ChessPlatform
{
    public static class CastlingOptionsExtensions
    {
        #region Constants and Fields

        private static readonly CastlingOptions[] OptionsToCheck =
        {
            CastlingOptions.WhiteKingSide,
            CastlingOptions.WhiteQueenSide,
            CastlingOptions.BlackKingSide,
            CastlingOptions.BlackQueenSide
        };

        private static readonly Dictionary<CastlingOptions, char> CastlingOptionToFenCharMap =
            OptionsToCheck.ToDictionary(Factotum.Identity, item => BaseFenCharAttribute.GetBaseFenCharNonCached(item));

        #endregion

        #region Public Methods

        public static string GetFenSnippet(this CastlingOptions castlingOptions)
        {
            if (castlingOptions == CastlingOptions.None)
            {
                return "-";
            }

            var resultBuilder = new StringBuilder(4);

            foreach (var option in OptionsToCheck)
            {
                if (castlingOptions.IsAnySet(option))
                {
                    var fenChar = CastlingOptionToFenCharMap[option];
                    resultBuilder.Append(fenChar);
                }
            }

            return resultBuilder.ToString();
        }

        #endregion
    }
}