using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChessPlatform
{
    public static class GameMoveFlagsExtensions
    {
        #region Public Methods

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnySet(this GameMoveFlags value, GameMoveFlags flags) => (value & flags) != 0;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPawnPromotion(this GameMoveFlags value) => value.IsAnySet(GameMoveFlags.IsPawnPromotion);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRegularCapture(this GameMoveFlags value)
            => value.IsAnySet(GameMoveFlags.IsRegularCapture);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnPassantCapture(this GameMoveFlags value)
            => value.IsAnySet(GameMoveFlags.IsEnPassantCapture);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyCapture(this GameMoveFlags value)
            => value.IsAnySet(GameMoveFlags.IsRegularCapture | GameMoveFlags.IsEnPassantCapture);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKingCastling(this GameMoveFlags value) => value.IsAnySet(GameMoveFlags.IsKingCastling);

        #endregion
    }
}