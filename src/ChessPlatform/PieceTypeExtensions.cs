using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public static class PieceTypeExtensions
    {
        #region Public Methods

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceType EnsureDefined(this PieceType pieceType)
        {
            if (DebugConstants.EnsureEnumValuesDefined && !ChessConstants.PieceTypes.Contains(pieceType))
            {
                throw new InvalidEnumArgumentException("pieceType", (int)pieceType, pieceType.GetType());
            }

            return pieceType;
        }

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece ToPiece(this PieceType pieceType, PieceColor color)
        {
            var result = pieceType == PieceType.None
                ? Piece.None
                : (Piece)((int)pieceType | ((int)color << PieceConstants.BlackColorShift));

            return result;
        }

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetFenChar(this PieceType pieceType)
        {
            char result;
            if (!ChessConstants.PieceTypeToFenCharMap.TryGetValue(pieceType, out result))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "Invalid piece type ({0}).", pieceType),
                    "pieceType");
            }

            return result;
        }

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSlidingDiagonally(this PieceType pieceType)
        {
            return ((int)pieceType & (int)PieceTypeMask.SlidingDiagonally) == (int)PieceTypeMask.SlidingDiagonally;
        }

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSlidingStraight(this PieceType pieceType)
        {
            return ((int)pieceType & (int)PieceTypeMask.SlidingStraight) == (int)PieceTypeMask.SlidingStraight;
        }

        //// TODO [vmcl] Use for FW 4.5+
        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSliding(this PieceType pieceType)
        {
            return ((int)pieceType & (int)PieceTypeMask.Sliding) == (int)PieceTypeMask.Sliding;
        }

        #endregion
    }
}