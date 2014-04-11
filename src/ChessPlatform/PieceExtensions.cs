using System;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public static class PieceExtensions
    {
        #region Public Methods

        public static PieceColor? GetColor(this Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

            if (piece == Piece.None)
            {
                return null;
            }

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            return (piece & Piece.ColorMask) == Piece.BlackColor ? PieceColor.Black : PieceColor.White;
        }

        public static PieceType GetPieceType(this Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            return (PieceType)(piece & Piece.TypeMask);
        }

        public static char GetFenChar(this Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

            var color = piece.GetColor();
            if (!color.HasValue)
            {
                throw new ArgumentException("Empty board square must be encoded in a different way.", "piece");
            }

            var baseFenChar = piece.GetPieceType().GetBaseFenChar();

            var result = color == PieceColor.White
                ? char.ToUpperInvariant(baseFenChar)
                : char.ToLowerInvariant(baseFenChar);

            return result;
        }

        public static string GetDescription(this Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

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