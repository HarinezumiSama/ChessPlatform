using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class PieceExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameSide? GetSide(this Piece piece)
        {
            if (piece == Piece.None)
            {
                return null;
            }

            var result = (GameSide)(((int)piece & PieceConstants.SideMask) >> PieceConstants.BlackSideShift);
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
            var side = piece.GetSide();
            var pieceType = piece.GetPieceType();

            if (!side.HasValue || pieceType == PieceType.None)
            {
                return "Empty Square";
            }

            return $@"{side.Value} {pieceType}";
        }

        #endregion
    }
}