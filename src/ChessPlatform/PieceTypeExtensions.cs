using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessPlatform
{
    public static class PieceTypeExtensions
    {
        #region Public Methods

        public static Piece ToPiece(this PieceType pieceType, PieceColor color)
        {
            #region Argument Check

            pieceType.EnsureDefined();
            color.EnsureDefined();

            #endregion

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            var result = pieceType == PieceType.None
                ? Piece.None
                : ((Piece)pieceType) | (color == PieceColor.Black ? Piece.BlackColor : Piece.WhiteColor);

            return result;
        }

        public static bool IsSlidingDiagonally(this PieceType pieceType)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            return (pieceType & PieceTypeMask.SlidingDiagonally) == PieceTypeMask.SlidingDiagonally;
        }

        public static bool IsSlidingStraight(this PieceType pieceType)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            return (pieceType & PieceTypeMask.SlidingStraight) == PieceTypeMask.SlidingStraight;
        }

        #endregion
    }
}