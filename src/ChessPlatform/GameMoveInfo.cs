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
        ///     Initializes a new instance of the <see cref="GameMoveInfo"/> class
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsPawnPromotion) != 0;
            }
        }

        public bool IsAnyCapture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & (GameMoveFlags.IsCapture | GameMoveFlags.IsEnPassantCapture)) != 0;
            }
        }

        public bool IsRegularCapture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsCapture) != 0;
            }
        }

        public bool IsEnPassantCapture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsEnPassantCapture) != 0;
            }
        }

        public bool IsKingCastling
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (Flags & GameMoveFlags.IsKingCastling) != 0;
            }
        }

        #endregion

        #region Internal Properties

        internal GameMoveFlags Flags
        {
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