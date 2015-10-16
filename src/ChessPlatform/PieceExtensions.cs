using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class PieceExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceColor? GetColor(this Piece piece)
        {
            if (piece == Piece.None)
            {
                return null;
            }

            var result = (PieceColor)(((int)piece & PieceConstants.ColorMask) >> PieceConstants.BlackColorShift);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceType GetPieceType(this Piece piece)
        {
            var result = (PieceType)((int)piece & PieceConstants.TypeMask);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                throw new ArgumentException("Invalid piece.", nameof(piece));
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