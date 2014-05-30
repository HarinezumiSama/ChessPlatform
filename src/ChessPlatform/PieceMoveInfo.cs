using System;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public sealed class PieceMoveInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceMoveInfo"/> class
        ///     using the specified flags.
        /// </summary>
        internal PieceMoveInfo(PieceMoveFlags flags)
        {
            this.IsPawnPromotion = (flags & PieceMoveFlags.IsPawnPromotion) != 0;
            this.IsCapture = (flags & (PieceMoveFlags.IsCapture | PieceMoveFlags.IsEnPassantCapture)) != 0;
            this.IsEnPassantCapture = (flags & PieceMoveFlags.IsEnPassantCapture) != 0;
        }

        #endregion

        #region Public Properties

        public bool IsPawnPromotion
        {
            get;
            private set;
        }

        public bool IsCapture
        {
            get;
            private set;
        }

        public bool IsEnPassantCapture
        {
            get;
            private set;
        }

        #endregion
    }
}