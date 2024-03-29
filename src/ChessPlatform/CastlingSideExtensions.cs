﻿using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class CastlingSideExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CastlingType ToCastlingType(this CastlingSide castlingSide, GameSide gameSide)
        {
            return unchecked((CastlingType)(((int)gameSide << 1) + castlingSide));
        }
    }
}