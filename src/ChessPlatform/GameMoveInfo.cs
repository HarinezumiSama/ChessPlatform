using System;
using System.Globalization;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public struct GameMoveInfo
    {
        #region Constants and Fields

        private readonly GameMoveFlags _flags;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameMoveInfo"/> class
        ///     using the specified flags.
        /// </summary>
        internal GameMoveInfo(GameMoveFlags flags)
        {
            _flags = flags;
        }

        #endregion

        #region Public Properties

        public bool IsPawnPromotion
        {
            get
            {
                return (_flags & GameMoveFlags.IsPawnPromotion) != 0;
            }
        }

        public bool IsCapture
        {
            get
            {
                return (_flags & (GameMoveFlags.IsCapture | GameMoveFlags.IsEnPassantCapture)) != 0;
            }
        }

        public bool IsEnPassantCapture
        {
            get
            {
                return (_flags & GameMoveFlags.IsEnPassantCapture) != 0;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{{0}}}", this.ToPropertyString());
        }

        #endregion
    }
}