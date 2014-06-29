using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public static class PieceExtensions
    {
        #region Public Methods

        public static PieceColor? GetColor(this Piece piece)
        {
            if (piece == Piece.None)
            {
                return null;
            }

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            var result = (piece & Piece.ColorMask) == Piece.BlackColor ? PieceColor.Black : PieceColor.White;
            return result;
        }

        public static PieceType GetPieceType(this Piece piece)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            var result = (PieceType)(piece & Piece.TypeMask);
            return result;
        }

        public static PieceInfo GetPieceInfo(this Piece piece)
        {
            var result = new PieceInfo(piece);
            return result;
        }

        public static char GetFenChar(this Piece piece)
        {
            char result;
            if (!ChessConstants.PieceToFenCharMap.TryGetValue(piece, out result))
            {
                throw new ArgumentException("Invalid piece.", "piece");
            }

            return result;
        }

        public static string GetDescription(this Piece piece)
        {
            var color = piece.GetColor();
            var pieceType = piece.GetPieceType();

            if (!color.HasValue || pieceType == PieceType.None)
            {
                return "Empty Square";
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", color.Value, pieceType);
        }

        #endregion
    }
}