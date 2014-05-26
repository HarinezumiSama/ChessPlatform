using System;
using System.Linq;
using ChessPlatform.Internal;

namespace ChessPlatform
{
    public sealed class PieceMoveInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceMoveInfo"/> class.
        /// </summary>
        internal PieceMoveInfo(PieceMoveFlags flags)
        {
            this.IsCapture = (flags & PieceMoveFlags.IsCapture) != 0;
            this.IsPawnPromotion = (flags & PieceMoveFlags.IsPawnPromotion) != 0;
        }

        #endregion

        #region Public Properties

        public bool IsCapture
        {
            get;
            private set;
        }

        public bool IsPawnPromotion
        {
            get;
            private set;
        }

        #endregion
    }
}