using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public static class PieceExtensions
    {
        #region Constants and Fields

        private static readonly ReadOnlyDictionary<int, PieceColor> PieceToColorMap =
            ChessConstants
                .Pieces
                .Where(item => item != Piece.None)
                .ToDictionary(item => (int)item, GetColorNonCached)
                .AsReadOnly();

        private static readonly ReadOnlyDictionary<int, PieceType> PieceToPieceTypeMap =
            ChessConstants
                .Pieces
                .ToDictionary(item => (int)item, GetPieceTypeNonCached)
                .AsReadOnly();

        #endregion

        #region Public Methods

        public static Piece EnsureDefined(this Piece piece)
        {
            if (!ChessConstants.Pieces.Contains(piece))
            {
                throw new InvalidEnumArgumentException("piece", (int)piece, piece.GetType());
            }

            return piece;
        }

        public static PieceColor? GetColor(this Piece piece)
        {
            if (piece == Piece.None)
            {
                return null;
            }

            PieceColor result;
            if (!PieceToColorMap.TryGetValue((int)piece, out result))
            {
                throw new InvalidEnumArgumentException("piece", (int)piece, piece.GetType());
            }

            return result;
        }

        public static PieceType GetPieceType(this Piece piece)
        {
            PieceType result;
            if (!PieceToPieceTypeMap.TryGetValue((int)piece, out result))
            {
                throw new InvalidEnumArgumentException("piece", (int)piece, piece.GetType());
            }

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

        #region Private Methods

        private static PieceColor GetColorNonCached(Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            if (piece == Piece.None)
            {
                throw new ArgumentException("Cannot be an empty square.", "piece");
            }

            #endregion

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            var result = (piece & Piece.ColorMask) == Piece.BlackColor ? PieceColor.Black : PieceColor.White;
            return result.EnsureDefined();
        }

        private static PieceType GetPieceTypeNonCached(Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            var result = (PieceType)(piece & Piece.TypeMask);
            return result.EnsureDefined();
        }

        #endregion
    }
}