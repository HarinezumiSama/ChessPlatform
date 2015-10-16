﻿using System;
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

        public bool IsPawnPromotion => (_flags & GameMoveFlags.IsPawnPromotion) != 0;

        public bool IsAnyCapture => (_flags & (GameMoveFlags.IsCapture | GameMoveFlags.IsEnPassantCapture)) != 0;

        public bool IsCapture => (_flags & GameMoveFlags.IsCapture) != 0;

        public bool IsEnPassantCapture => (_flags & GameMoveFlags.IsEnPassantCapture) != 0;

        public bool IsKingCastling => (_flags & GameMoveFlags.IsKingCastling) != 0;

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{{0}}}", this.ToPropertyString());
        }

        #endregion
    }
}