using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public struct GameMoveInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMoveInfo"/> structure
        ///     using the specified flags.
        /// </summary>
        internal GameMoveInfo(GameMoveFlags flags)
        {
            Flags = flags;
        }

        #endregion

        #region Public Properties

        public bool IsPawnPromotion
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsPawnPromotion) != 0;
            }
        }

        public bool IsAnyCapture
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & (GameMoveFlags.IsRegularCapture | GameMoveFlags.IsEnPassantCapture)) != 0;
            }
        }

        public bool IsRegularCapture
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsRegularCapture) != 0;
            }
        }

        public bool IsEnPassantCapture
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsEnPassantCapture) != 0;
            }
        }

        public bool IsKingCastling
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsKingCastling) != 0;
            }
        }

        public GameMoveFlags Flags
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{{ Flags = {Flags} }}";
        }

        #endregion
    }
}