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

            var result = (PieceColor)(((int)piece & PieceConstants.ColorMask) >> PieceConstants.BlackColorShift);
            return result;
        }

        public static PieceType GetPieceType(this Piece piece)
        {
            var result = (PieceType)((int)piece & PieceConstants.TypeMask);
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