using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public static class PieceTypeExtensions
    {
        #region Public Methods

        public static PieceType EnsureDefined(this PieceType pieceType)
        {
            if (DebugConstants.EnsureEnumValuesDefined && !ChessConstants.PieceTypes.Contains(pieceType))
            {
                throw new InvalidEnumArgumentException("pieceType", (int)pieceType, pieceType.GetType());
            }

            return pieceType;
        }

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

        public static char GetFenChar(this PieceType pieceType)
        {
            char result;
            if (!ChessConstants.PieceTypeToFenCharMap.TryGetValue(pieceType, out result))
            {
                throw new ArgumentException("Invalid piece type.", "pieceType");
            }

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

        public static bool IsSliding(this PieceType pieceType)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags - [vmcl] By design
            return (pieceType & PieceTypeMask.Sliding) == PieceTypeMask.Sliding;
        }

        #endregion
    }
}