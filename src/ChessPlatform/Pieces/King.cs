using System;
using System.Linq;

namespace ChessPlatform.Pieces
{
    public sealed class King : Piece
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="King"/> class.
        /// </summary>
        internal King()
        {
            // Nothing to do
        }

        #endregion

        #region Protected Properties

        protected override char BaseFenChar
        {
            get
            {
                return 'K';
            }
        }

        #endregion
    }
}