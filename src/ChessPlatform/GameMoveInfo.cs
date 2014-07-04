using System;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public struct GameMoveInfo
    {
        #region Constants and Fields

        private readonly PieceMoveFlags _flags;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMoveInfo"/> class
        ///     using the specified flags.
        /// </summary>
        internal GameMoveInfo(PieceMoveFlags flags)
        {
            _flags = flags;
        }

        #endregion

        #region Public Properties

        public bool IsPawnPromotion
        {
            get
            {
                return (_flags & PieceMoveFlags.IsPawnPromotion) != 0;
            }
        }

        public bool IsCapture
        {
            get
            {
                return (_flags & (PieceMoveFlags.IsCapture | PieceMoveFlags.IsEnPassantCapture)) != 0;
            }
        }

        public bool IsEnPassantCapture
        {
            get
            {
                return (_flags & PieceMoveFlags.IsEnPassantCapture) != 0;
            }
        }

        #endregion
    }
}