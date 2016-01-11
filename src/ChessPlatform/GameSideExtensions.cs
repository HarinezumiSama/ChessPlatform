using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class GameSideExtensions
    {
        #region Public Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameSide EnsureDefined(this GameSide side)
        {
            switch (side)
            {
                case GameSide.White:
                case GameSide.Black:
                    return side;

                default:
                    throw new InvalidEnumArgumentException(nameof(side), (int)side, side.GetType());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameSide Invert(this GameSide side)
        {
            return (GameSide)(GameSide.Black - side);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece ToPiece(this GameSide side, PieceType pieceType)
        {
            return pieceType.ToPiece(side);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFenSnippet(this GameSide side)
        {
            return ChessConstants.GameSideToFenSnippetMap[side];
        }

        #endregion
    }
}