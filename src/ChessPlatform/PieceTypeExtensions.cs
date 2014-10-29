using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public static class PieceTypeExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PieceType EnsureDefined(this PieceType pieceType)
        {
            if (DebugConstants.EnsureEnumValuesDefined && !ChessConstants.PieceTypes.Contains(pieceType))
            {
                throw new InvalidEnumArgumentException("pieceType", (int)pieceType, pieceType.GetType());
            }

            return pieceType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece ToPiece(this PieceType pieceType, PieceColor color)
        {
            var result = pieceType == PieceType.None
                ? Piece.None
                : (Piece)((int)pieceType | ((int)color << PieceConstants.BlackColorShift));

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSlidingDiagonally(this PieceType pieceType)
        {
            return ((int)pieceType & (int)PieceTypeMask.SlidingDiagonally) == (int)PieceTypeMask.SlidingDiagonally;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSlidingStraight(this PieceType pieceType)
        {
            return ((int)pieceType & (int)PieceTypeMask.SlidingStraight) == (int)PieceTypeMask.SlidingStraight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSliding(this PieceType pieceType)
        {
            return ((int)pieceType & (int)PieceTypeMask.Sliding) == (int)PieceTypeMask.Sliding;
        }

        #endregion
    }
}