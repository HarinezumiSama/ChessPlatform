using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessPlatform
{
    public static class CastlingOptionsExtensions
    {
        public static string GetFenSnippet(this CastlingOptions castlingOptions)
        {
            if (castlingOptions == CastlingOptions.None)
            {
                return ChessConstants.NoneCastlingOptionsFenSnippet;
            }

            var resultBuilder = new StringBuilder(4);

            foreach (var option in ChessConstants.FenRelatedCastlingOptions)
            {
                if (castlingOptions.IsAnySet(option))
                {
                    var fenChar = ChessConstants.CastlingOptionToFenCharMap[option];
                    resultBuilder.Append(fenChar);
                }
            }

            return resultBuilder.ToString();
        }
    }
}