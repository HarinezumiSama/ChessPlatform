using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                result = BaseFenCharCache.GetValueOrCreate(pieceType, GetBaseFenCharNonCached);
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

        #region Private Methods

        private static char GetBaseFenCharNonCached(PieceType pieceType)
        {
            var field = pieceType.GetType()
                .GetField(pieceType.GetName(), BindingFlags.Static | BindingFlags.Public)
                .EnsureNotNull();

            var attribute = field.GetSingleCustomAttribute<BaseFenCharAttribute>(false);
            return attribute.BaseFenChar;
        }

        #endregion
    }
}