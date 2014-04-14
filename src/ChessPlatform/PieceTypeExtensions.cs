using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessPlatform
{
    public static class PieceTypeExtensions
    {
        #region Constants and Fields

        private static readonly Dictionary<PieceType, char> BaseFenCharCache = new Dictionary<PieceType, char>();

        #endregion

        #region Public Methods

        public static char GetBaseFenChar(this PieceType pieceType)
        {
            #region Argument Check

            pieceType.EnsureDefined();

            #endregion

            char result;
            lock (BaseFenCharCache)
            {
                result = BaseFenCharCache.GetValueOrCreate(
                    pieceType,
                    item => BaseFenCharAttribute.GetBaseFenCharNonCached(item));
            }

            return result;
        }

        public static Piece ToPiece(this PieceType pieceType, PieceColor color)
        {
            #region Argument Check

            pieceType.EnsureDefined();
            color.EnsureDefined();

            #endregion

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            var result = ((Piece)pieceType)
                | (color == PieceColor.Black ? Piece.BlackColor : Piece.WhiteColor);

            return result;
        }

        #endregion
    }
}